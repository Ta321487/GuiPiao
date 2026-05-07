using System.Collections.ObjectModel;

namespace GuiPiao.ViewModel.TrainTicketForm;

/// <summary>
///     业务规则引擎
/// </summary>
public class BusinessRuleEngine
{
    private readonly OptionsProvider _optionsProvider;

    public BusinessRuleEngine(OptionsProvider optionsProvider)
    {
        _optionsProvider = optionsProvider;
    }

    public static BusinessRuleEngine CreateDefault()
    {
        return new BusinessRuleEngine(new OptionsProvider());
    }

    /// <summary>
    ///     执行业务规则
    /// </summary>
    /// <param name="data">表单数据</param>
    /// <param name="propertyName">变更的属性名</param>
    /// <param name="ticketPurposeOptions">车票用途选项集合（需要更新时）</param>
    /// <returns>是否修改了数据</returns>
    public bool Execute(TrainTicketFormData data, string propertyName,
        ObservableCollection<string>? ticketPurposeOptions)
    {
        var modified = false;

        // 席别变化时更新座位字母选项
        if (propertyName == nameof(data.SeatType)) modified = true;
        return modified;
    }

    /// <summary>
    ///     处理票种类型互斥（学生票与儿童票）
    /// </summary>
    public void HandleTicketTypeMutex(TrainTicketFormData data, string changedProperty)
    {
        if (changedProperty == nameof(data.IsStudentTicket) && data.IsStudentTicket)
            // 选中学生票时，取消儿童票
            data.IsChildTicket = false;
        else if (changedProperty == nameof(data.IsChildTicket) && data.IsChildTicket)
            // 选中儿童票时，取消学生票
            data.IsStudentTicket = false;
    }

    /// <summary>
    ///     处理支付渠道互斥
    ///     支付宝与微信互斥
    ///     银行只能选择一个
    /// </summary>
    public void HandlePaymentChannelMutex(TrainTicketFormData data, string changedProperty)
    {
        // 支付宝与微信互斥
        if (changedProperty == nameof(data.IsAlipay) && data.IsAlipay)
            data.IsWeChat = false;
        else if (changedProperty == nameof(data.IsWeChat) && data.IsWeChat) data.IsAlipay = false;

        // 银行只能选择一个
        if (changedProperty.StartsWith("Is") && changedProperty != nameof(data.IsAlipay) &&
            changedProperty != nameof(data.IsWeChat))
        {
            // 如果选中了一个银行，取消其他银行
            var isBankSelected = changedProperty switch
            {
                nameof(data.IsABC) => data.IsABC,
                nameof(data.IsCCB) => data.IsCCB,
                nameof(data.IsICBC) => data.IsICBC,
                nameof(data.IsBCOM) => data.IsBCOM,
                nameof(data.IsCMB) => data.IsCMB,
                nameof(data.IsPSBC) => data.IsPSBC,
                nameof(data.IsBOC) => data.IsBOC,
                _ => false
            };

            if (isBankSelected)
            {
                // 取消其他银行选择
                if (changedProperty != nameof(data.IsABC)) data.IsABC = false;
                if (changedProperty != nameof(data.IsCCB)) data.IsCCB = false;
                if (changedProperty != nameof(data.IsICBC)) data.IsICBC = false;
                if (changedProperty != nameof(data.IsBCOM)) data.IsBCOM = false;
                if (changedProperty != nameof(data.IsCMB)) data.IsCMB = false;
                if (changedProperty != nameof(data.IsPSBC)) data.IsPSBC = false;
                if (changedProperty != nameof(data.IsBOC)) data.IsBOC = false;
            }
        }
    }
}