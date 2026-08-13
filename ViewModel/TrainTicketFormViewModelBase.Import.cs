using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GuiPiao.DataAccess;
using GuiPiao.Messages;
using GuiPiao.Model;
using GuiPiao.Services;
using GuiPiao.Utils;
using GuiPiao.View;

namespace GuiPiao.ViewModel.TrainTicketForm;

public abstract partial class TrainTicketFormViewModelBase
{
    /// <summary>
    ///     根据导入识别稿设置字段浅黄高亮（已抽出或需核对）。
    /// </summary>
    public void ApplyImportHighlights(TicketImportDraft draft)
    {
        if (draft == null) return;

        bool Need(string label) =>
            draft.FieldsNeedingReview.Any(x => x.Contains(label, StringComparison.Ordinal));

        HighlightTrainNo = !string.IsNullOrWhiteSpace(draft.TrainNo) || Need("车次");
        HighlightDepartStation = !string.IsNullOrWhiteSpace(draft.DepartStation) || Need("出发站");
        HighlightArriveStation = !string.IsNullOrWhiteSpace(draft.ArriveStation) || Need("到达站");
        HighlightDepartDate = !string.IsNullOrWhiteSpace(draft.DepartDate) || Need("出发日期");
        HighlightDepartTime = !string.IsNullOrWhiteSpace(draft.DepartTime) || Need("出发时间");
        HighlightArriveTime = !string.IsNullOrWhiteSpace(draft.ArriveTime);
        HighlightCoachNo = !string.IsNullOrWhiteSpace(draft.CoachNo) || draft.IsJiaChe || Need("车厢");
        HighlightSeatNo = !string.IsNullOrWhiteSpace(draft.SeatNo) || draft.IsNoSeat || Need("座位");
        HighlightSeatType = !string.IsNullOrWhiteSpace(draft.SeatType) ||
                            !string.IsNullOrWhiteSpace(draft.UnmappedSeatTypeRaw) || Need("席别");
        HighlightMoney = !string.IsNullOrWhiteSpace(draft.MoneyText) || Need("票价");
        HighlightCheckIn = !string.IsNullOrWhiteSpace(draft.CheckInLocation);
        HighlightTicketNumber = !string.IsNullOrWhiteSpace(draft.TicketNumber);
    }

    public void ClearImportHighlights()
    {
        HighlightTrainNo = false;
        HighlightDepartStation = false;
        HighlightArriveStation = false;
        HighlightDepartDate = false;
        HighlightDepartTime = false;
        HighlightArriveTime = false;
        HighlightCoachNo = false;
        HighlightSeatNo = false;
        HighlightSeatType = false;
        HighlightMoney = false;
        HighlightCheckIn = false;
        HighlightTicketNumber = false;
    }

    /// <summary>
    ///     将识别稿字段写入表单（不落库）。新增/编辑共用。
    /// </summary>
    public async Task FillFromImportDraftAsync(TicketImportDraft draft)
    {
        if (draft == null) return;

        _isApplyingRescheduleData = true;
        try
        {
            var tempRide = new TrainRideInfo
            {
                TrainNo = draft.TrainNo ?? string.Empty,
                DepartStation = draft.DepartStation ?? string.Empty,
                ArriveStation = draft.ArriveStation ?? string.Empty,
                DepartDate = draft.DepartDate ?? string.Empty,
                DepartTime = draft.DepartTime ?? string.Empty,
                ArriveTime = draft.ArriveTime ?? string.Empty,
                CoachNo = BuildCoachNoForImport(draft),
                SeatNo = draft.IsNoSeat ? "无座" : (draft.SeatNo ?? string.Empty),
                SeatType = draft.SeatType ?? string.Empty,
                Money = decimal.TryParse(draft.MoneyText, out var money) ? money : 0,
                CheckInLocation = draft.CheckInLocation ?? string.Empty,
                TicketNumber = draft.TicketNumber ?? string.Empty
            };

            var parsed = _dataTransformer.FromEntity(tempRide);

            if (!string.IsNullOrWhiteSpace(draft.TrainNo))
            {
                SelectedTrainNoPrefix = parsed.SelectedTrainNoPrefix;
                TrainNoNumber = parsed.TrainNoNumber;
            }

            if (!string.IsNullOrWhiteSpace(draft.DepartStation))
                DepartStationInput = parsed.DepartStationInput;
            if (!string.IsNullOrWhiteSpace(draft.ArriveStation))
                ArriveStationInput = parsed.ArriveStationInput;

            if (!string.IsNullOrWhiteSpace(draft.DepartDate) && parsed.DepartDateTime.HasValue)
                DepartDateTime = parsed.DepartDateTime;
            if (!string.IsNullOrWhiteSpace(draft.DepartTime) && parsed.DepartTimeValue.HasValue)
                DepartTimeValue = parsed.DepartTimeValue;
            if (!string.IsNullOrWhiteSpace(draft.ArriveTime) && parsed.ArriveTimeValue.HasValue)
                ArriveTimeValue = parsed.ArriveTimeValue;

            if (!string.IsNullOrWhiteSpace(draft.SeatType) &&
                SeatTypeOptions.Contains(draft.SeatType))
                SeatType = draft.SeatType;

            if (!string.IsNullOrWhiteSpace(draft.CoachNo) || draft.IsJiaChe)
            {
                CoachNoInput = parsed.CoachNoInput;
                IsJiaChe = draft.IsJiaChe || parsed.IsJiaChe;
            }

            if (draft.IsNoSeat)
            {
                IsNoSeat = true;
                SeatNoNumber = string.Empty;
                SelectedSeatLetter = string.Empty;
            }
            else if (!string.IsNullOrWhiteSpace(draft.SeatNo))
            {
                IsNoSeat = false;
                SeatNoNumber = parsed.SeatNoNumber;
                if (!string.IsNullOrEmpty(parsed.SelectedSeatLetter) &&
                    SeatLetterOptions.Contains(parsed.SelectedSeatLetter))
                    SelectedSeatLetter = parsed.SelectedSeatLetter;
                else if (!string.IsNullOrEmpty(parsed.SelectedSeatLetter))
                    SelectedSeatLetter = parsed.SelectedSeatLetter;
            }

            if (!string.IsNullOrWhiteSpace(draft.MoneyText))
                MoneyText = parsed.MoneyText;
            if (!string.IsNullOrWhiteSpace(draft.CheckInLocation))
                CheckInLocation = parsed.CheckInLocation;
            if (!string.IsNullOrWhiteSpace(draft.TicketNumber))
                TicketNumber = parsed.TicketNumber;

            await QueryDepartStationInfoAsync();
            await QueryArriveStationInfoAsync();

            IsDepartStationDropdownOpen = false;
            IsArriveStationDropdownOpen = false;
            DepartStationSuggestions.Clear();
            ArriveStationSuggestions.Clear();
        }
        finally
        {
            _isApplyingRescheduleData = false;
        }

        ApplyImportHighlights(draft);
        HasUnsavedChanges = true;
    }

    private static string BuildCoachNoForImport(TicketImportDraft draft)
    {
        if (string.IsNullOrWhiteSpace(draft.CoachNo) && !draft.IsJiaChe)
            return string.Empty;

        var num = draft.CoachNo?.Trim() ?? string.Empty;
        if (draft.IsJiaChe)
            return string.IsNullOrEmpty(num) ? "加车" : $"加{num}车";
        return $"{num}车";
    }

    /// <summary>
    ///     检查是否有必填项未填写
    /// </summary>
    public bool HasRequiredFieldsEmpty()
    {
        return _formValidator.HasRequiredFieldsEmpty(_formData);
    }

    /// <summary>
    ///     获取未填写的必填项列表
    /// </summary>
    public List<string> GetEmptyRequiredFields()
    {
        return _formValidator.GetEmptyRequiredFields(_formData);
    }
}
