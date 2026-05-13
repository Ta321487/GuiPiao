using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

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

    public string TrainNo { get; set; }
    public string DepartStation { get; set; }
    public string ArriveStation { get; set; }
    public string DepartDate { get; set; }
    public string DepartTime { get; set; }
    public string SeatType { get; set; }
    public string Money { get; set; }
    public int Status { get; set; }

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
    public string CoachNo { get; set; }
    public string SeatNo { get; set; }
    public string TicketNumber { get; set; }
    public string DepartStationPinyin { get; set; }
    public string ArriveStationPinyin { get; set; }
    public string CheckInLocation { get; set; }
    public string Hint { get; set; }
    public string AdditionalInfo { get; set; }
    public string TicketPurpose { get; set; }
    public string TicketModificationType { get; set; }

    // 票种类型和支付渠道
    public string TicketType { get; set; }
    public string PaymentChannel { get; set; }
}