using GuiPiao.Model;
using GuiPiao.Utils;

namespace GuiPiao.Services;

/// <summary>
///     地图设置服务
/// </summary>
public class MapSettingsService
{
    private const string ConfigFileName = "mapsettings.json";

    public MapSettingsService()
    {
        Config = LoadConfig();
    }

    public MapSettingsConfig Config { get; private set; }

    /// <summary>
    ///     从JSON文件加载配置，如果不存在则使用默认值
    /// </summary>
    private MapSettingsConfig LoadConfig()
    {
        var config = JsonConfigManager.Instance.LoadConfig(ConfigFileName, new MapSettingsConfig());
        return config;
    }

    /// <summary>
    ///     保存配置到JSON文件
    /// </summary>
    public void SaveConfig(MapSettingsConfig config)
    {
        Config = config;
        JsonConfigManager.Instance.SaveConfig(ConfigFileName, config);
    }

    /// <summary>
    ///     获取默认配置
    /// </summary>
    public MapSettingsConfig GetDefaultConfig()
    {
        return new MapSettingsConfig();
    }

    /// <summary>
    ///     刷新配置（重新从文件加载）
    /// </summary>
    public void RefreshConfig()
    {
        Config = LoadConfig();
    }
}