using System;
using GuiPiao.Model;
using GuiPiao.Utils;

namespace GuiPiao.Services;

/// <summary>
///     仪表盘设置服务
/// </summary>
public class DashboardSettingsService
{
    private const string ConfigFileName = "dashboardsettings.json";

    public DashboardSettingsService()
    {
        Config = LoadConfig();
    }

    public DashboardConfig Config { get; private set; }

    /// <summary>
    ///     配置保存事件
    /// </summary>
    public event EventHandler? ConfigSaved;

    /// <summary>
    ///     从JSON文件加载配置，如果不存在则使用默认值
    /// </summary>
    private DashboardConfig LoadConfig()
    {
        var config = JsonConfigManager.Instance.LoadConfig(ConfigFileName, new DashboardConfig());
        return config;
    }

    /// <summary>
    ///     保存配置到JSON文件
    /// </summary>
    public void SaveConfig(DashboardConfig config)
    {
        Config = config;
        JsonConfigManager.Instance.SaveConfig(ConfigFileName, config);

        // 触发配置保存事件
        ConfigSaved?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    ///     获取默认配置
    /// </summary>
    public DashboardConfig GetDefaultConfig()
    {
        return new DashboardConfig();
    }

    /// <summary>
    ///     刷新配置（重新从文件加载）
    /// </summary>
    public void RefreshConfig()
    {
        Config = LoadConfig();
    }

    /// <summary>
    ///     恢复默认配置
    /// </summary>
    public void ResetToDefault()
    {
        Config = new DashboardConfig();
        SaveConfig(Config);
    }
}