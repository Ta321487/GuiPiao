using GuiPiao.Model;
using GuiPiao.Utils;

namespace GuiPiao.Services;

/// <summary>同步设置（端口 / 是否允许局域网监听）。</summary>
public class SyncSettingsService
{
    private const string ConfigFileName = "syncsettings.json";

    public SyncSettingsService()
    {
        Config = JsonConfigManager.Instance.LoadConfig(ConfigFileName, new SyncSettingsConfig());
        if (Config.ListenPort is < 1 or > 65535)
            Config.ListenPort = SyncHttpServer.DefaultPort;
    }

    public SyncSettingsConfig Config { get; private set; }

    public void Save()
    {
        JsonConfigManager.Instance.SaveConfig(ConfigFileName, Config);
    }

    public void Reload()
    {
        Config = JsonConfigManager.Instance.LoadConfig(ConfigFileName, new SyncSettingsConfig());
        if (Config.ListenPort is < 1 or > 65535)
            Config.ListenPort = SyncHttpServer.DefaultPort;
    }
}
