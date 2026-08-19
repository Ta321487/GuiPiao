using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GuiPiao.Model;
using GuiPiao.Utils;

namespace GuiPiao.ViewModel.TrainTicketForm;

/// <summary>
///     数据转换器 - 在表单数据和实体之间转换
/// </summary>
public class DataTransformer
{
    /// <summary>
    ///     将表单数据转换为实体
    /// </summary>
    public TrainRideInfo ToEntity(TrainTicketFormData data, int id = 0)
    {
        return new TrainRideInfo
        {
            Id = id,
            TrainNo = data.TrainNo,
            DepartStation = data.DepartStation,
            ArriveStation = data.ArriveStation,
            DepartDate = data.DepartDate,
            DepartTime = data.DepartTime,
            ArriveTime = data.ArriveTime,
            ArriveDayOffset = string.IsNullOrEmpty(data.ArriveTime)
                ? 0
                : ArriveTimeFormat.NormalizeOffset(data.ArriveDayOffset),
            CoachNo = FormatCoachNo(data),
            SeatNo = FormatSeatNo(data),
            Money = data.Money,
            SeatType = data.SeatType ?? string.Empty,
            AdditionalInfo = data.AdditionalInfo ?? string.Empty,
            TicketPurpose = data.TicketPurpose ?? string.Empty,
            TicketModificationType = data.TicketModificationType ?? string.Empty,
            TicketTypeFlags = data.TicketTypeFlags,
            PaymentChannelFlags = data.PaymentChannelFlags,
            Hint = data.Hint ?? string.Empty,
            TicketNumber = data.TicketNumber ?? string.Empty,
            CheckInLocation = data.CheckInLocation ?? string.Empty,
            DepartStationCode = data.DepartStationCode ?? string.Empty,
            ArriveStationCode = data.ArriveStationCode ?? string.Empty,
            DepartStationPinyin = data.DepartStationPinyin ?? string.Empty,
            ArriveStationPinyin = data.ArriveStationPinyin ?? string.Empty,
            Status = data.StatusValue,
            Tags = data.SelectedTagIds?.Select(tagId => new TicketTag { Id = tagId }).ToList() ?? new List<TicketTag>()
        };
    }

    /// <summary>
    ///     格式化车厢号（普通：09车；加挂：加09车）。
    /// </summary>
    private string FormatCoachNo(TrainTicketFormData data)
    {
        var coachNoInput = data.CoachNoInput;
        if (string.IsNullOrWhiteSpace(coachNoInput))
            return string.Empty;

        string body;
        if (int.TryParse(coachNoInput, out var coachNo))
            body = coachNo.ToString("D2");
        else
            body = coachNoInput.Trim();

        return data.IsJiaChe ? $"加{body}车" : $"{body}车";
    }

    /// <summary>
    ///     格式化座位号（根据席别补齐位数）
    /// </summary>
    private string FormatSeatNo(TrainTicketFormData data)
    {
        if (data.IsNoSeat)
            return "无座";

        if (string.IsNullOrWhiteSpace(data.SeatNoNumber))
            return string.Empty;

        if (!int.TryParse(data.SeatNoNumber, out var seatNo))
            return data.SeatNoNumber + data.SelectedSeatLetter;

        // 根据席别确定补齐位数
        var paddingLength = GetSeatNumberPaddingLength(data.SeatType);
        var formattedNumber = seatNo.ToString("D" + paddingLength);

        // 拼接字母/铺位
        return formattedNumber + data.SelectedSeatLetter;
    }

    /// <summary>
    ///     获取座位号补齐位数（高铁2位，普速3位）
    /// </summary>
    private int GetSeatNumberPaddingLength(string seatType)
    {
        // 高铁/动车类席别（需要2位数字+字母）
        if (seatType is "二等座" or "一等座" or "商务座" or "特等座" or "硬卧代硬座") return 2;
        // 普速类（需要3位数字）
        return 3;
    }

    /// <summary>
    ///     将实体转换为表单数据
    /// </summary>
    public TrainTicketFormData FromEntity(TrainRideInfo entity)
    {
        var data = new TrainTicketFormData
        {
            DepartDateTime = RideDateTime.TryParseDate(entity.DepartDate, out var date) ? date : DateTime.Now,
            DepartTimeValue = RideDateTime.TryParseTimeAsDateTime(entity.DepartTime, out var time) ? time : DateTime.Now,
            ArriveTimeValue = RideDateTime.TryParseTimeAsDateTime(entity.ArriveTime, out var arriveTime)
                ? arriveTime
                : null,
            ArriveDayOffset = string.IsNullOrWhiteSpace(entity.ArriveTime)
                ? 0
                : ArriveTimeFormat.NormalizeOffset(entity.ArriveDayOffset),
            MoneyText = entity.Money.ToString("0.00"),
            SeatType = entity.SeatType ?? string.Empty,
            AdditionalInfo = entity.AdditionalInfo ?? string.Empty,
            TicketPurpose = entity.TicketPurpose ?? string.Empty,
            TicketModificationType = entity.TicketModificationType ?? string.Empty,
            TicketTypeFlags = entity.TicketTypeFlags,
            PaymentChannelFlags = entity.PaymentChannelFlags,
            Hint = entity.Hint ?? string.Empty,
            SelectedHint = entity.Hint ?? string.Empty,
            TicketNumber = entity.TicketNumber ?? string.Empty,
            CheckInLocation = entity.CheckInLocation ?? string.Empty,
            DepartStationCode = entity.DepartStationCode ?? string.Empty,
            ArriveStationCode = entity.ArriveStationCode ?? string.Empty,
            DepartStationPinyin = entity.DepartStationPinyin ?? string.Empty,
            ArriveStationPinyin = entity.ArriveStationPinyin ?? string.Empty,
            SelectedStatus = entity.Status switch
            {
                0 => "未出行",
                1 => "已完成",
                2 => "已改签",
                3 => "已退票",
                _ => "已完成"
            }
        };

        // 解析车次号
        ParseTrainNo(entity.TrainNo, data);

        // 解析车站（去掉"站"字）
        data.DepartStationInput = StationFormRules.ToNameBody(entity.DepartStation);
        data.ArriveStationInput = StationFormRules.ToNameBody(entity.ArriveStation);

        // 解析车厢号（含加挂）
        ParseCoachNo(entity.CoachNo, data);

        // 解析座位号
        ParseSeatNo(entity.SeatNo, data);

        // 加载标签
        if (entity.Tags != null && entity.Tags.Any())
            data.SelectedTagIds = new ObservableCollection<int>(
                entity.Tags.Select(t => t.Id));
        else
            data.SelectedTagIds = new ObservableCollection<int>();

        return data;
    }

    /// <summary>
    ///     解析车次号
    /// </summary>
    private void ParseTrainNo(string trainNo, TrainTicketFormData data)
    {
        if (string.IsNullOrEmpty(trainNo))
        {
            data.SelectedTrainNoPrefix = "G";
            data.TrainNoNumber = string.Empty;
            return;
        }

        // 检查是否为纯数字
        if (char.IsDigit(trainNo[0]))
        {
            data.SelectedTrainNoPrefix = "纯数字";
            data.TrainNoNumber = trainNo;
        }
        else
        {
            data.SelectedTrainNoPrefix = trainNo[0].ToString().ToUpper();
            data.TrainNoNumber = trainNo.Length > 1 ? trainNo.Substring(1) : string.Empty;
        }
    }

    /// <summary>
    ///     解析车厢号：去掉「车」；若含「加挂/加」则勾选加车，输入框只保留数字。
    /// </summary>
    private void ParseCoachNo(string coachNo, TrainTicketFormData data)
    {
        if (string.IsNullOrWhiteSpace(coachNo))
        {
            data.IsJiaChe = false;
            data.CoachNoInput = string.Empty;
            return;
        }

        var s = coachNo.Trim();
        if (s.EndsWith("车", StringComparison.Ordinal) && s.Length > 1)
            s = s[..^1];

        var isJia = s.Contains("加挂", StringComparison.Ordinal) ||
                    s.Contains("加", StringComparison.Ordinal);
        if (isJia)
        {
            s = s.Replace("加挂", "", StringComparison.Ordinal)
                .Replace("加", "", StringComparison.Ordinal)
                .Trim();
        }

        data.IsJiaChe = isJia;
        data.CoachNoInput = s;
    }

    /// <summary>
    ///     解析座位号
    /// </summary>
    private void ParseSeatNo(string seatNo, TrainTicketFormData data)
    {
        if (string.IsNullOrEmpty(seatNo))
        {
            data.IsNoSeat = false;
            data.SeatNoNumber = string.Empty;
            data.SelectedSeatLetter = string.Empty;
            return;
        }

        if (seatNo == "无座")
        {
            data.IsNoSeat = true;
            data.SeatNoNumber = string.Empty;
            data.SelectedSeatLetter = string.Empty;
        }
        else
        {
            data.IsNoSeat = false;

            // 提取数字部分和字母部分
            var letterIndex = -1;
            for (var i = 0; i < seatNo.Length; i++)
                if (char.IsLetter(seatNo[i]))
                {
                    letterIndex = i;
                    break;
                }

            if (letterIndex > 0)
            {
                data.SeatNoNumber = seatNo.Substring(0, letterIndex);
                data.SelectedSeatLetter = seatNo.Substring(letterIndex);
            }
            else
            {
                data.SeatNoNumber = seatNo;
                data.SelectedSeatLetter = string.Empty;
            }
        }
    }
}