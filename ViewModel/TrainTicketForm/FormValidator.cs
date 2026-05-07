using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GuiPiao.ViewModel.TrainTicketForm;

/// <summary>
///     表单验证结果
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<ValidationError> Errors { get; set; } = new();
}

public class ValidationError
{
    public string PropertyName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}

/// <summary>
///     表单验证器
/// </summary>
public class FormValidator
{
    public static FormValidator CreateDefault()
    {
        return new FormValidator();
    }

    public ValidationResult Validate(TrainTicketFormData data)
    {
        var result = new ValidationResult { IsValid = true };

        // 验证车次号
        if (string.IsNullOrWhiteSpace(data.TrainNoNumber))
        {
            result.IsValid = false;
            result.Errors.Add(
                new ValidationError { PropertyName = nameof(data.TrainNoNumber), ErrorMessage = "请输入车次号" });
        }

        // 验证出发车站
        if (string.IsNullOrWhiteSpace(data.DepartStationInput))
        {
            result.IsValid = false;
            result.Errors.Add(new ValidationError
                { PropertyName = nameof(data.DepartStationInput), ErrorMessage = "请输入出发车站" });
        }

        // 验证到达车站
        if (string.IsNullOrWhiteSpace(data.ArriveStationInput))
        {
            result.IsValid = false;
            result.Errors.Add(new ValidationError
                { PropertyName = nameof(data.ArriveStationInput), ErrorMessage = "请输入到达车站" });
        }

        // 验证出发日期
        if (!data.DepartDateTime.HasValue)
        {
            result.IsValid = false;
            result.Errors.Add(new ValidationError
                { PropertyName = nameof(data.DepartDateTime), ErrorMessage = "请选择出发日期" });
        }

        // 验证出发时间
        if (!data.DepartTimeValue.HasValue)
        {
            result.IsValid = false;
            result.Errors.Add(new ValidationError
                { PropertyName = nameof(data.DepartTimeValue), ErrorMessage = "请选择出发时间" });
        }

        // 验证席别
        if (string.IsNullOrWhiteSpace(data.SeatType))
        {
            result.IsValid = false;
            result.Errors.Add(new ValidationError { PropertyName = nameof(data.SeatType), ErrorMessage = "请选择席别" });
        }

        // 验证车厢号
        if (string.IsNullOrWhiteSpace(data.CoachNoInput))
        {
            result.IsValid = false;
            result.Errors.Add(new ValidationError
                { PropertyName = nameof(data.CoachNoInput), ErrorMessage = "请输入车厢号" });
        }
        else if (!ValidateCoachNo(data.CoachNoInput, out var coachNoError))
        {
            result.IsValid = false;
            result.Errors.Add(new ValidationError
                { PropertyName = nameof(data.CoachNoInput), ErrorMessage = coachNoError });
        }

        // 验证座位号（如果不是无座）
        if (!data.IsNoSeat)
        {
            if (string.IsNullOrWhiteSpace(data.SeatNoNumber))
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError
                    { PropertyName = nameof(data.SeatNoNumber), ErrorMessage = "请输入座位号" });
            }
            else if (!ValidateSeatNo(data.SeatNoNumber, out var seatNoError))
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError
                    { PropertyName = nameof(data.SeatNoNumber), ErrorMessage = seatNoError });
            }
        }

        // 验证金额
        if (string.IsNullOrWhiteSpace(data.MoneyText))
        {
            result.IsValid = false;
            result.Errors.Add(new ValidationError { PropertyName = nameof(data.MoneyText), ErrorMessage = "请输入金额" });
        }
        else if (!ValidateMoneyFormat(data.MoneyText))
        {
            result.IsValid = false;
            result.Errors.Add(new ValidationError
                { PropertyName = nameof(data.MoneyText), ErrorMessage = "金额格式不正确，请输入有效的数值（最多两位小数）" });
        }
        else if (data.Money < 0)
        {
            result.IsValid = false;
            result.Errors.Add(new ValidationError { PropertyName = nameof(data.MoneyText), ErrorMessage = "金额不能为负数" });
        }

        return result;
    }

    /// <summary>
    ///     验证金额格式
    /// </summary>
    private bool ValidateMoneyFormat(string moneyText)
    {
        // 匹配数字，可选的小数部分（最多两位）
        return Regex.IsMatch(moneyText, @"^\d+(\.\d{1,2})?$");
    }

    /// <summary>
    ///     验证车厢号（必须大于0）
    /// </summary>
    private bool ValidateCoachNo(string coachNoInput, out string errorMessage)
    {
        errorMessage = string.Empty;

        // 尝试解析数字
        if (!int.TryParse(coachNoInput, out var coachNo))
        {
            errorMessage = "车厢号必须是有效的数字";
            return false;
        }

        // 必须大于0
        if (coachNo <= 0)
        {
            errorMessage = "车厢号必须大于0";
            return false;
        }

        return true;
    }

    /// <summary>
    ///     验证座位号（必须大于0）
    /// </summary>
    private bool ValidateSeatNo(string seatNoNumber, out string errorMessage)
    {
        errorMessage = string.Empty;

        // 尝试解析数字
        if (!int.TryParse(seatNoNumber, out var seatNo))
        {
            errorMessage = "座位号必须是有效的数字";
            return false;
        }

        // 必须大于0
        if (seatNo <= 0)
        {
            errorMessage = "座位号必须大于0";
            return false;
        }

        return true;
    }

    public bool HasRequiredFieldsEmpty(TrainTicketFormData data)
    {
        return GetEmptyRequiredFields(data).Count > 0;
    }

    public List<string> GetEmptyRequiredFields(TrainTicketFormData data)
    {
        var emptyFields = new List<string>();

        if (string.IsNullOrWhiteSpace(data.TrainNoNumber))
            emptyFields.Add("车次号");

        if (string.IsNullOrWhiteSpace(data.DepartStationInput))
            emptyFields.Add("出发车站");

        if (string.IsNullOrWhiteSpace(data.ArriveStationInput))
            emptyFields.Add("到达车站");

        if (!data.DepartDateTime.HasValue)
            emptyFields.Add("出发日期");

        if (!data.DepartTimeValue.HasValue)
            emptyFields.Add("出发时间");

        if (string.IsNullOrWhiteSpace(data.SeatType))
            emptyFields.Add("席别");

        if (string.IsNullOrWhiteSpace(data.CoachNoInput))
            emptyFields.Add("车厢号");

        if (!data.IsNoSeat && string.IsNullOrWhiteSpace(data.SeatNoNumber))
            emptyFields.Add("座位号");

        if (string.IsNullOrWhiteSpace(data.MoneyText))
            emptyFields.Add("金额");

        return emptyFields;
    }
}