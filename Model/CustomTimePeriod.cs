namespace GuiPiao.Model;

/// <summary>
///     自定义时间段配置
/// </summary>
public class CustomTimePeriod
{
    /// <summary>
    ///     时段名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     开始小时 (0-23)
    /// </summary>
    public int StartHour { get; set; }

    /// <summary>
    ///     开始分钟 (0-59)
    /// </summary>
    public int StartMinute { get; set; }

    /// <summary>
    ///     结束小时 (0-24)
    /// </summary>
    public int EndHour { get; set; }

    /// <summary>
    ///     结束分钟 (0-59)
    /// </summary>
    public int EndMinute { get; set; }

    /// <summary>
    ///     显示颜色 (Hex格式，如 #1976D2)
    /// </summary>
    public string Color { get; set; } = "#1976D2";

    /// <summary>
    ///     获取开始时间的总分钟数
    /// </summary>
    public int StartTotalMinutes => StartHour * 60 + StartMinute;

    /// <summary>
    ///     获取结束时间的总分钟数
    /// </summary>
    public int EndTotalMinutes => EndHour * 60 + EndMinute;

    /// <summary>
    ///     获取时长（分钟）
    /// </summary>
    public int DurationMinutes => EndTotalMinutes - StartTotalMinutes;

    /// <summary>
    ///     获取格式化的时间范围字符串
    /// </summary>
    public string TimeRange => $"{StartHour:D2}:{StartMinute:D2} - {EndHour:D2}:{EndMinute:D2}";

    /// <summary>
    ///     验证时段是否有效
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Name)
               && StartHour >= 0 && StartHour < 24
               && StartMinute >= 0 && StartMinute < 60
               && EndHour >= 0 && EndHour <= 24
               && EndMinute >= 0 && EndMinute < 60
               && (EndHour > StartHour || (EndHour == StartHour && EndMinute > StartMinute));
    }

    /// <summary>
    ///     检查指定时间是否在该时段内
    /// </summary>
    public bool ContainsTime(int hour, int minute)
    {
        var totalMinutes = hour * 60 + minute;
        return totalMinutes >= StartTotalMinutes && totalMinutes < EndTotalMinutes;
    }
}