using System;
using System.Text;

namespace GuiPiao.Utils;

public static class CommonUtils
{
    /// <summary>
    ///     格式化日期时间
    /// </summary>
    /// <param name="dateTime">日期时间对象</param>
    /// <returns>格式化后的日期字符串 (YYYY-MM-DD)</returns>
    public static string FormatDate(DateTime dateTime)
    {
        return dateTime.ToString("yyyy-MM-dd");
    }

    /// <summary>
    ///     格式化时间
    /// </summary>
    /// <param name="dateTime">日期时间对象</param>
    /// <returns>格式化后的时间字符串 (HH:MM)</returns>
    public static string FormatTime(DateTime dateTime)
    {
        return dateTime.ToString("HH:mm");
    }

    /// <summary>
    ///     解析日期字符串
    /// </summary>
    /// <param name="dateString">日期字符串 (YYYY-MM-DD)</param>
    /// <returns>日期时间对象</returns>
    public static DateTime ParseDate(string dateString)
    {
        return DateTime.ParseExact(dateString, "yyyy-MM-dd", null);
    }

    /// <summary>
    ///     生成唯一标识符
    /// </summary>
    /// <returns>唯一标识符</returns>
    public static string GenerateUniqueId()
    {
        return Guid.NewGuid().ToString("N");
    }

    /// <summary>
    ///     生成取票号
    /// </summary>
    /// <returns>取票号</returns>
    public static string GenerateTicketNumber()
    {
        return "T" + DateTime.Now.ToString("yyyyMMddHHmmss") + new Random().Next(1000, 9999);
    }

    /// <summary>
    ///     计算两个日期之间的天数差
    /// </summary>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <returns>天数差</returns>
    public static int CalculateDaysDifference(DateTime startDate, DateTime endDate)
    {
        return (endDate - startDate).Days;
    }

    /// <summary>
    ///     验证身份证号
    /// </summary>
    /// <param name="idCard">身份证号</param>
    /// <returns>是否有效</returns>
    public static bool ValidateIdCard(string idCard)
    {
        if (string.IsNullOrEmpty(idCard) || idCard.Length != 18)
            return false;

        // 简单验证，实际项目中需要更复杂的验证
        for (var i = 0; i < 17; i++)
            if (!char.IsDigit(idCard[i]))
                return false;

        var lastChar = idCard[17];
        return char.IsDigit(lastChar) || lastChar == 'X' || lastChar == 'x';
    }

    /// <summary>
    ///     隐藏敏感信息
    /// </summary>
    /// <param name="info">敏感信息</param>
    /// <param name="showLength">显示长度</param>
    /// <returns>隐藏后的信息</returns>
    public static string HideSensitiveInfo(string info, int showLength = 4)
    {
        if (string.IsNullOrEmpty(info) || info.Length <= showLength)
            return info;

        var sb = new StringBuilder();
        sb.Append(info.Substring(0, showLength));
        for (var i = showLength; i < info.Length; i++) sb.Append('*');
        return sb.ToString();
    }
}