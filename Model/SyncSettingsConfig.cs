namespace GuiPiao.Model;

/// <summary>同步服务偏好（AppData；不同步到手机）。</summary>
public class SyncSettingsConfig
{
    public int ListenPort { get; set; } = 17880;

    /// <summary>是否绑定局域网网卡。默认 true；失败时回退本机监听。</summary>
    public bool AllowLan { get; set; } = true;
}
