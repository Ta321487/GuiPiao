using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using GuiPiao.Model;

namespace GuiPiao.Services;

/// <summary>
///     下载服务 - 支持断点续传、暂�?恢复
///     配置持久化到OcrConfig�?
/// </summary>
public class DownloadService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly object _lock = new();
    private readonly OcrSettingsService _settingsService;
    private CancellationTokenSource? _cancellationTokenSource;

    public DownloadService(OcrSettingsService settingsService)
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromMinutes(30);
        _settingsService = settingsService;

        // 加载时如果之前是下载中状态，改为暂停
        var task = _settingsService.Config.PythonDownloadTask;
        if (task?.State == DownloadState.Downloading)
        {
            task.State = DownloadState.Paused;
            _settingsService.SaveConfig(_settingsService.Config);
        }
    }

    public DownloadState CurrentState => _settingsService.Config.PythonDownloadTask?.State ?? DownloadState.Idle;
    public PythonDownloadTask? CurrentTask => _settingsService.Config.PythonDownloadTask;

    public void Dispose()
    {
        // 如果正在下载，暂停并保存状�?
        if (CurrentState == DownloadState.Downloading) PauseDownload();

        _cancellationTokenSource?.Dispose();
        _httpClient?.Dispose();
    }

    public event EventHandler<DownloadProgressChangedEventArgs>? ProgressChanged;
    public event EventHandler<DownloadStateChangedEventArgs>? StateChanged;
    public event EventHandler<DownloadCompletedEventArgs>? Completed;

    /// <summary>
    ///     开始或继续下载Python
    /// </summary>
    public async Task<bool> StartPythonDownloadAsync(
        IProgress<(long downloaded, long total, string message)>? progress = null)
    {
        const string pythonUrl = "https://www.python.org/ftp/python/3.12.9/python-3.12.9-amd64.exe";
        var tempPath = Path.Combine(Path.GetTempPath(), "python-3.12.9-amd64.exe");

        lock (_lock)
        {
            var task = _settingsService.Config.PythonDownloadTask;

            if (task?.State == DownloadState.Downloading)
                return false;

            // 如果是新任务或已完成的任务，创建新任�?
            if (task == null || task.State == DownloadState.Completed || task.Url != pythonUrl)
            {
                _settingsService.Config.PythonDownloadTask = new PythonDownloadTask
                {
                    Url = pythonUrl,
                    FilePath = tempPath,
                    DownloadedBytes = 0,
                    State = DownloadState.Downloading
                };
            }
            else
            {
                task.State = DownloadState.Downloading;
                task.ErrorMessage = "";
            }

            _cancellationTokenSource = new CancellationTokenSource();
            _settingsService.SaveConfig(_settingsService.Config);
        }

        UpdateState(DownloadState.Downloading);

        try
        {
            var success = await DownloadWithResumeAsync(progress);

            var task = _settingsService.Config.PythonDownloadTask;
            if (task != null)
            {
                if (success)
                {
                    task.State = DownloadState.Completed;
                    task.CompletedAt = DateTime.Now;
                    UpdateState(DownloadState.Completed);
                    Completed?.Invoke(this, new DownloadCompletedEventArgs(true, null));
                }
                else if (_cancellationTokenSource?.IsCancellationRequested == true)
                {
                    task.State = DownloadState.Paused;
                    UpdateState(DownloadState.Paused);
                }
                else
                {
                    task.State = DownloadState.Failed;
                    UpdateState(DownloadState.Failed);
                }

                _settingsService.SaveConfig(_settingsService.Config);
            }

            return success;
        }
        catch (Exception ex)
        {
            var task = _settingsService.Config.PythonDownloadTask;
            if (task != null)
            {
                task.State = DownloadState.Failed;
                task.ErrorMessage = ex.Message;
                UpdateState(DownloadState.Failed);
                _settingsService.SaveConfig(_settingsService.Config);
            }

            Completed?.Invoke(this, new DownloadCompletedEventArgs(false, ex));
            return false;
        }
    }

    /// <summary>
    ///     暂停下载
    /// </summary>
    public void PauseDownload()
    {
        lock (_lock)
        {
            var task = _settingsService.Config.PythonDownloadTask;
            if (task?.State == DownloadState.Downloading)
            {
                _cancellationTokenSource?.Cancel();
                task.State = DownloadState.Paused;
                UpdateState(DownloadState.Paused);
                _settingsService.SaveConfig(_settingsService.Config);
            }
        }
    }

    /// <summary>
    ///     恢复下载
    /// </summary>
    public Task<bool> ResumeDownloadAsync(IProgress<(long downloaded, long total, string message)>? progress = null)
    {
        return StartPythonDownloadAsync(progress);
    }

    /// <summary>
    ///     取消下载（删除文件）
    /// </summary>
    public void CancelDownload()
    {
        lock (_lock)
        {
            _cancellationTokenSource?.Cancel();

            var task = _settingsService.Config.PythonDownloadTask;
            if (task != null)
            {
                // 删除未完成的文件
                if (File.Exists(task.FilePath))
                    try
                    {
                        File.Delete(task.FilePath);
                    }
                    catch
                    {
                    }

                _settingsService.Config.PythonDownloadTask = null;
                _settingsService.SaveConfig(_settingsService.Config);
            }

            UpdateState(DownloadState.Idle);
        }
    }

    /// <summary>
    ///     断点续传下载
    /// </summary>
    private async Task<bool> DownloadWithResumeAsync(IProgress<(long downloaded, long total, string message)>? progress)
    {
        var task = _settingsService.Config.PythonDownloadTask;
        if (task == null) return false;

        var filePath = task.FilePath;
        var url = task.Url;
        var startPosition = task.DownloadedBytes;

        // 确保目录存在
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        // 检查服务器是否支持断点续传
        var headResponse = await _httpClient.SendAsync(
            new HttpRequestMessage(HttpMethod.Head, url),
            HttpCompletionOption.ResponseHeadersRead);

        if (!headResponse.IsSuccessStatusCode)
        {
            task.ErrorMessage = "无法获取文件信息";
            return false;
        }

        var totalBytes = headResponse.Content.Headers.ContentLength ?? 0;
        task.TotalBytes = totalBytes;

        // 如果文件已存在且大小匹配，说明已下载完成
        if (File.Exists(filePath) && new FileInfo(filePath).Length == totalBytes && totalBytes > 0)
        {
            task.DownloadedBytes = totalBytes;
            return true;
        }

        // 创建或打开文件
        using var fileStream = new FileStream(
            filePath,
            FileMode.OpenOrCreate,
            FileAccess.Write,
            FileShare.None);

        fileStream.Seek(startPosition, SeekOrigin.Begin);

        // 设置Range头进行断点续�?
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (startPosition > 0) request.Headers.Range = new RangeHeaderValue(startPosition, null);

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            _cancellationTokenSource?.Token ?? CancellationToken.None);

        if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.PartialContent)
        {
            task.ErrorMessage = $"下载失败: {response.StatusCode}";
            return false;
        }

        using var stream = await response.Content.ReadAsStreamAsync();
        var buffer = new byte[8192];
        var bytesRead = 0;
        var totalRead = startPosition;

        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length,
                   _cancellationTokenSource?.Token ?? CancellationToken.None)) > 0)
        {
            await fileStream.WriteAsync(buffer, 0, bytesRead);
            totalRead += bytesRead;
            task.DownloadedBytes = totalRead;

            // 报告进度
            var percent = totalBytes > 0 ? (int)((double)totalRead / totalBytes * 100) : 0;
            progress?.Report((totalRead, totalBytes, $"正在下载... {percent}%"));
            ProgressChanged?.Invoke(this, new DownloadProgressChangedEventArgs(totalRead, totalBytes, percent));

            // 定期保存进度（每5%或每10MB�?
            if (percent % 5 == 0 || bytesRead >= 10 * 1024 * 1024) _settingsService.SaveConfig(_settingsService.Config);
        }

        return totalRead >= totalBytes || totalBytes == 0;
    }

    /// <summary>
    ///     获取下载进度百分�?
    /// </summary>
    public int GetProgressPercentage()
    {
        var task = _settingsService.Config.PythonDownloadTask;
        if (task == null || task.TotalBytes == 0) return 0;
        return (int)((double)task.DownloadedBytes / task.TotalBytes * 100);
    }

    /// <summary>
    ///     检查是否有未完成的下载任务
    /// </summary>
    public bool HasIncompleteTask()
    {
        var task = _settingsService.Config.PythonDownloadTask;
        return task != null && task.State != DownloadState.Completed && task.State != DownloadState.Idle;
    }

    private void UpdateState(DownloadState state)
    {
        StateChanged?.Invoke(this, new DownloadStateChangedEventArgs(state));
    }
}

public class DownloadProgressChangedEventArgs : EventArgs
{
    public DownloadProgressChangedEventArgs(long downloaded, long total, int percent)
    {
        DownloadedBytes = downloaded;
        TotalBytes = total;
        ProgressPercentage = percent;
    }

    public long DownloadedBytes { get; }
    public long TotalBytes { get; }
    public int ProgressPercentage { get; }
}

public class DownloadStateChangedEventArgs : EventArgs
{
    public DownloadStateChangedEventArgs(DownloadState state)
    {
        State = state;
    }

    public DownloadState State { get; }
}

public class DownloadCompletedEventArgs : EventArgs
{
    public DownloadCompletedEventArgs(bool success, Exception? error)
    {
        Success = success;
        Error = error;
    }

    public bool Success { get; }
    public Exception? Error { get; }
}