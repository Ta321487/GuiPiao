using System;
using System.Linq;
using GuiPiao.ViewModel.TrainTicketForm;
using Xunit;

namespace GuiPiao.Tests.ViewModel.TrainTicketForm;

public class FormValidatorTests
{
    private readonly FormValidator _sut = FormValidator.CreateDefault();

    private static TrainTicketFormData CreateMinimalValidForm()
    {
        return new TrainTicketFormData
        {
            TrainNoNumber = "123",
            DepartStationInput = "北京",
            ArriveStationInput = "上海",
            DepartDateTime = DateTime.Today,
            DepartTimeValue = DateTime.Today,
            SeatType = "二等座",
            CoachNoInput = "5",
            SeatNoNumber = "10",
            MoneyText = "100",
            IsNoSeat = false
        };
    }

    [Fact]
    public void Validate_全部必填合法时通过()
    {
        var r = _sut.Validate(CreateMinimalValidForm());
        Assert.True(r.IsValid);
        Assert.Empty(r.Errors);
    }

    [Fact]
    public void Validate_车次号为空时失败并包含车次号错误()
    {
        var data = CreateMinimalValidForm();
        data.TrainNoNumber = " ";
        var r = _sut.Validate(data);
        Assert.False(r.IsValid);
        Assert.Contains(r.Errors, e => e.PropertyName == nameof(TrainTicketFormData.TrainNoNumber));
    }

    [Fact]
    public void Validate_金额格式三位小数时失败()
    {
        var data = CreateMinimalValidForm();
        data.MoneyText = "12.345";
        var r = _sut.Validate(data);
        Assert.False(r.IsValid);
        Assert.Contains(r.Errors, e => e.PropertyName == nameof(TrainTicketFormData.MoneyText));
    }

    [Fact]
    public void Validate_车厢号不大于0时失败()
    {
        var data = CreateMinimalValidForm();
        data.CoachNoInput = "0";
        var r = _sut.Validate(data);
        Assert.False(r.IsValid);
        Assert.Contains(r.Errors, e => e.PropertyName == nameof(TrainTicketFormData.CoachNoInput));
    }

    [Fact]
    public void Validate_无座时不要求座位号()
    {
        var data = CreateMinimalValidForm();
        data.IsNoSeat = true;
        data.SeatNoNumber = "";
        var r = _sut.Validate(data);
        Assert.True(r.IsValid);
    }

    [Fact]
    public void GetEmptyRequiredFields_与校验必填项一致()
    {
        var data = new TrainTicketFormData();
        var empty = _sut.GetEmptyRequiredFields(data);
        Assert.Contains("车次号", empty);
        Assert.Contains("出发车站", empty);
        Assert.Contains("金额", empty);
    }
}
