namespace GuiPiao.Messages;

/// <summary>
///     日志颜色设置变更消息
/// </summary>
public class LogColorsChangedMessage
{
    public LogColorsChangedMessage(string infoColor, string warningColor, string errorColor, string fatalColor)
    {
        InfoColor = infoColor;
        WarningColor = warningColor;
        ErrorColor = errorColor;
        FatalColor = fatalColor;
    }

    public string InfoColor { get; }
    public string WarningColor { get; }
    public string ErrorColor { get; }
    public string FatalColor { get; }
}