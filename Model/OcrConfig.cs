using System;

namespace GuiPiao.Model;

/// <summary>
///     下载状态
/// </summary>
public enum DownloadState
{
    Idle,
    Downloading,
    Paused,
    Completed,
    Failed
}

/// <summary>
///     Python下载任务信息
/// </summary>
public class PythonDownloadTask
{
    public string Url { get; set; } = "";
    public string FilePath { get; set; } = "";
    public long TotalBytes { get; set; }
    public long DownloadedBytes { get; set; }
    public DownloadState State { get; set; } = DownloadState.Idle;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? CompletedAt { get; set; }
    public string ErrorMessage { get; set; } = "";
}

/// <summary>
///     OCR配置模型
/// </summary>
public class OcrConfig
{
    /// <summary>
    ///     Python可执行文件路径
    /// </summary>
    public string PythonPath { get; set; } = "python";

    /// <summary>
    ///     选择的识别模型
    /// </summary>
    public string SelectedModel { get; set; } = "densenet_lite_136-gru";

    /// <summary>
    ///     是否使用GPU
    /// </summary>
    public bool UseGpu { get; set; } = false;

    /// <summary>
    ///     置信度阈值
    /// </summary>
    public double ConfidenceThreshold { get; set; } = 0.85;

    /// <summary>
    ///     启用图片预处理
    /// </summary>
    public bool EnableImagePreprocessing { get; set; } = true;

    /// <summary>
    ///     自动旋转图片
    /// </summary>
    public bool AutoRotateImage { get; set; } = true;

    /// <summary>
    ///     最大图片尺寸
    /// </summary>
    public int MaxImageSize { get; set; } = 1920;

    /// <summary>
    ///     Python下载任务（用于断点续传）
    /// </summary>
    public PythonDownloadTask? PythonDownloadTask { get; set; }
}