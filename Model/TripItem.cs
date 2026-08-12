using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using GuiPiao.Utils;

namespace GuiPiao.Model;

public partial class TripItem : ObservableObject
{
    // UI状态（不保存到数据库）
    [ObservableProperty] private bool _isSelected;

    // 核心列（默认显示）
    public int Id { get; set; }

    /// <summary>
    ///     数据库真实ID（用于批量更新等操作）
    /// </summary>
    public int DatabaseId { get; set; }

    [ObservableProperty] private string _trainNo = string.Empty;
    [ObservableProperty] private string _departStation = string.Empty;
    [ObservableProperty] private string _arriveStation = string.Empty;
    [ObservableProperty] private string _departDate = string.Empty;
    [ObservableProperty] private string _departTime = string.Empty;
    [ObservableProperty] private string _arriveTime = string.Empty;
    [ObservableProperty] private int _arriveDayOffset;
    [ObservableProperty] private string _seatType = string.Empty;
    [ObservableProperty] private string _money = string.Empty;
    public int Status { get; set; }

    /// <summary>
    ///     到达时间展示（含跨天，如「04:52(+1)」）
    /// </summary>
    public string ArriveTimeDisplay => ArriveTimeFormat.Format(ArriveTime, ArriveDayOffset);

    /// <summary>卡片用跨天角标（+1 / +2，当日为空）。</summary>
    public string ArriveDayOffsetBadge => ArriveTimeFormat.FormatBadge(ArriveDayOffset);

    partial void OnArriveTimeChanged(string value) => OnPropertyChanged(nameof(ArriveTimeDisplay));

    partial void OnArriveDayOffsetChanged(int value)
    {
        OnPropertyChanged(nameof(ArriveTimeDisplay));
        OnPropertyChanged(nameof(ArriveDayOffsetBadge));
    }

    /// <summary>
    ///     状态显示文本（中文）
    /// </summary>
    public string StatusDisplay
    {
        get
        {
            return Status switch
            {
                0 => "未出行",
                1 => "已完成",
                2 => "已改签",
                3 => "已退票",
                _ => "未知"
            };
        }
    }

    /// <summary>
    ///     标签列表（用于UI展示）
    /// </summary>
    public List<TicketTag> Tags { get; set; } = new();

    // 可选列（票面信息）
    [ObservableProperty] private string _coachNo = string.Empty;
    [ObservableProperty] private string _seatNo = string.Empty;
    [ObservableProperty] private string _ticketNumber = string.Empty;
    [ObservableProperty] private string _departStationPinyin = string.Empty;
    [ObservableProperty] private string _arriveStationPinyin = string.Empty;
    [ObservableProperty] private string _checkInLocation = string.Empty;
    [ObservableProperty] private string _hint = string.Empty;
    [ObservableProperty] private string _additionalInfo = string.Empty;
    [ObservableProperty] private string _ticketPurpose = string.Empty;
    [ObservableProperty] private string _ticketModificationType = string.Empty;

    // 票种类型和支付渠道
    [ObservableProperty] private string _ticketType = string.Empty;
    [ObservableProperty] private string _paymentChannel = string.Empty;
}
