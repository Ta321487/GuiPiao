using GuiPiao.ViewModel.TrainTicketForm;
using Xunit;

namespace GuiPiao.Tests.ViewModel.TrainTicketForm;

public class BusinessRuleEngineTests
{
    private readonly BusinessRuleEngine _sut = BusinessRuleEngine.CreateDefault();

    [Fact]
    public void HandleTicketTypeMutex_选学生票时取消儿童票()
    {
        var data = new TrainTicketFormData { IsStudentTicket = true, IsChildTicket = true };
        _sut.HandleTicketTypeMutex(data, nameof(TrainTicketFormData.IsStudentTicket));
        Assert.True(data.IsStudentTicket);
        Assert.False(data.IsChildTicket);
    }

    [Fact]
    public void HandleTicketTypeMutex_选儿童票时取消学生票()
    {
        var data = new TrainTicketFormData { IsStudentTicket = true, IsChildTicket = true };
        _sut.HandleTicketTypeMutex(data, nameof(TrainTicketFormData.IsChildTicket));
        Assert.True(data.IsChildTicket);
        Assert.False(data.IsStudentTicket);
    }

    [Fact]
    public void HandlePaymentChannelMutex_开支付宝时关微信()
    {
        var data = new TrainTicketFormData { IsAlipay = true, IsWeChat = true };
        _sut.HandlePaymentChannelMutex(data, nameof(TrainTicketFormData.IsAlipay));
        Assert.True(data.IsAlipay);
        Assert.False(data.IsWeChat);
    }

    [Fact]
    public void HandlePaymentChannelMutex_开微信时关支付宝()
    {
        var data = new TrainTicketFormData { IsAlipay = true, IsWeChat = true };
        _sut.HandlePaymentChannelMutex(data, nameof(TrainTicketFormData.IsWeChat));
        Assert.True(data.IsWeChat);
        Assert.False(data.IsAlipay);
    }

    [Fact]
    public void HandlePaymentChannelMutex_选中某银行时仅保留该银行()
    {
        var data = new TrainTicketFormData
        {
            IsABC = true,
            IsCCB = true,
            IsICBC = true,
            IsBCOM = true,
            IsCMB = true,
            IsPSBC = true,
            IsBOC = true
        };
        data.IsICBC = true;
        _sut.HandlePaymentChannelMutex(data, nameof(TrainTicketFormData.IsICBC));
        Assert.False(data.IsABC);
        Assert.False(data.IsCCB);
        Assert.True(data.IsICBC);
        Assert.False(data.IsBCOM);
        Assert.False(data.IsCMB);
        Assert.False(data.IsPSBC);
        Assert.False(data.IsBOC);
    }

    [Fact]
    public void Execute_席别变更时返回已修改()
    {
        var data = new TrainTicketFormData();
        var modified = _sut.Execute(data, nameof(TrainTicketFormData.SeatType), null);
        Assert.True(modified);
    }
}
