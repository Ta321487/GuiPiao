namespace GuiPiao.Mobile.Model;

/// <summary>与 PC Model/GeneralConfig 同源枚举（主题设置不同步）。</summary>
public enum ThemeMode
{
    Light = 0,
    Dark = 1,
    System = 2
}

/// <summary>与 PC AccentColor 预设一致。</summary>
public enum AccentColor
{
    MicrosoftBlue = 0,
    FreshGreen = 1,
    VitalityOrange = 2,
    DarkPurple = 3,
    MinimalGray = 4,
    Custom = 5
}

/// <summary>与 PC FontSizeOption 一致（本机，不同步）。</summary>
public enum FontSizeOption
{
    Small = 0,
    Medium = 1,
    Large = 2,
    ExtraLarge = 3
}

/// <summary>手机本机外观与操作偏好（存 AppData，不同步）。</summary>
public class AppearanceConfig
{
    public ThemeMode ThemeMode { get; set; } = ThemeMode.Light;
    public AccentColor AccentColor { get; set; } = AccentColor.MicrosoftBlue;
    public string CustomColor { get; set; } = "#0078D4";
    /// <summary>全局字号档位；默认中（14）。</summary>
    public FontSizeOption FontSize { get; set; } = FontSizeOption.Medium;
    /// <summary>删除行程前是否弹确认；默认开。</summary>
    public bool ConfirmDelete { get; set; } = true;
}

/// <summary>同步客户端连接与水位（本机）。</summary>
public class SyncClientConfig
{
    public string BaseUrl { get; set; } = "http://127.0.0.1:17880";
    public string DeviceName { get; set; } = "GuiPiao Mobile";
    public string? DeviceId { get; set; }
    public string? DeviceToken { get; set; }
    public long LastPullSeq { get; set; }
}
