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
    ///     异步查询出发车站信息
    /// </summary>
    protected async Task QueryDepartStationInfoAsync()
    {
        if (string.IsNullOrWhiteSpace(DepartStationInput))
        {
            DepartStationCode = string.Empty;
            DepartStationPinyin = string.Empty;
            return;
        }

        try
        {
            var station = await _stationQueryService.QueryStationAsync(DepartStationInput);
            if (station != null)
            {
                DepartStationCode = station.StationCode ?? string.Empty;
                DepartStationPinyin = station.StationPinyin ?? string.Empty;
                _logService?.Info("TrainTicketFormViewModelBase",
                    $"查询出发车站成功: {DepartStationInput} -> 代码:{DepartStationCode}, 拼音:{DepartStationPinyin}");
            }
            else
            {
                DepartStationCode = string.Empty;
                DepartStationPinyin = string.Empty;
                _logService?.Info("TrainTicketFormViewModelBase", $"未找到出发车站: {DepartStationInput}");
            }
        }
        catch (Exception ex)
        {
            _logService?.Error("TrainTicketFormViewModelBase", $"查询出发车站失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     异步查询到达车站信息
    /// </summary>
    protected async Task QueryArriveStationInfoAsync()
    {
        if (string.IsNullOrWhiteSpace(ArriveStationInput))
        {
            ArriveStationCode = string.Empty;
            ArriveStationPinyin = string.Empty;
            return;
        }

        try
        {
            var station = await _stationQueryService.QueryStationAsync(ArriveStationInput);
            if (station != null)
            {
                ArriveStationCode = station.StationCode ?? string.Empty;
                ArriveStationPinyin = station.StationPinyin ?? string.Empty;
                _logService?.Info("TrainTicketFormViewModelBase",
                    $"查询到达车站成功: {ArriveStationInput} -> 代码:{ArriveStationCode}, 拼音:{ArriveStationPinyin}");
            }
            else
            {
                ArriveStationCode = string.Empty;
                ArriveStationPinyin = string.Empty;
                _logService?.Info("TrainTicketFormViewModelBase", $"未找到到达车站: {ArriveStationInput}");
            }
        }
        catch (Exception ex)
        {
            _logService?.Error("TrainTicketFormViewModelBase", $"查询到达车站失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     异步搜索出发车站联想建议
    /// </summary>
    private async Task SearchDepartStationSuggestionsAsync()
    {
        _logService?.Info("TrainTicketFormViewModelBase",
            $"[DEBUG] SearchDepartStationSuggestionsAsync 开始执行，输入: '{DepartStationInput}'");

        if (string.IsNullOrWhiteSpace(DepartStationInput) || DepartStationInput.Length < 1)
        {
            _logService?.Info("TrainTicketFormViewModelBase", "[DEBUG] 输入为空或长度小于1，清空建议并关闭下拉框");
            if (DepartStationSuggestions.Count > 0)
                DepartStationSuggestions.Clear();
            IsDepartStationDropdownOpen = false;
            return;
        }

        try
        {
            _logService?.Info("TrainTicketFormViewModelBase", $"[DEBUG] 开始搜索车站，关键词: '{DepartStationInput}'");
            var suggestions = await _stationQueryService.SmartSearchStationNamesAsync(DepartStationInput);
            _logService?.Info("TrainTicketFormViewModelBase", $"[DEBUG] 搜索完成，找到 {suggestions.Count} 个建议");

            // 使用临时集合避免多次触发CollectionChanged事件
            var newSuggestions = new ObservableCollection<string>(suggestions);
            DepartStationSuggestions = newSuggestions;
            OnPropertyChanged(nameof(DepartStationSuggestions));

            // 有建议时打开下拉框
            var shouldOpen = DepartStationSuggestions.Count > 0;
            _logService?.Info("TrainTicketFormViewModelBase",
                $"[DEBUG] 建议数量: {DepartStationSuggestions.Count}, 是否打开下拉框: {shouldOpen}");
            IsDepartStationDropdownOpen = shouldOpen;
            DepartStationSelectedIndex = -1;

            _logService?.Info("TrainTicketFormViewModelBase",
                $"[DEBUG] IsDepartStationDropdownOpen 设置为: {IsDepartStationDropdownOpen}");
        }
        catch (Exception ex)
        {
            _logService?.Error("TrainTicketFormViewModelBase", $"[DEBUG] 搜索出发车站联想失败: {ex.Message}");
            _logService?.Error("TrainTicketFormViewModelBase", $"[DEBUG] 异常详情: {ex.StackTrace}");
        }
    }

    /// <summary>
    ///     异步搜索到达车站联想建议
    /// </summary>
    private async Task SearchArriveStationSuggestionsAsync()
    {
        _logService?.Info("TrainTicketFormViewModelBase",
            $"[DEBUG] SearchArriveStationSuggestionsAsync 开始执行，输入: '{ArriveStationInput}'");

        if (string.IsNullOrWhiteSpace(ArriveStationInput) || ArriveStationInput.Length < 1)
        {
            _logService?.Info("TrainTicketFormViewModelBase", "[DEBUG] 输入为空或长度小于1，清空建议并关闭下拉框");
            if (ArriveStationSuggestions.Count > 0)
                ArriveStationSuggestions.Clear();
            IsArriveStationDropdownOpen = false;
            return;
        }

        try
        {
            _logService?.Info("TrainTicketFormViewModelBase", $"[DEBUG] 开始搜索车站，关键词: '{ArriveStationInput}'");
            var suggestions = await _stationQueryService.SmartSearchStationNamesAsync(ArriveStationInput);
            _logService?.Info("TrainTicketFormViewModelBase", $"[DEBUG] 搜索完成，找到 {suggestions.Count} 个建议");

            // 使用临时集合避免多次触发CollectionChanged事件
            var newSuggestions = new ObservableCollection<string>(suggestions);
            ArriveStationSuggestions = newSuggestions;
            OnPropertyChanged(nameof(ArriveStationSuggestions));

            // 有建议时打开下拉框
            var shouldOpen = ArriveStationSuggestions.Count > 0;
            _logService?.Info("TrainTicketFormViewModelBase",
                $"[DEBUG] 建议数量: {ArriveStationSuggestions.Count}, 是否打开下拉框: {shouldOpen}");
            IsArriveStationDropdownOpen = shouldOpen;
            ArriveStationSelectedIndex = -1;

            _logService?.Info("TrainTicketFormViewModelBase",
                $"[DEBUG] IsArriveStationDropdownOpen 设置为: {IsArriveStationDropdownOpen}");
        }
        catch (Exception ex)
        {
            _logService?.Error("TrainTicketFormViewModelBase", $"[DEBUG] 搜索到达车站联想失败: {ex.Message}");
            _logService?.Error("TrainTicketFormViewModelBase", $"[DEBUG] 异常详情: {ex.StackTrace}");
        }
    }

    /// <summary>
    ///     出发车站文本改变命令（供 AutoCompleteTextBox 使用）
    /// </summary>
    [RelayCommand]
    private async Task DepartStationTextChanged(string keyword)
    {
        DepartStationInput = keyword;
        await SearchDepartStationSuggestionsAsync();
    }

    /// <summary>
    ///     到达车站文本改变命令（供 AutoCompleteTextBox 使用）
    /// </summary>
    [RelayCommand]
    private async Task ArriveStationTextChanged(string keyword)
    {
        ArriveStationInput = keyword;
        await SearchArriveStationSuggestionsAsync();
    }

    /// <summary>
    ///     选择出发车站联想项
    /// </summary>
    [RelayCommand]
    public void SelectDepartStation(string suggestion)
    {
        if (string.IsNullOrEmpty(suggestion))
            return;

        _isProcessingLinkedChanges = true;
        try
        {
            DepartStationInput = suggestion;
            IsDepartStationDropdownOpen = false;
            DepartStationSuggestions.Clear();

            // 触发车站信息查询
            _ = QueryDepartStationInfoAsync();
        }
        finally
        {
            _isProcessingLinkedChanges = false;
        }
    }

    /// <summary>
    ///     选择到达车站联想项
    /// </summary>
    [RelayCommand]
    public void SelectArriveStation(string suggestion)
    {
        if (string.IsNullOrEmpty(suggestion))
            return;

        _isProcessingLinkedChanges = true;
        try
        {
            ArriveStationInput = suggestion;
            IsArriveStationDropdownOpen = false;
            ArriveStationSuggestions.Clear();

            // 触发车站信息查询
            _ = QueryArriveStationInfoAsync();
        }
        finally
        {
            _isProcessingLinkedChanges = false;
        }
    }
}
