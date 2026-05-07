using GuiPiao.Model;
using GuiPiao.Utils;

namespace GuiPiao.Services;

/// <summary>
///     OCR设置服务
/// </summary>
public class OcrSettingsService
{
    private const string ConfigFileName = "ocrsettings.json";

    public OcrSettingsService()
    {
        Config = LoadConfig();
    }

    public OcrConfig Config { get; private set; }

    /// <summary>
    ///     从JSON文件加载配置，如果不存在则使用默认值
    /// </summary>
    private OcrConfig LoadConfig()
    {
        var config = JsonConfigManager.Instance.LoadConfig(ConfigFileName, new OcrConfig());
        return config;
    }

    /// <summary>
    ///     保存配置到JSON文件
    /// </summary>
    public void SaveConfig(OcrConfig config)
    {
        Config = config;
        JsonConfigManager.Instance.SaveConfig(ConfigFileName, config);
    }

    /// <summary>
    ///     获取默认配置
    /// </summary>
    public OcrConfig GetDefaultConfig()
    {
        return new OcrConfig();
    }

    /// <summary>
    ///     刷新配置（重新从文件加载）
    /// </summary>
    public void RefreshConfig()
    {
        Config = LoadConfig();
    }
}