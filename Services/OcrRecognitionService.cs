using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using GuiPiao.Model;
using GuiPiao.Models;
using Newtonsoft.Json;

namespace GuiPiao.Services;

/// <summary>
///     OCR识别服务 - 执行实际的OCR识别
/// </summary>
public class OcrRecognitionService
{
    private readonly OcrSettingsService _settingsService;

    public OcrRecognitionService()
    {
        _settingsService = new OcrSettingsService();
    }

    /// <summary>
    ///     执行OCR识别
    /// </summary>
    /// <param name="imagePath">图片路径</param>
    /// <param name="progress">进度报告</param>
    /// <param name="config">OCR配置（为null时使用默认配置）</param>
    /// <returns>OCR结果列表</returns>
    public async Task<List<OcrResult>> RecognizeAsync(string imagePath, IProgress<string>? progress = null,
        OcrConfig? config = null)
    {
        if (config == null)
        {
            _settingsService.RefreshConfig();
            config = _settingsService.Config;
        }

        try
        {
            progress?.Report("正在初始化OCR引擎...");

            // 生成Python脚本
            var script = GenerateOcrScript(imagePath, config);
            var tempScriptPath = Path.Combine(Path.GetTempPath(), $"ocr_{Guid.NewGuid()}.py");

            try
            {
                // 写入临时脚本
                await File.WriteAllTextAsync(tempScriptPath, script, Encoding.UTF8);
                progress?.Report("正在执行OCR识别...");

                // 执行Python脚本
                var processInfo = new ProcessStartInfo
                {
                    FileName = config.PythonPath,
                    Arguments = tempScriptPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                using var process = Process.Start(processInfo);
                if (process == null) throw new Exception("无法启动Python进程");

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0) throw new Exception($"OCR执行失败: {error}");

                progress?.Report("正在解析识别结果...");

                // 解析结果
                var results = ParseOcrResults(output);
                progress?.Report($"识别完成，共识别 {results.Count} 个文本区域");

                return results;
            }
            finally
            {
                // 清理临时文件
                try
                {
                    File.Delete(tempScriptPath);
                }
                catch
                {
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"OCR识别失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    ///     生成OCR Python脚本
    /// </summary>
    private string GenerateOcrScript(string imagePath, OcrConfig config)
    {
        // 根据CnOCR文档生成脚本
        var device = config.UseGpu ? "'cuda'" : "'cpu'";
        var confidenceThreshold = config.ConfidenceThreshold;
        var autoRotate = config.AutoRotateImage;
        var enablePreprocessing = config.EnableImagePreprocessing;

        return $@"
import sys
import io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8')

from cnocr import CnOcr
from cnstd import CnStd
import json
import numpy as np
from PIL import Image
import cv2

# 自定义JSON编码器处理numpy类型
class NumpyEncoder(json.JSONEncoder):
    def default(self, obj):
        if isinstance(obj, np.ndarray):
            return obj.tolist()
        if isinstance(obj, np.floating):
            return float(obj)
        if isinstance(obj, np.integer):
            return int(obj)
        return super().default(obj)

def preprocess_image(img_path):
    img = cv2.imread(img_path)
    if img is None:
        return None
    gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
    denoised = cv2.fastNlMeansDenoising(gray, None, 10, 7, 21)
    binary = cv2.adaptiveThreshold(denoised, 255, cv2.ADAPTIVE_THRESH_GAUSSIAN_C, cv2.THRESH_BINARY, 11, 2)
    temp_path = img_path + '.preprocessed.png'
    cv2.imwrite(temp_path, binary)
    return temp_path

def auto_rotate_image(img_path):
    img = Image.open(img_path)
    if img.width < img.height * 0.8:
        img = img.rotate(90, expand=True)
        temp_path = img_path + '.rotated.png'
        img.save(temp_path)
        return temp_path
    return img_path

try:
    img_fp = r'{imagePath.Replace("\\", "/")}'
    processed_img = img_fp
    
    # 自动旋转
    {(autoRotate ? "processed_img = auto_rotate_image(processed_img)" : "")}
    
    # 图像预处理
    {(enablePreprocessing ? "processed_img = preprocess_image(processed_img) or processed_img" : "")}

    # 初始化检测器 (CnSTD)
    detector = CnStd(
        model_name='ch_PP-OCRv4_det',
        rotated_bbox=True,
        det_model_backend='onnx'
    )

    # 创建OCR识别器 (CnOCR)
    ocr = CnOcr(
        rec_model_name='{config.SelectedModel}',
        rec_model_backend='onnx',
        det_model_name='ch_PP-OCRv4_det',
        det_model_backend='onnx',
        context={device}
    )

    # 执行OCR识别
    result = ocr.ocr(processed_img)

    # 转换为标准格式并过滤低置信度结果
    output = []
    for item in result:
        if isinstance(item, dict):
            score = float(item.get('score', 0))
            # 根据置信度阈值过滤
            if score >= {confidenceThreshold}:
                output.append({{
                    'text': item.get('text', ''),
                    'score': score,
                    'position': item.get('position', [])
                }})
        else:
            output.append({{
                'text': str(item),
                'score': 0.95,
                'position': []
            }})

    print(json.dumps(output, ensure_ascii=False, cls=NumpyEncoder))
except Exception as e:
    print(json.dumps({{'error': str(e)}}))
";
    }

    /// <summary>
    ///     解析OCR结果
    /// </summary>
    private List<OcrResult> ParseOcrResults(string json)
    {
        try
        {
            // 查找JSON部分
            var jsonStart = json.IndexOf('[');
            var jsonEnd = json.LastIndexOf(']');

            if (jsonStart >= 0 && jsonEnd > jsonStart) json = json.Substring(jsonStart, jsonEnd - jsonStart + 1);

            var results = JsonConvert.DeserializeObject<List<OcrResult>>(json);
            return results ?? new List<OcrResult>();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"解析OCR结果失败: {ex.Message}");
            return new List<OcrResult>();
        }
    }

    /// <summary>
    ///     测试OCR功能
    /// </summary>
    public async Task<(bool success, string message, List<OcrResult>? results)> TestOcrAsync(string imagePath)
    {
        try
        {
            var results = await RecognizeAsync(imagePath);

            if (results.Count == 0) return (false, "未能识别到任何文本", null);

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

            return (true, sb.ToString(), results);
        }
        catch (Exception ex)
        {
            return (false, $"识别失败: {ex.Message}", null);
        }
    }
}