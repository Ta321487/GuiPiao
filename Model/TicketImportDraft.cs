using System.Collections.Generic;

namespace GuiPiao.Model;

/// <summary>
///     OCR/粘贴导入的识别稿（预填用，不等于已保存记录）。
/// </summary>
public class TicketImportDraft
{
    /// <summary>原始全文（粘贴原文或 OCR 拼接文本）。</summary>
    public string RawText { get; set; } = string.Empty;

    /// <summary>粗分来源族提示，如「短信」「未知」。</summary>
    public string SourceHint { get; set; } = "未知";

    public string? TrainNo { get; set; }
    public string? DepartStation { get; set; }
    public string? ArriveStation { get; set; }
    public string? DepartDate { get; set; }
    public string? DepartTime { get; set; }
    public string? ArriveTime { get; set; }

    /// <summary>车厢号数字部分，如 05；不含「车」「加」。</summary>
    public string? CoachNo { get; set; }

    /// <summary>是否加挂车厢（对应表单「加」勾选）。</summary>
    public bool IsJiaChe { get; set; }

    /// <summary>座位号：如 12A、003中铺；无座时为空并看 <see cref="IsNoSeat"/>。</summary>
    public string? SeatNo { get; set; }

    /// <summary>是否无座（对应表单「无座」勾选）。</summary>
    public bool IsNoSeat { get; set; }

    /// <summary>已映射到表单席别选项的值；无法映射时为 null。</summary>
    public string? SeatType { get; set; }

    /// <summary>识别到但未映射进下拉的原始席别文案（供核对提示）。</summary>
    public string? UnmappedSeatTypeRaw { get; set; }

    public string? MoneyText { get; set; }
    public string? CheckInLocation { get; set; }
    public string? TicketNumber { get; set; }

    /// <summary>未能抽出或需人工核对的字段显示名。</summary>
    public List<string> FieldsNeedingReview { get; set; } = new();

    /// <summary>是否至少抽出一项可用字段。</summary>
    public bool HasAnyField =>
        !string.IsNullOrWhiteSpace(TrainNo) ||
        !string.IsNullOrWhiteSpace(DepartStation) ||
        !string.IsNullOrWhiteSpace(ArriveStation) ||
        !string.IsNullOrWhiteSpace(DepartDate) ||
        !string.IsNullOrWhiteSpace(DepartTime) ||
        !string.IsNullOrWhiteSpace(ArriveTime) ||
        !string.IsNullOrWhiteSpace(CoachNo) ||
        IsJiaChe ||
        !string.IsNullOrWhiteSpace(SeatNo) ||
        IsNoSeat ||
        !string.IsNullOrWhiteSpace(SeatType) ||
        !string.IsNullOrWhiteSpace(MoneyText) ||
        !string.IsNullOrWhiteSpace(CheckInLocation) ||
        !string.IsNullOrWhiteSpace(TicketNumber);
}
