using System.Collections.Generic;
using GuiPiao.Model;
using GuiPiao.ViewModel.TrainTicketForm;
using Xunit;

namespace GuiPiao.Tests.ViewModel.TrainTicketForm;

public class DataTransformerTests
{
    private readonly DataTransformer _sut = new();

    [Fact]
    public void ToEntity_车厢号数字补齐为两位()
    {
        var data = new TrainTicketFormData
        {
            SelectedTrainNoPrefix = "G",
            TrainNoNumber = "1",
            DepartStationInput = "北京",
            ArriveStationInput = "上海",
            DepartDateTime = new System.DateTime(2026, 5, 14),
            DepartTimeValue = new System.DateTime(2000, 1, 1, 8, 30, 0),
            SeatType = "二等座",
            CoachNoInput = "9",
            SeatNoNumber = "5",
            SelectedSeatLetter = "A",
            MoneyText = "100.00",
            IsNoSeat = false
        };

        var e = _sut.ToEntity(data, 42);
        Assert.Equal(42, e.Id);
        Assert.Equal("G1", e.TrainNo);
        Assert.Equal("09车", e.CoachNo);
        Assert.Equal("05A", e.SeatNo);
    }

    [Fact]
    public void ToEntity_普速席别座位号补齐为三位()
    {
        var data = new TrainTicketFormData
        {
            SelectedTrainNoPrefix = "K",
            TrainNoNumber = "528",
            DepartStationInput = "成都",
            ArriveStationInput = "重庆",
            DepartDateTime = new System.DateTime(2026, 5, 1),
            DepartTimeValue = new System.DateTime(2000, 1, 1, 10, 0, 0),
            SeatType = "新空调硬座",
            CoachNoInput = "12",
            SeatNoNumber = "7",
            SelectedSeatLetter = "",
            MoneyText = "50",
            IsNoSeat = false
        };

        var e = _sut.ToEntity(data);
        Assert.Equal("007", e.SeatNo);
    }

    [Fact]
    public void ToEntity_无座()
    {
        var data = new TrainTicketFormData
        {
            TrainNoNumber = "1234",
            DepartStationInput = "A",
            ArriveStationInput = "B",
            DepartDateTime = System.DateTime.Today,
            DepartTimeValue = System.DateTime.Today,
            SeatType = "二等座",
            CoachNoInput = "1",
            MoneyText = "0",
            IsNoSeat = true,
            SeatNoNumber = ""
        };
        var e = _sut.ToEntity(data);
        Assert.Equal("无座", e.SeatNo);
    }

    [Fact]
    public void FromEntity_ToEntity_关键字段往返一致()
    {
        var entity = new TrainRideInfo
        {
            Id = 7,
            TrainNo = "G88",
            DepartStation = "北京南站",
            ArriveStation = "上海虹桥站",
            DepartDate = "2026-06-01",
            DepartTime = "09:15",
            CoachNo = "06车",
            SeatNo = "12F",
            Money = 553.5m,
            SeatType = "一等座",
            AdditionalInfo = "",
            TicketPurpose = "",
            TicketModificationType = "",
            TicketTypeFlags = 0,
            PaymentChannelFlags = 0,
            Hint = "",
            TicketNumber = "",
            CheckInLocation = "",
            DepartStationCode = "",
            ArriveStationCode = "",
            DepartStationPinyin = "",
            ArriveStationPinyin = "",
            Status = 0,
            Tags = new List<TicketTag>()
        };

        var data = _sut.FromEntity(entity);
        var back = _sut.ToEntity(data, entity.Id);

        Assert.Equal(entity.Id, back.Id);
        Assert.Equal(entity.TrainNo, back.TrainNo);
        Assert.Equal(entity.DepartStation, back.DepartStation);
        Assert.Equal(entity.ArriveStation, back.ArriveStation);
        Assert.Equal(entity.CoachNo, back.CoachNo);
        Assert.Equal(entity.SeatNo, back.SeatNo);
        Assert.Equal(entity.Money, back.Money);
        Assert.Equal(entity.SeatType, back.SeatType);
        Assert.Equal(entity.Status, back.Status);
    }

    [Fact]
    public void FromEntity_纯数字车次解析()
    {
        var entity = new TrainRideInfo
        {
            TrainNo = "6063",
            DepartStation = "站",
            ArriveStation = "站",
            DepartDate = "2026-01-01",
            DepartTime = "00:00",
            CoachNo = "1车",
            SeatNo = "无座",
            Money = 1m,
            SeatType = "硬座",
            AdditionalInfo = "",
            TicketPurpose = "",
            TicketModificationType = "",
            DepartStationCode = "",
            ArriveStationCode = "",
            DepartStationPinyin = "",
            ArriveStationPinyin = "",
            Hint = "",
            TicketNumber = "",
            CheckInLocation = "",
            Tags = new List<TicketTag>()
        };

        var data = _sut.FromEntity(entity);
        Assert.Equal("纯数字", data.SelectedTrainNoPrefix);
        Assert.Equal("6063", data.TrainNoNumber);
        Assert.True(data.IsNoSeat);
    }
}
