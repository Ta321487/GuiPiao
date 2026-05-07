using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace GuiPiao.ViewModel.TrainTicketForm;

/// <summary>
///     选项提供器 - 管理表单中的各种下拉选项
/// </summary>
public class OptionsProvider
{
    // 提示信息选项
    private readonly List<string> _hintOptions = new()
    {
        "",
        "报销凭证 遗失不补|退票改签时须交回车站",
        "买票请到12306发货请到95306|中国铁路祝您旅途愉快",
        "欢度国庆 祝福祖国|中国铁路祝您旅途愉快",
        "奋斗百年路启航新征程|热烈庆祝中国共产党成立100周年",
        "锦州银行欢迎您",
        "中国铁路沈阳局集团公司|团体订票电话024-12306",
        "自定义"
    };

    public OptionsProvider()
    {
        foreach (var hint in _hintOptions) HintOptions.Add(hint);
    }

    // 车次号前缀选项
    public ObservableCollection<string> TrainNoPrefixes { get; } = new()
    {
        "G", "C", "D", "Z", "T", "K", "L", "S", "纯数字"
    };

    // 席别选项
    public ObservableCollection<string> SeatTypeOptions { get; } = new()
    {
        "新空调硬座", "软座", "新空调硬卧", "新空调软卧", "商务座", "特等座", "一等座", "二等座", "硬卧代硬座"
    };

    // 座位字母选项
    public ObservableCollection<string> SeatLetterOptions { get; } = new();

    // 铺位选项（硬卧）
    public ObservableCollection<string> HardSleeperOptions { get; } = new()
    {
        "上铺", "中铺", "下铺"
    };

    // 铺位选项（软卧）
    public ObservableCollection<string> SoftSleeperOptions { get; } = new()
    {
        "上铺", "下铺"
    };

    // 附加信息选项
    public ObservableCollection<string> AdditionalInfoOptions { get; } = new()
    {
        "", "限乘当日当次车", "退票费"
    };

    // 车票用途选项
    public ObservableCollection<string> TicketPurposeOptions { get; } = new()
    {
        "", "仅供报销使用"
    };

    // 改签类型选项
    public ObservableCollection<string> TicketModificationTypeOptions { get; } = new()
    {
        "", "始发改签", "变更到站"
    };

    // 状态选项
    public ObservableCollection<string> StatusOptions { get; } = new()
    {
        "已完成", "未出行", "已改签", "已退票"
    };

    public ObservableCollection<string> HintOptions { get; } = new();

    /// <summary>
    ///     获取座位字母/铺位选项（根据席别）
    /// </summary>
    public ObservableCollection<string> GetSeatLetterOptions(string seatType)
    {
        var options = new ObservableCollection<string>();

        switch (seatType)
        {
            case "二等座":
                options.Add("A");
                options.Add("B");
                options.Add("C");
                options.Add("D");
                options.Add("F");
                break;
            case "一等座":
                options.Add("A");
                options.Add("C");
                options.Add("D");
                options.Add("F");
                break;
            case "商务座":
            case "特等座":
                options.Add("A");
                options.Add("C");
                options.Add("F");
                break;
            case "硬卧代硬座":
                options.Add("A");
                options.Add("B");
                options.Add("C");
                options.Add("D");
                break;
            case "新空调硬卧":
                // 硬卧显示铺位选项
                options.Add("上铺");
                options.Add("中铺");
                options.Add("下铺");
                break;
            case "新空调软卧":
                // 软卧显示铺位选项
                options.Add("上铺");
                options.Add("下铺");
                break;
        }

        return options;
    }

    /// <summary>
    ///     判断是否为高铁/动车类席别（需要2位数字+字母）
    /// </summary>
    public bool IsHighSpeedSeatType(string seatType)
    {
        return seatType == "二等座" ||
               seatType == "一等座" ||
               seatType == "商务座" ||
               seatType == "特等座" ||
               seatType == "硬卧代硬座";
    }

    /// <summary>
    ///     判断是否为普速座席（需要3位数字，无字母）
    /// </summary>
    public bool IsSlowTrainSeatType(string seatType)
    {
        return seatType == "新空调硬座" ||
               seatType == "软座";
    }

    /// <summary>
    ///     判断是否为卧铺席别（需要3位数字+铺位）
    /// </summary>
    public bool IsSleeperSeatType(string seatType)
    {
        return seatType == "新空调硬卧" ||
               seatType == "新空调软卧";
    }

    /// <summary>
    ///     判断座位字母/铺位下拉框是否可见
    /// </summary>
    public bool IsSeatLetterVisible(string seatType)
    {
        return IsHighSpeedSeatType(seatType) || IsSleeperSeatType(seatType);
    }

    /// <summary>
    ///     获取座位号补齐位数（高铁2位，普速3位）
    /// </summary>
    public int GetSeatNumberPaddingLength(string seatType)
    {
        if (IsHighSpeedSeatType(seatType))
            return 2; // 高铁/动车类补齐2位
        return 3; // 普速类补齐3位
    }

    /// <summary>
    ///     更新车票用途选项（根据附加信息）
    /// </summary>
    public void UpdateTicketPurposeOptions(string additionalInfo, ObservableCollection<string> ticketPurposeOptions,
        string currentTicketPurpose)
    {
        // 计算新的选项列表
        var newOptions = new List<string>();

        if (additionalInfo == "限乘当日当次车")
        {
            // 限乘当日当次车时，车票用途只有空选项
            newOptions.Add("");
        }
        else if (additionalInfo == "退票费")
        {
            // 退票费时，显示仅供报销使用
            newOptions.Add("");
            newOptions.Add("仅供报销使用");
        }
        else
        {
            // 默认情况
            newOptions.Add("");
            newOptions.Add("仅供报销使用");
        }

        // 如果当前选中的值不在新列表中，且不为空，则保留它
        if (!string.IsNullOrEmpty(currentTicketPurpose) && !newOptions.Contains(currentTicketPurpose))
            newOptions.Add(currentTicketPurpose);

        // 智能更新：只添加新项，删除旧项，不直接Clear，避免WPF重置选中值
        UpdateCollection(ticketPurposeOptions, newOptions);
    }

    /// <summary>
    ///     更新附加信息选项（根据车票用途）
    /// </summary>
    public void UpdateAdditionalInfoOptions(string ticketPurpose, ObservableCollection<string> additionalInfoOptions,
        string currentAdditionalInfo)
    {
        // 计算新的选项列表
        var newOptions = new List<string>();

        if (ticketPurpose == "仅供报销使用")
        {
            // 仅供报销使用时，不显示"限乘当日当次车"
            newOptions.Add("");
            newOptions.Add("退票费");
        }
        else
        {
            // 默认情况，显示所有选项
            newOptions.Add("");
            newOptions.Add("限乘当日当次车");
            newOptions.Add("退票费");
        }

        // 如果当前选中的值不在新列表中，且不为空，则保留它
        if (!string.IsNullOrEmpty(currentAdditionalInfo) && !newOptions.Contains(currentAdditionalInfo))
            newOptions.Add(currentAdditionalInfo);

        // 智能更新：只添加新项，删除旧项，不直接Clear，避免WPF重置选中值
        UpdateCollection(additionalInfoOptions, newOptions);
    }

    /// <summary>
    ///     智能更新集合：只添加新项、删除旧项，不直接Clear
    ///     这样可以避免WPF在Clear时重置选中值
    /// </summary>
    private void UpdateCollection(ObservableCollection<string> collection, List<string> newItems)
    {
        // 先添加新列表中有但集合中没有的项
        foreach (var item in newItems)
            if (!collection.Contains(item))
                collection.Add(item);

        // 再删除集合中有但新列表中没有的项
        for (var i = collection.Count - 1; i >= 0; i--)
            if (!newItems.Contains(collection[i]))
                collection.RemoveAt(i);
    }

    /// <summary>
    ///     添加自定义提示信息
    /// </summary>
    public void AddCustomHint(string hint)
    {
        if (!string.IsNullOrEmpty(hint) && !HintOptions.Contains(hint))
        {
            // 在"自定义"之前插入
            var customIndex = HintOptions.IndexOf("自定义");
            if (customIndex >= 0)
                HintOptions.Insert(customIndex, hint);
            else
                HintOptions.Add(hint);
        }
    }

    /// <summary>
    ///     确保提示信息在选项列表中
    /// </summary>
    public void EnsureHintInOptions(string hint)
    {
        if (!string.IsNullOrEmpty(hint) && !HintOptions.Contains(hint))
        {
            var customIndex = HintOptions.IndexOf("自定义");
            if (customIndex >= 0)
                HintOptions.Insert(customIndex, hint);
            else
                HintOptions.Add(hint);
        }
    }

    /// <summary>
    ///     重新排序席别选项（将默认席别放在第一位）
    /// </summary>
    public void ReorderSeatTypeOptions(string defaultSeatType)
    {
        if (string.IsNullOrEmpty(defaultSeatType) || !SeatTypeOptions.Contains(defaultSeatType))
            return;

        var list = SeatTypeOptions.ToList();
        SeatTypeOptions.Clear();

        // 先添加默认席别
        SeatTypeOptions.Add(defaultSeatType);

        // 再添加其他席别
        foreach (var item in list)
            if (item != defaultSeatType)
                SeatTypeOptions.Add(item);
    }

    /// <summary>
    ///     重新排序状态选项（将默认状态放在第一位）
    /// </summary>
    public void ReorderStatusOptions(string defaultStatus)
    {
        if (string.IsNullOrEmpty(defaultStatus) || !StatusOptions.Contains(defaultStatus))
            return;

        var list = StatusOptions.ToList();
        StatusOptions.Clear();

        // 先添加默认状态
        StatusOptions.Add(defaultStatus);

        // 再添加其他状态
        foreach (var item in list)
            if (item != defaultStatus)
                StatusOptions.Add(item);
    }
}