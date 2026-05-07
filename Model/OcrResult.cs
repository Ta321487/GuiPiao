using System.Collections.Generic;
using Newtonsoft.Json;

namespace GuiPiao.Models;

/// <summary>
///     OCR识别结果模型
/// </summary>
public class OcrResult
{
    /// <summary>
    ///     识别文本
    /// </summary>
    [JsonProperty("text")]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    ///     置信度 (0-1)
    /// </summary>
    [JsonProperty("score")]
    public double Score { get; set; }

    /// <summary>
    ///     文本位置坐标
    /// </summary>
    [JsonProperty("position")]
    public List<List<double>>? Position { get; set; }
}