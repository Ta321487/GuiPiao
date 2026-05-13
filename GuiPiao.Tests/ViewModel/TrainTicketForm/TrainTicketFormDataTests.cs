using GuiPiao.ViewModel.TrainTicketForm;
using Xunit;

namespace GuiPiao.Tests.ViewModel.TrainTicketForm;

public class TrainTicketFormDataTests
{
    [Fact]
    public void TrainNo_前缀与数字拼接()
    {
        var data = new TrainTicketFormData { SelectedTrainNoPrefix = "G", TrainNoNumber = "88" };
        Assert.Equal("G88", data.TrainNo);
    }

    [Fact]
    public void TrainNo_纯数字前缀时仅为数字部分()
    {
        var data = new TrainTicketFormData { SelectedTrainNoPrefix = "纯数字", TrainNoNumber = "6063" };
        Assert.Equal("6063", data.TrainNo);
    }

    [Theory]
    [InlineData("未出行", 0)]
    [InlineData("已完成", 1)]
    [InlineData("已改签", 2)]
    [InlineData("已退票", 3)]
    [InlineData("未知状态", 1)]
    public void StatusValue_与状态文案对应(string status, int expected)
    {
        var data = new TrainTicketFormData { SelectedStatus = status };
        Assert.Equal(expected, data.StatusValue);
    }

    [Fact]
    public void TicketTypeFlags_可读写还原()
    {
        var data = new TrainTicketFormData();
        data.TicketTypeFlags = 1 | 8;
        Assert.True(data.IsStudentTicket);
        Assert.True(data.IsChildTicket);
        Assert.False(data.IsDiscountTicket);
        Assert.False(data.IsOnlineTicket);
    }

    [Fact]
    public void PaymentChannelFlags_可读写还原()
    {
        var data = new TrainTicketFormData();
        data.PaymentChannelFlags = 1 | 16;
        Assert.True(data.IsAlipay);
        Assert.True(data.IsICBC);
        Assert.False(data.IsWeChat);
    }
}
