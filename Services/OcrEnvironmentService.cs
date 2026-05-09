using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace GuiPiao.Services;

/// <summary>
///     模型类型
/// </summary>
public enum ModelType
{
    Detection, // 文本检测模型
    Recognition // 文字识别模型
}

/// <summary>
///     OCR环境管理服务 - 负责Python环境、CnOCR库和模型的检测与安装
/// </summary>
public class OcrEnvironmentService
{
    // Python下载地址 (Python 3.12.9)
    private const string PythonDownloadUrl = "https://www.python.org/ftp/python/3.12.9/python-3.12.9-amd64.exe";

    // 模型文件目标路径
    private static string CnocrModelDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "cnocr", "2.3", "densenet_lite_136-gru");

    private static string CnstdModelDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "cnstd", "1.2", "ppocr", "ch_PP-OCRv4_det");

    /// <summary>
    ///     检查Python是否已安装，并返回版本信息
    ///     支持检测任意版本，但建议 >= 3.8
    /// </summary>
    public async Task<(bool installed, string version, bool isVersionValid, string pythonPath)> CheckPythonInstalled()
    {
        // 尝试多个可能的Python命令
        string[] pythonCommands = { "python", "python3", "py" };

        foreach (var cmd in pythonCommands)
            try
            {
                // 先获取Python的完整路径
                var fullPath = await GetPythonFullPathAsync(cmd);
                if (string.IsNullOrEmpty(fullPath)) fullPath = cmd; // 如果获取不到完整路径，使用命令名

                var processInfo = new ProcessStartInfo
                {
                    FileName = cmd,
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                if (process == null) continue;

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    // Python版本信息通常输出到stderr
                    var versionText = string.IsNullOrEmpty(output) ? error : output;
                    versionText = versionText.Trim();

                    // 解析版本号
                    var versionInfo = ParsePythonVersion(versionText);
                    var isValid = versionInfo.major > 3 || (versionInfo.major == 3 && versionInfo.minor >= 8);

                    if (isValid) return (true, versionText, true, fullPath);

                    return (true, versionText, false, fullPath);
                }
            }
            catch
            {
                // 继续尝试下一个命令
            }

        return (false, "", false, "");
    }

    /// <summary>
    ///     获取Python可执行文件的完整路径
    /// </summary>
    private async Task<string> GetPythonFullPathAsync(string pythonCommand)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "where",
                Arguments = pythonCommand,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null) return string.Empty;

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            await Task.WhenAll(outputTask, errorTask);
            await process.WaitForExitAsync();

            var output = outputTask.Result;
            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                // where命令可能返回多个路径，取第一个
                var paths = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (paths.Length > 0) return paths[0].Trim();
            }
        }
        catch
        {
            // 忽略错误
        }

        return string.Empty;
    }

    /// <summary>
    ///     解析Python版本字符串
    /// </summary>
    private (int major, int minor) ParsePythonVersion(string versionText)
    {
        try
        {
            // 格式: "Python 3.12.9" 或 "Python 3.8.0"
            var parts = versionText.Split(' ');
            if (parts.Length >= 2)
            {
                var versionParts = parts[1].Split('.');
                if (versionParts.Length >= 2 &&
                    int.TryParse(versionParts[0], out var major) &&
                    int.TryParse(versionParts[1], out var minor))
                    return (major, minor);
            }
        }
        catch
        {
        }

        return (0, 0);
    }

    /// <summary>
    ///     检查CnOCR是否已安装
    /// </summary>
    public async Task<bool> CheckCnocrInstalled()
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = "-c \"import cnocr; print('OK')\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null) return false;

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            await Task.WhenAll(outputTask, errorTask);
            await process.WaitForExitAsync();

            return process.ExitCode == 0 && outputTask.Result.Contains("OK");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"检查CnOCR失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    ///     检查CNSTD是否已安装
    /// </summary>
    public async Task<bool> CheckCnstdInstalled()
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = "-c \"import cnstd; print('OK')\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null) return false;

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            await Task.WhenAll(outputTask, errorTask);
            await process.WaitForExitAsync();

            return process.ExitCode == 0 && outputTask.Result.Contains("OK");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"检查CNSTD失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    ///     检查识别模型文件是否存在
    /// </summary>
    public bool CheckRecognitionModelInstalled()
    {
        var modelPath = Path.Combine(CnocrModelDir, "cnocr-v2.3-densenet_lite_136-gru-epoch=004-ft-model.onnx");
        return File.Exists(modelPath);
    }

    /// <summary>
    ///     检查检测模型文件是否存在
    /// </summary>
    public bool CheckDetectionModelInstalled()
    {
        var modelPath = Path.Combine(CnstdModelDir, "ch_PP-OCRv4_det_infer.onnx");
        return File.Exists(modelPath);
    }

    /// <summary>
    ///     检查识别模型是否可用（通过测试CnOCR初始化）
    ///     注意：这会触发自动下载，慎用
    /// </summary>
    public async Task<bool> CheckRecognitionModelAvailable()
    {
        try
        {
            // 先检查文件是否存在
            if (!CheckRecognitionModelInstalled())
                return false;

            // 使用Python测试CnOCR是否能正常初始化
            var processInfo = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = "-c \"from cnocr import CnOcr; ocr = CnOcr(); print('OK')\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null) return false;

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            await Task.WhenAll(outputTask, errorTask);
            await process.WaitForExitAsync();

            return process.ExitCode == 0 && outputTask.Result.Contains("OK");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"检查识别模型失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    ///     检查检测模型是否可用（通过测试CNSTD初始化）
    ///     注意：这会触发自动下载，慎用
    /// </summary>
    public async Task<bool> CheckDetectionModelAvailable()
    {
        try
        {
            // 先检查文件是否存在
            if (!CheckDetectionModelInstalled())
                return false;

            // 使用Python测试CNSTD是否能正常初始化
            var processInfo = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = "-c \"from cnstd import CnStd; det = CnStd(); print('OK')\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null) return false;

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            await Task.WhenAll(outputTask, errorTask);
            await process.WaitForExitAsync();

            return process.ExitCode == 0 && outputTask.Result.Contains("OK");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"检查检测模型失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    ///     获取模型下载提示信息
    /// </summary>
    public string GetModelDownloadInstructions()
    {
        return $"请从以下地址下载模型文件：\n\n" +
               $"1. 识别模型（约12MB）：\n" +
               $"   文件名：cnocr-v2.3-densenet_lite_136-gru-epoch=004-ft-model.onnx\n" +
               $"   放置位置：{CnocrModelDir}\n\n" +
               $"2. 检测模型（约4MB）：\n" +
               $"   文件名：ch_PP-OCRv4_det_infer.onnx\n" +
               $"   放置位置：{CnstdModelDir}\n\n" +
               $"百度网盘下载：\n" +
               $"   链接：https://pan.baidu.com/s/1RhLBf8DcLnLuGLPrp89hUg?pwd=nocr\n" +
               $"   提取码：nocr\n\n" +
               $"下载后使用选择模型文件按钮导入即可。";
    }

    /// <summary>
    ///     导入本地模型文件到正确位置
    /// </summary>
    public async Task<bool> ImportModelFile(string sourcePath, ModelType modelType)
    {
        try
        {
            if (!File.Exists(sourcePath)) throw new FileNotFoundException("模型文件不存在", sourcePath);

            var targetDir = modelType == ModelType.Detection ? CnstdModelDir : CnocrModelDir;
            var fileName = Path.GetFileName(sourcePath);
            var targetPath = Path.Combine(targetDir, fileName);

            // 确保目标目录存在
            Directory.CreateDirectory(targetDir);

            // 如果目标文件已存在，先删除
            if (File.Exists(targetPath)) File.Delete(targetPath);

            // 复制文件
            await Task.Run(() => File.Copy(sourcePath, targetPath, true));

            // 验证文件大小
            var fileInfo = new FileInfo(targetPath);
            if (fileInfo.Length < 1024)
            {
                File.Delete(targetPath);
                throw new Exception("导入的文件太小，可能不是有效的模型文件");
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"导入模型文件失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    ///     下载并安装Python
    /// </summary>
    public async Task<bool> DownloadAndInstallPython(IProgress<(int progress, string message)>? progress = null)
    {
        try
        {
            progress?.Report((0, "正在下载Python 3.12.9安装包..."));

            var tempPath = Path.Combine(Path.GetTempPath(), "python-3.12.9-amd64.exe");

            // 下载安装包
            using var client = new WebClient();
            client.DownloadProgressChanged += (s, e) =>
            {
                var percent = e.ProgressPercentage / 2; // 下载占50%
                progress?.Report((percent, $"正在下载Python... {e.ProgressPercentage}%"));
            };

            await client.DownloadFileTaskAsync(PythonDownloadUrl, tempPath);

            progress?.Report((50, "正在安装Python..."));

            // 静默安装Python
            var processInfo = new ProcessStartInfo
            {
                FileName = tempPath,
                Arguments = "/quiet InstallAllUsers=0 PrependPath=1 Include_test=0",
                UseShellExecute = true,
                Verb = "runas" // 以管理员身份运行
            };

            using var process = Process.Start(processInfo);
            if (process == null)
            {
                progress?.Report((0, "安装程序启动失败"));
                return false;
            }

            await process.WaitForExitAsync();

            // 清理临时文件
            try
            {
                File.Delete(tempPath);
            }
            catch
            {
            }

            progress?.Report((100, "Python安装完成"));
            return true;
        }
        catch (Exception ex)
        {
            progress?.Report((0, $"安装失败: {ex.Message}"));
            return false;
        }
    }

    /// <summary>
    ///     安装CnOCR和CNSTD
    /// </summary>
    public async Task<bool> InstallCnocrAndCnstd(IProgress<(int progress, string message)>? progress = null)
    {
        try
        {
            progress?.Report((0, "正在安装CnOCR和CNSTD..."));

            // 使用pip安装CnOCR和CNSTD
            var processInfo = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = "-m pip install cnocr[ort-cpu] cnstd -i https://pypi.tuna.tsinghua.edu.cn/simple",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
            {
                progress?.Report((0, "无法启动pip安装"));
                return false;
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                progress?.Report((100, "CnOCR和CNSTD安装完成"));
                return true;
            }

            progress?.Report((0, $"安装失败: {error}"));
            return false;
        }
        catch (Exception ex)
        {
            progress?.Report((0, $"安装失败: {ex.Message}"));
            return false;
        }
    }
}