using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GuiPiao.Model;
using GuiPiao.Services;
using GuiPiao.View;
using Microsoft.Win32;

namespace GuiPiao.ViewModel;

/// <summary>
///     OCR设置视图模型
/// </summary>
public partial class OcrSettingsViewModel : ObservableObject, ISettingsViewModel
{
    private readonly DownloadService _downloadService;
    private readonly OcrEnvironmentService _envService;
    private readonly OcrRecognitionService _recognitionService;
    private readonly OcrSettingsService _settingsService;
    private bool _isLoadingConfig;
    private OcrConfig _originalConfig;

    public OcrSettingsViewModel()
    {
        if (DesignerProperties.GetIsInDesignMode(new DependencyObject())) return;

        _settingsService = new OcrSettingsService();
        _envService = new OcrEnvironmentService();
        _recognitionService = new OcrRecognitionService();
        _downloadService = new DownloadService(_settingsService);

        // 订阅下载事件
        _downloadService.StateChanged += OnDownloadStateChanged;
        _downloadService.ProgressChanged += OnDownloadProgressChanged;

        LoadConfig();

        // 检查是否有未完成的下载任务
        CheckIncompleteDownload();

        // 打开页面时自动检测环境（在后台执行，不阻塞UI）
        _ = AutoCheckEnvironmentAsync();
    }

    // 状态文本属性 - 在安装完成后需要手动触发属性变更通知
    public string CnOCRStatusText => IsCnocrInstalled ? "已安装" : "未安装";
    public string CnstdStatusText => IsCnstdInstalled ? "已安装" : "未安装";
    public string DetectionStatusText => IsDetectionModelInstalled ? "已安装" : "未安装";
    public string RecognitionStatusText => IsRecognitionModelInstalled ? "已安装" : "未安装";

    // 状态颜色属性 (返回Brush) - 在安装完成后需要手动触发属性变更通知
    public Brush CnOCRStatusColor => IsCnocrInstalled
        ? new SolidColorBrush(Color.FromRgb(40, 167, 69))
        : // #28A745
        new SolidColorBrush(Color.FromRgb(220, 53, 69)); // #DC3545

    public Brush CnstdStatusColor => IsCnstdInstalled
        ? new SolidColorBrush(Color.FromRgb(40, 167, 69))
        : new SolidColorBrush(Color.FromRgb(220, 53, 69));

    public Brush DetectionStatusColor => IsDetectionModelInstalled
        ? new SolidColorBrush(Color.FromRgb(40, 167, 69))
        : new SolidColorBrush(Color.FromRgb(220, 53, 69));

    public Brush RecognitionStatusColor => IsRecognitionModelInstalled
        ? new SolidColorBrush(Color.FromRgb(40, 167, 69))
        : new SolidColorBrush(Color.FromRgb(220, 53, 69));

    /// <summary>
    ///     是否有未保存的更改
    /// </summary>
    public bool HasUnsavedChanges
    {
        get
        {
            if (_isLoadingConfig || _originalConfig == null)
                return false;

            var currentConfig = GetCurrentConfig();
            return !ConfigsEqual(_originalConfig, currentConfig);
        }
    }

    /// <summary>
    ///     触发所有状态属性的属性变更通知
    /// </summary>
    private void NotifyStatusPropertiesChanged()
    {
        OnPropertyChanged(nameof(CnOCRStatusText));
        OnPropertyChanged(nameof(CnstdStatusText));
        OnPropertyChanged(nameof(DetectionStatusText));
        OnPropertyChanged(nameof(RecognitionStatusText));
        OnPropertyChanged(nameof(CnOCRStatusColor));
        OnPropertyChanged(nameof(CnstdStatusColor));
        OnPropertyChanged(nameof(DetectionStatusColor));
        OnPropertyChanged(nameof(RecognitionStatusColor));
    }

    /// <summary>
    ///     检查是否有未完成的下载任务
    /// </summary>
    private void CheckIncompleteDownload()
    {
        var task = _settingsService.Config.PythonDownloadTask;
        if (task != null && task.State == DownloadState.Paused)
        {
            // 有暂停的下载任务，显示恢复按钮
            IsDownloadPaused = true;
            DownloadButtonText = "▶️ 恢复下载";
            ShowPauseButton = true;
            IsInstalling = true;
            InstallProgress = _downloadService.GetProgressPercentage();
            InstallMessage = $"下载已暂停 ({InstallProgress}%)，点击恢复继续下载";
        }
    }

    /// <summary>
    ///     下载状态变化处理
    /// </summary>
    private void OnDownloadStateChanged(object? sender, DownloadStateChangedEventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            switch (e.State)
            {
                case DownloadState.Downloading:
                    IsDownloadPaused = false;
                    DownloadButtonText = "⏸️ 暂停下载";
                    ShowPauseButton = true;
                    break;
                case DownloadState.Paused:
                    IsDownloadPaused = true;
                    DownloadButtonText = "▶️ 恢复下载";
                    ShowPauseButton = true;
                    InstallMessage = $"下载已暂停 ({InstallProgress}%)";
                    break;
                case DownloadState.Completed:
                    ShowPauseButton = false;
                    break;
                case DownloadState.Failed:
                    IsDownloadPaused = false;
                    DownloadButtonText = "⏸️ 暂停下载";
                    ShowPauseButton = false;
                    break;
            }
        });
    }

    /// <summary>
    ///     下载进度变化处理
    /// </summary>
    private void OnDownloadProgressChanged(object? sender, DownloadProgressChangedEventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            InstallProgress = e.ProgressPercentage;
            InstallMessage =
                $"正在下载Python... {e.ProgressPercentage}% ({e.DownloadedBytes / 1024 / 1024}MB / {e.TotalBytes / 1024 / 1024}MB)";
        });
    }

    /// <summary>
    ///     自动检测环境（后台执行）
    /// </summary>
    private async Task AutoCheckEnvironmentAsync()
    {
        try
        {
            // 显示检测中状态
            IsChecking = true;
            EnvironmentStatus = "正在检测...";

            // 检查Python
            var (pythonInstalled, pythonVer, isVersionValid, pythonPath) = await _envService.CheckPythonInstalled();
            IsPythonInstalled = pythonInstalled;

            // 保存检测到的Python路径用于显示
            _detectedPythonPath = pythonPath;
            OnPropertyChanged(nameof(PythonPathDisplay));

            // 如果PythonPath是默认值，触发PythonPathDisplay更新以显示检测到的路径
            if (PythonPath == "python") OnPropertyChanged(nameof(PythonPathDisplay));

            if (pythonInstalled)
            {
                if (isVersionValid)
                {
                    PythonVersion = pythonVer;
                    // 自动更新Python路径
                    if (!string.IsNullOrEmpty(pythonPath) && PythonPath == "python")
                    {
                        PythonPath = pythonPath;
                        OnPropertyChanged(nameof(PythonPathDisplay));
                    }
                }
                else
                {
                    PythonVersion = $"{pythonVer} (版本过低，建议 >= 3.8)";
                }
            }
            else
            {
                PythonVersion = "未安装";
            }

            // 检查CnOCR
            IsCnocrInstalled = await _envService.CheckCnocrInstalled();

            // 检查CNSTD
            IsCnstdInstalled = await _envService.CheckCnstdInstalled();

            // 检查模型文件是否存在（直接检查文件，不触发自动下载）
            IsDetectionModelInstalled = _envService.CheckDetectionModelInstalled();
            IsRecognitionModelInstalled = _envService.CheckRecognitionModelInstalled();

            UpdateEnvironmentStatus();
        }
        catch (Exception ex)
        {
            EnvironmentStatus = $"检测失败: {ex.Message}";
        }
        finally
        {
            IsChecking = false;
            IsEnvironmentCheckCompleted = true;
        }
    }

    /// <summary>
    ///     加载配置到视图模型
    /// </summary>
    private void LoadConfig()
    {
        _isLoadingConfig = true;
        try
        {
            _originalConfig = _settingsService.Config;

            // OCR参数配置
            PythonPath = _originalConfig.PythonPath;
            SelectedModel = _originalConfig.SelectedModel;
            UseGpu = _originalConfig.UseGpu;
            ConfidenceThreshold = _originalConfig.ConfidenceThreshold;
            EnableImagePreprocessing = _originalConfig.EnableImagePreprocessing;
            AutoRotateImage = _originalConfig.AutoRotateImage;
            MaxImageSize = _originalConfig.MaxImageSize;

            // 初始环境状态为未检测
            UpdateEnvironmentStatus();

            _originalConfig = GetCurrentConfig();
        }
        finally
        {
            _isLoadingConfig = false;
        }
    }

    /// <summary>
    ///     从视图模型获取当前配置
    /// </summary>
    private OcrConfig GetCurrentConfig()
    {
        return new OcrConfig
        {
            PythonPath = PythonPath,
            SelectedModel = SelectedModel,
            UseGpu = UseGpu,
            ConfidenceThreshold = ConfidenceThreshold,
            EnableImagePreprocessing = EnableImagePreprocessing,
            AutoRotateImage = AutoRotateImage,
            MaxImageSize = MaxImageSize
        };
    }

    /// <summary>
    ///     比较两个配置是否相等
    /// </summary>
    private bool ConfigsEqual(OcrConfig a, OcrConfig b)
    {
        return a.PythonPath == b.PythonPath &&
               a.SelectedModel == b.SelectedModel &&
               a.UseGpu == b.UseGpu &&
               a.ConfidenceThreshold == b.ConfidenceThreshold &&
               a.EnableImagePreprocessing == b.EnableImagePreprocessing &&
               a.AutoRotateImage == b.AutoRotateImage &&
               a.MaxImageSize == b.MaxImageSize;
    }

    /// <summary>
    ///     更新环境状态显示
    /// </summary>
    private void UpdateEnvironmentStatus()
    {
        if (IsPythonInstalled && IsCnocrInstalled && IsCnstdInstalled &&
            IsDetectionModelInstalled && IsRecognitionModelInstalled)
        {
            IsEnvironmentReady = true;
            EnvironmentStatus = "环境就绪 ✓";
        }
        else if (IsPythonInstalled)
        {
            IsEnvironmentReady = false;
            EnvironmentStatus = "环境不完整，需要安装OCR组件";
        }
        else
        {
            IsEnvironmentReady = false;
            EnvironmentStatus = "Python未安装";
        }

        // 触发状态属性变更通知，确保UI更新
        NotifyStatusPropertiesChanged();
    }

    #region 环境状态

    [ObservableProperty] private bool _isPythonInstalled;

    [ObservableProperty] private bool _isCnocrInstalled;

    [ObservableProperty] private bool _isCnstdInstalled;

    [ObservableProperty] private bool _isDetectionModelInstalled;

    [ObservableProperty] private bool _isRecognitionModelInstalled;

    [ObservableProperty] private bool _isEnvironmentReady;

    [ObservableProperty] private string _pythonVersion = "未检测";

    [ObservableProperty] private string _environmentStatus = "未检测";

    [ObservableProperty] private bool _isChecking;

    [ObservableProperty] private bool _isEnvironmentCheckCompleted;

    #endregion

    #region 安装进度

    [ObservableProperty] private bool _isInstalling;

    [ObservableProperty] private string _installMessage = "";

    [ObservableProperty] private int _installProgress;

    [ObservableProperty] private bool _isPythonDownloading;

    [ObservableProperty] private bool _isCnocrInstalling;

    [ObservableProperty] private bool _isModelCopying;

    [ObservableProperty] private bool _isDownloadPaused;

    [ObservableProperty] private string _downloadButtonText = "⏸️ 暂停下载";

    [ObservableProperty] private bool _showPauseButton;

    #endregion

    #region OCR参数配置

    [ObservableProperty] private string _pythonPath = "python";

    // PythonPath变更时触发PythonPathDisplay更新
    partial void OnPythonPathChanged(string value)
    {
        OnPropertyChanged(nameof(PythonPathDisplay));
    }

    // 用于显示的Python路径（自动检测时显示实际路径）
    public string PythonPathDisplay => PythonPath == "python" ? $"自动检测 ({_detectedPythonPath ?? "未检测"})" : PythonPath;

    private string? _detectedPythonPath;

    [ObservableProperty] private string _selectedModel = "densenet_lite_136-gru";

    [ObservableProperty] private bool _useGpu;

    [ObservableProperty] private double _confidenceThreshold = 0.85;

    [ObservableProperty] private bool _enableImagePreprocessing = true;

    [ObservableProperty] private bool _autoRotateImage = true;

    [ObservableProperty] private int _maxImageSize = 1920;

    #endregion

    #region 测试区域

    [ObservableProperty] private string _testImagePath = "";

    [ObservableProperty] private bool _isTesting;

    [ObservableProperty] private string _testResult = "";

    [ObservableProperty] private bool _testSuccess;

    #endregion

    #region 命令

    /// <summary>
    ///     检查环境命令
    /// </summary>
    [RelayCommand]
    private async Task CheckEnvironment()
    {
        IsInstalling = true;
        IsChecking = true;
        InstallMessage = "正在检测OCR环境...";
        InstallProgress = 0;

        try
        {
            // 检查Python
            InstallMessage = "正在检测Python环境...";
            var (pythonInstalled, pythonVer, isVersionValid, pythonPath) = await _envService.CheckPythonInstalled();
            IsPythonInstalled = pythonInstalled;

            // 保存检测到的Python路径用于显示
            _detectedPythonPath = pythonPath;
            OnPropertyChanged(nameof(PythonPathDisplay));

            if (pythonInstalled)
            {
                if (isVersionValid)
                {
                    PythonVersion = pythonVer;
                    // 自动更新Python路径
                    if (!string.IsNullOrEmpty(pythonPath) && PythonPath == "python")
                    {
                        PythonPath = pythonPath;
                        OnPropertyChanged(nameof(PythonPathDisplay));
                    }
                }
                else
                {
                    PythonVersion = $"{pythonVer} (版本过低，建议 >= 3.8)";
                }
            }
            else
            {
                PythonVersion = "未安装";
            }

            InstallProgress = 20;

            // 检查CnOCR
            InstallMessage = "正在检测CnOCR...";
            IsCnocrInstalled = await _envService.CheckCnocrInstalled();
            InstallProgress = 40;

            // 检查CNSTD
            InstallMessage = "正在检测CNSTD...";
            IsCnstdInstalled = await _envService.CheckCnstdInstalled();
            InstallProgress = 60;

            // 检查检测模型
            InstallMessage = "正在检测文本检测模型...";
            IsDetectionModelInstalled = _envService.CheckDetectionModelInstalled();
            InstallProgress = 80;

            // 检查识别模型
            InstallMessage = "正在检测文字识别模型...";
            IsRecognitionModelInstalled = _envService.CheckRecognitionModelInstalled();
            InstallProgress = 100;

            UpdateEnvironmentStatus();
            InstallMessage = "环境检测完成";
        }
        catch (Exception ex)
        {
            InstallMessage = $"检测失败: {ex.Message}";
        }
        finally
        {
            await Task.Delay(500);
            IsInstalling = false;
            IsChecking = false;
        }
    }

    /// <summary>
    ///     安装Python命令（支持暂停/恢复）
    /// </summary>
    [RelayCommand]
    private async Task InstallPython()
    {
        Debug.WriteLine($"[DEBUG] InstallPython command executed at {DateTime.Now}");
        try
        {
            // 检查是否有暂停的下载任务
            if (_downloadService.CurrentState == DownloadState.Paused)
            {
                Debug.WriteLine("[DEBUG] Resuming paused download");
                // 恢复下载
                await ToggleDownloadPause();
                return;
            }

            Debug.WriteLine($"[DEBUG] IsPythonInstalled = {IsPythonInstalled}");

            // 如果Python已安装，提示用户
            if (IsPythonInstalled)
            {
                Debug.WriteLine("[DEBUG] Python already installed, showing confirmation dialog");
                var result = MessageBoxWindow.Show(Application.Current.MainWindow,
                    "Python已经安装，是否重新下载安装？",
                    "确认",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                Debug.WriteLine($"[DEBUG] User choice: {result}");

                if (result != MessageBoxResult.Yes)
                {
                    Debug.WriteLine("[DEBUG] User cancelled installation");
                    return;
                }
            }

            Debug.WriteLine("[DEBUG] Starting Python installation process");
            IsPythonDownloading = true;
            IsInstalling = true;
            InstallProgress = 0;
            ShowPauseButton = true;

            Debug.WriteLine("[DEBUG] Starting download...");

            var progress = new Progress<(long downloaded, long total, string message)>(p =>
            {
                InstallProgress = (int)((double)p.downloaded / p.total * 100);
                InstallMessage = p.message;
            });

            var downloadSuccess = await _downloadService.StartPythonDownloadAsync(progress);

            if (!downloadSuccess)
            {
                if (_downloadService.CurrentState == DownloadState.Paused)
                    // 用户暂停了下载
                    return;
                InstallMessage = "Python下载失败";
                ShowPauseButton = false;
                IsPythonDownloading = false;
                IsInstalling = false;
                MessageBoxWindow.Show(Application.Current.MainWindow,
                    "Python下载失败。\n\n请检查网络连接或手动下载安装。",
                    "下载失败",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            // 下载完成，执行安装
            InstallProgress = 100;
            InstallMessage = "正在安装Python...";
            ShowPauseButton = false;

            var installSuccess = await InstallDownloadedPythonAsync();

            if (installSuccess)
            {
                var (installed, version, isVersionValid, pythonPath) = await _envService.CheckPythonInstalled();
                IsPythonInstalled = installed;
                PythonVersion = installed ? version : "未安装";
                if (installed && !string.IsNullOrEmpty(pythonPath)) PythonPath = pythonPath;
                UpdateEnvironmentStatus();
                MessageBoxWindow.Show(Application.Current.MainWindow,
                    "Python安装成功！",
                    "安装成功");
            }
            else
            {
                MessageBoxWindow.Show(Application.Current.MainWindow,
                    "Python安装失败。\n\n请检查安装包或手动安装。",
                    "安装失败",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            IsPythonDownloading = false;
            IsInstalling = false;
        }
        catch (Exception ex)
        {
            MessageBoxWindow.Show(Application.Current.MainWindow,
                $"安装Python时发生错误：{ex.Message}",
                "错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            IsPythonDownloading = false;
            IsInstalling = false;
            ShowPauseButton = false;
        }
    }

    /// <summary>
    ///     安装CnOCR命令
    /// </summary>
    [RelayCommand]
    private async Task InstallCnocr()
    {
        Debug.WriteLine($"[DEBUG] InstallCnocr command executed at {DateTime.Now}");

        // 检查是否已安装
        if (IsCnocrInstalled && IsCnstdInstalled)
        {
            var result = MessageBoxWindow.Show(Application.Current.MainWindow,
                "CnOCR和CNSTD库已经安装，是否重新安装？",
                "确认",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                Debug.WriteLine("[DEBUG] User cancelled CnOCR installation");
                return;
            }
        }

        IsCnocrInstalling = true;
        IsInstalling = true;
        InstallProgress = 0;

        var progress = new Progress<(int progress, string message)>(p =>
        {
            InstallProgress = p.progress;
            InstallMessage = p.message;
        });

        Debug.WriteLine("[DEBUG] Starting CnOCR installation...");
        var success = await _envService.InstallCnocrAndCnstd(progress);
        Debug.WriteLine($"[DEBUG] CnOCR installation result: {success}");

        if (success)
        {
            IsCnocrInstalled = await _envService.CheckCnocrInstalled();
            IsCnstdInstalled = await _envService.CheckCnstdInstalled();
            UpdateEnvironmentStatus();
            MessageBoxWindow.Show(Application.Current.MainWindow,
                "CnOCR和CNSTD安装成功！",
                "安装成功");
        }
        else
        {
            MessageBoxWindow.Show(Application.Current.MainWindow,
                "CnOCR安装失败。\n\n请检查网络连接或手动安装。\n\n手动安装命令：\npip install cnocr[ort-cpu] cnstd",
                "安装失败",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        IsCnocrInstalling = false;
        IsInstalling = false;
    }

    /// <summary>
    ///     打开百度网盘链接
    /// </summary>
    [RelayCommand]
    private void OpenBaiduPan()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "https://pan.baidu.com/s/1RhLBf8DcLnLuGLPrp89hUg?pwd=nocr",
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            MessageBoxWindow.Show(Application.Current.MainWindow,
                $"无法打开浏览器：{ex.Message}\n\n请手动访问：\nhttps://pan.baidu.com/s/1RhLBf8DcLnLuGLPrp89hUg?pwd=nocr",
                "错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    /// <summary>
    ///     导入模型文件（本地选择）
    /// </summary>
    [RelayCommand]
    private async Task ImportModels()
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择模型文件",
            Filter = "ONNX模型文件|*.onnx|所有文件|*.*",
            Multiselect = true,
            CheckFileExists = true
        };

        if (dialog.ShowDialog() == true)
        {
            var files = dialog.FileNames;
            if (files.Length == 0)
            {
                MessageBoxWindow.Show(Application.Current.MainWindow, "请至少选择一个模型文件");
                return;
            }

            IsModelCopying = true;
            IsInstalling = true;
            InstallMessage = "正在导入模型文件...";
            InstallProgress = 0;

            try
            {
                var successCount = 0;
                var detectionModelPath = "";
                var recognitionModelPath = "";

                // 分析选中的文件
                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file).ToLower();

                    // 识别检测模型
                    if (fileName.Contains("det") || fileName.Contains("ch_pp-ocrv4_det"))
                        detectionModelPath = file;
                    // 识别识别模型
                    else if (fileName.Contains("densenet") || fileName.Contains("cnocr")) recognitionModelPath = file;
                }

                // 导入检测模型
                if (!string.IsNullOrEmpty(detectionModelPath))
                {
                    InstallProgress = 30;
                    InstallMessage = "正在导入检测模型...";
                    var success = await _envService.ImportModelFile(detectionModelPath, ModelType.Detection);
                    if (success)
                    {
                        successCount++;
                        IsDetectionModelInstalled = true;
                    }
                }

                // 导入识别模型
                if (!string.IsNullOrEmpty(recognitionModelPath))
                {
                    InstallProgress = 70;
                    InstallMessage = "正在导入识别模型...";
                    var success = await _envService.ImportModelFile(recognitionModelPath, ModelType.Recognition);
                    if (success)
                    {
                        successCount++;
                        IsRecognitionModelInstalled = true;
                    }
                }

                InstallProgress = 100;

                if (successCount > 0)
                {
                    UpdateEnvironmentStatus();
                    MessageBoxWindow.Show(Application.Current.MainWindow,
                        $"成功导入 {successCount} 个模型文件！",
                        "导入成功");
                }
                else
                {
                    MessageBoxWindow.Show(Application.Current.MainWindow,
                        "未能识别有效的模型文件。\n\n请确保选择正确的.onnx文件：\n" +
                        "1. ch_PP-OCRv4_det_infer.onnx（检测模型）\n" +
                        "2. cnocr-v2.3-densenet_lite_136-gru-epoch=004-ft-model.onnx（识别模型）",
                        "导入失败",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBoxWindow.Show(Application.Current.MainWindow,
                    $"导入失败：{ex.Message}",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsModelCopying = false;
                IsInstalling = false;
            }
        }
    }

    /// <summary>
    ///     一键安装所有环境命令
    /// </summary>
    [RelayCommand]
    private async Task InstallAll()
    {
        Debug.WriteLine($"[DEBUG] InstallAll command executed at {DateTime.Now}");
        Debug.WriteLine($"[DEBUG] IsEnvironmentCheckCompleted = {IsEnvironmentCheckCompleted}");
        Debug.WriteLine("[DEBUG] Setting IsInstalling = true");
        IsInstalling = true;
        InstallMessage = "开始一键安装OCR环境...";
        InstallProgress = 0;
        Debug.WriteLine($"[DEBUG] IsInstalling = {IsInstalling}, InstallMessage = {InstallMessage}");

        try
        {
            // 安装Python
            Debug.WriteLine($"[DEBUG] IsPythonInstalled = {IsPythonInstalled}");
            if (!IsPythonInstalled)
            {
                // 检查是否有暂停的下载任务
                if (_downloadService.CurrentState == DownloadState.Paused)
                {
                    // 恢复下载
                    await ToggleDownloadPause();
                    return;
                }

                ShowPauseButton = true;
                var progress = new Progress<(long downloaded, long total, string message)>(p =>
                {
                    InstallProgress = (int)((double)p.downloaded / p.total * 50); // 下载占50%
                    InstallMessage = p.message;
                });

                var downloadSuccess = await _downloadService.StartPythonDownloadAsync(progress);
                if (!downloadSuccess)
                {
                    if (_downloadService.CurrentState == DownloadState.Paused)
                        // 用户暂停了下载
                        return;
                    InstallMessage = "Python下载失败";
                    ShowPauseButton = false;
                    return;
                }

                // 下载完成，执行安装
                InstallProgress = 50;
                InstallMessage = "正在安装Python...";
                ShowPauseButton = false;

                var installSuccess = await InstallDownloadedPythonAsync();
                if (!installSuccess)
                {
                    InstallMessage = "Python安装失败";
                    return;
                }

                var (installed, version, isVersionValid, pythonPath) = await _envService.CheckPythonInstalled();
                IsPythonInstalled = installed;
                PythonVersion = installed ? version : "未安装";
                if (installed && !string.IsNullOrEmpty(pythonPath)) PythonPath = pythonPath;
            }
            else
            {
                // Python已安装，跳过下载
                Debug.WriteLine("[DEBUG] Python already installed, skipping download");
                InstallProgress = 50;
                InstallMessage = "Python已安装，跳过下载...";
                Debug.WriteLine($"[DEBUG] InstallMessage set to: {InstallMessage}");
                // 延迟500ms让用户能看到提示
                await Task.Delay(500);
            }

            // 安装CnOCR和CNSTD
            Debug.WriteLine($"[DEBUG] IsCnocrInstalled = {IsCnocrInstalled}, IsCnstdInstalled = {IsCnstdInstalled}");
            if (!IsCnocrInstalled || !IsCnstdInstalled)
            {
                var cnocrProgress = new Progress<(int progress, string message)>(p =>
                {
                    InstallProgress = 50 + p.progress / 2; // CnOCR占50%
                    InstallMessage = p.message;
                });

                var cnocrSuccess = await _envService.InstallCnocrAndCnstd(cnocrProgress);
                if (!cnocrSuccess)
                {
                    InstallMessage = "CnOCR安装失败";
                    return;
                }

                IsCnocrInstalled = await _envService.CheckCnocrInstalled();
                IsCnstdInstalled = await _envService.CheckCnstdInstalled();
            }
            else
            {
                // CnOCR已安装，跳过
                InstallProgress = 75;
                InstallMessage = "CnOCR库已安装，跳过...";
                await Task.Delay(300);
            }

            // 检查模型文件
            Debug.WriteLine(
                $"[DEBUG] IsDetectionModelInstalled = {IsDetectionModelInstalled}, IsRecognitionModelInstalled = {IsRecognitionModelInstalled}");
            if (!IsDetectionModelInstalled || !IsRecognitionModelInstalled)
            {
                InstallProgress = 90;
                InstallMessage = "检查模型文件...";

                IsDetectionModelInstalled = _envService.CheckDetectionModelInstalled();
                IsRecognitionModelInstalled = _envService.CheckRecognitionModelInstalled();
                Debug.WriteLine(
                    $"[DEBUG] After check - IsDetectionModelInstalled = {IsDetectionModelInstalled}, IsRecognitionModelInstalled = {IsRecognitionModelInstalled}");

                if (!IsDetectionModelInstalled || !IsRecognitionModelInstalled)
                {
                    InstallProgress = 90;
                    var result = MessageBoxWindow.Show(Application.Current.MainWindow,
                        "模型文件未导入。\n\n请从百度网盘下载模型文件，然后使用\"选择模型文件\"按钮导入。\n\n是否立即打开百度网盘？",
                        "需要导入模型",
                        MessageBoxButton.YesNo);

                    if (result == MessageBoxResult.Yes) OpenBaiduPan();
                    return;
                }
            }
            else
            {
                // 模型已安装，跳过
                InstallProgress = 90;
                InstallMessage = "模型文件已安装，跳过...";
                await Task.Delay(300);
            }

            UpdateEnvironmentStatus();
            InstallProgress = 100;
            InstallMessage = "OCR环境安装完成！";
            Debug.WriteLine("[DEBUG] Installation completed successfully");
        }
        catch (Exception ex)
        {
            InstallMessage = $"安装失败: {ex.Message}";
            Debug.WriteLine($"[DEBUG] Installation failed: {ex.Message}");
        }
        finally
        {
            IsInstalling = false;
            ShowPauseButton = false;
            Debug.WriteLine("[DEBUG] IsInstalling set to false");
        }
    }

    /// <summary>
    ///     浏览Python路径命令
    /// </summary>
    [RelayCommand]
    private void BrowsePythonPath()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Python可执行文件|python.exe|所有文件|*.*",
            Title = "选择Python可执行文件"
        };

        if (dialog.ShowDialog() == true) PythonPath = dialog.FileName;
    }

    /// <summary>
    ///     选择测试图片命令
    /// </summary>
    [RelayCommand]
    private void SelectTestImage()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp|所有文件|*.*",
            Title = "选择测试图片"
        };

        if (dialog.ShowDialog() == true)
        {
            TestImagePath = dialog.FileName;
            TestResult = "";
        }
    }

    /// <summary>
    ///     暂停/恢复下载命令
    /// </summary>
    [RelayCommand]
    private async Task ToggleDownloadPause()
    {
        if (_downloadService.CurrentState == DownloadState.Downloading)
        {
            // 暂停下载
            _downloadService.PauseDownload();
            IsDownloadPaused = true;
            DownloadButtonText = "▶️ 恢复下载";
            InstallMessage = $"下载已暂停 ({InstallProgress}%)";
        }
        else if (_downloadService.CurrentState == DownloadState.Paused)
        {
            // 恢复下载
            IsDownloadPaused = false;
            DownloadButtonText = "⏸️ 暂停下载";
            InstallMessage = "正在恢复下载...";

            var progress = new Progress<(long downloaded, long total, string message)>(p =>
            {
                InstallProgress = (int)((double)p.downloaded / p.total * 50); // 下载占50%
                InstallMessage = p.message;
            });

            var success = await _downloadService.ResumeDownloadAsync(progress);

            if (success)
            {
                // 下载完成，继续安装
                InstallProgress = 50;
                InstallMessage = "正在安装Python...";

                // 执行安装
                var installSuccess = await InstallDownloadedPythonAsync();

                if (installSuccess)
                {
                    InstallProgress = 100;
                    InstallMessage = "Python安装完成！";
                    MessageBoxWindow.Show(Application.Current.MainWindow, "Python安装成功！", "安装成功");

                    // 更新Python状态
                    var (installed, version, isVersionValid, pythonPath) = await _envService.CheckPythonInstalled();
                    IsPythonInstalled = installed;
                    PythonVersion = installed ? version : "未安装";
                    if (installed && !string.IsNullOrEmpty(pythonPath)) PythonPath = pythonPath;
                    UpdateEnvironmentStatus();
                }
                else
                {
                    InstallMessage = "Python安装失败";
                    MessageBoxWindow.Show(Application.Current.MainWindow, "Python安装失败，请重试或手动安装", "安装失败",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }

                ShowPauseButton = false;
                IsInstalling = false;
            }
        }
    }

    /// <summary>
    ///     安装已下载的Python
    /// </summary>
    private async Task<bool> InstallDownloadedPythonAsync()
    {
        try
        {
            var task = _settingsService.Config.PythonDownloadTask;
            if (task == null || !File.Exists(task.FilePath)) return false;

            // 静默安装Python
            var processInfo = new ProcessStartInfo
            {
                FileName = task.FilePath,
                Arguments = "/quiet InstallAllUsers=0 PrependPath=1 Include_test=0",
                UseShellExecute = true,
                Verb = "runas" // 以管理员身份运行
            };

            using var process = Process.Start(processInfo);
            if (process == null) return false;

            await process.WaitForExitAsync();

            // 清理临时文件
            try
            {
                File.Delete(task.FilePath);
            }
            catch
            {
            }

            // 清除下载任务
            _settingsService.Config.PythonDownloadTask = null;
            _settingsService.SaveConfig(_settingsService.Config);

            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"安装Python失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    ///     窗口关闭时暂停下载
    /// </summary>
    public void OnWindowClosing()
    {
        if (_downloadService.CurrentState == DownloadState.Downloading) _downloadService.PauseDownload();
    }

    /// <summary>
    ///     测试OCR命令
    /// </summary>
    [RelayCommand]
    private async Task TestOcr()
    {
        if (string.IsNullOrEmpty(TestImagePath))
        {
            MessageBoxWindow.Show(Application.Current.MainWindow, "请先选择测试图片");
            return;
        }

        if (!IsEnvironmentReady)
        {
            var result = MessageBoxWindow.Show(Application.Current.MainWindow,
                "OCR环境未就绪，是否先检查环境？", "提示",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes) await CheckEnvironment();
            return;
        }

        IsTesting = true;
        IsInstalling = true;
        InstallProgress = 0;
        InstallMessage = "正在初始化OCR引擎...";
        TestResult = "";
        TestSuccess = false;

        try
        {
            var progress = new Progress<string>(msg =>
            {
                InstallMessage = msg;
                // 根据消息更新进度
                if (msg.Contains("初始化")) InstallProgress = 20;
                else if (msg.Contains("执行")) InstallProgress = 50;
                else if (msg.Contains("解析")) InstallProgress = 80;
                else if (msg.Contains("完成")) InstallProgress = 100;
            });

            // 使用当前界面的配置进行测试（不保存到文件）
            var currentConfig = GetCurrentConfig();
            var results = await _recognitionService.RecognizeAsync(TestImagePath, progress, currentConfig);

            if (results.Count == 0)
            {
                TestResult = "未能识别到任何文本";
                TestSuccess = false;
            }
            else
            {
                // 计算平均置信度
                double avgScore = 0;
                foreach (var r in results) avgScore += r.Score;
                avgScore /= results.Count;

                var sb = new StringBuilder();
                sb.AppendLine($"识别成功！共识别 {results.Count} 个文本区域");
                sb.AppendLine($"平均置信度: {avgScore:P2}");
                sb.AppendLine();
                sb.AppendLine("识别内容:");

                foreach (var result in results) sb.AppendLine($"- {result.Text} (置信度: {result.Score:P2})");

                TestResult = sb.ToString();
                TestSuccess = true;
            }
        }
        catch (Exception ex)
        {
            TestResult = $"识别失败: {ex.Message}";
            TestSuccess = false;
        }
        finally
        {
            IsTesting = false;
            IsInstalling = false;
        }
    }

    /// <summary>
    ///     保存设置命令
    /// </summary>
    [RelayCommand]
    private async Task SaveSettings()
    {
        await SaveSettingsInternalAsync(true);
    }

    /// <summary>
    ///     保存设置内部实现
    /// </summary>
    private async Task SaveSettingsInternalAsync(bool showMessage)
    {
        // 获取设置窗口作为父窗口
        var settingsWindow = Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(w => w.DataContext is SettingsViewModel);

        // 校验置信度阈值
        if (ConfidenceThreshold < 0 || ConfidenceThreshold > 1)
        {
            MessageBoxWindow.Show(settingsWindow, "置信度阈值必须在0-1之间", "校验失败", MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        // 校验最大图片尺寸
        if (MaxImageSize < 100 || MaxImageSize > 4096)
        {
            MessageBoxWindow.Show(settingsWindow, "最大图片尺寸必须在100-4096像素之间", "校验失败", MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        try
        {
            var config = GetCurrentConfig();
            _settingsService.SaveConfig(config);
            _originalConfig = GetCurrentConfig();

            if (showMessage) MessageBoxWindow.Show(settingsWindow, "OCR设置已保存", "成功");
        }
        catch (Exception ex)
        {
            MessageBoxWindow.Show(settingsWindow, $"保存失败：{ex.Message}", "错误", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    /// <summary>
    ///     恢复默认设置命令
    /// </summary>
    [RelayCommand]
    private async Task RestoreDefaults()
    {
        // 获取设置窗口作为父窗口
        var settingsWindow = Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(w => w.DataContext is SettingsViewModel);

        var result = MessageBoxWindow.Show(settingsWindow, "确定要恢复默认OCR设置吗？", "确认", MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes)
            return;

        var defaultConfig = _settingsService.GetDefaultConfig();

        PythonPath = defaultConfig.PythonPath;
        SelectedModel = defaultConfig.SelectedModel;
        UseGpu = defaultConfig.UseGpu;
        ConfidenceThreshold = defaultConfig.ConfidenceThreshold;
        EnableImagePreprocessing = defaultConfig.EnableImagePreprocessing;
        AutoRotateImage = defaultConfig.AutoRotateImage;
        MaxImageSize = defaultConfig.MaxImageSize;

        MessageBoxWindow.Show(settingsWindow, "已恢复默认设置，请点击保存设置按钮保存更改。");
    }

    #endregion

    #region ISettingsViewModel 实现

    /// <summary>
    ///     重新加载设置（放弃更改）
    /// </summary>
    public void ReloadSettings()
    {
        _settingsService.RefreshConfig();
        LoadConfig();
    }

    /// <summary>
    ///     保存设置（接口实现）
    /// </summary>
    async Task ISettingsViewModel.SaveSettingsAsync(bool showMessage)
    {
        await SaveSettingsInternalAsync(showMessage);
    }

    #endregion
}