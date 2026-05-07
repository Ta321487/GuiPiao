using GuiPiao.Model;
using GuiPiao.Services;
using System;

namespace GuiPiao.ViewModel.TrainTicketForm
{
    /// <summary>
    /// 默认值加载器
    /// </summary>
    public class DefaultValueLoader
    {
        private readonly GeneralSettingsService _settingsService;
        private readonly OptionsProvider _optionsProvider;

        public DefaultValueLoader(GeneralSettingsService settingsService, OptionsProvider optionsProvider)
        {
            _settingsService = settingsService;
            _optionsProvider = optionsProvider;
        }

        /// <summary>
        /// 加载默认值
        /// </summary>
        public void LoadDefaults(TrainTicketFormData data, bool isStatusVisible)
        {
            var config = _settingsService.Config;

            // 加载默认席别
            var defaultSeatType = GetSeatTypeString(config.DefaultSeatType);
            if (!string.IsNullOrEmpty(defaultSeatType) && _optionsProvider.SeatTypeOptions.Contains(defaultSeatType))
            {
                data.SeatType = defaultSeatType;
            }

            // 加载默认状态（仅在新增窗口）
            if (isStatusVisible)
            {
                var defaultStatus = GetStatusString(config.DefaultTicketStatus);
                if (!string.IsNullOrEmpty(defaultStatus) && _optionsProvider.StatusOptions.Contains(defaultStatus))
                {
                    data.SelectedStatus = defaultStatus;
                }
            }

            // 加载默认日期（今天）
            data.DepartDateTime = DateTime.Now;
            data.DepartTimeValue = DateTime.Now;

            // 其他默认值...
            data.TicketModificationType = "";
            data.AdditionalInfo = "";
            data.TicketPurpose = "";
        }

        /// <summary>
        /// 根据设置重新排序选项
        /// </summary>
        public void ReorderOptionsBySettings()
        {
            var config = _settingsService.Config;

            // 重新排序席别选项
            var defaultSeatType = GetSeatTypeString(config.DefaultSeatType);
            _optionsProvider.ReorderSeatTypeOptions(defaultSeatType);

            // 重新排序状态选项
            var defaultStatus = GetStatusString(config.DefaultTicketStatus);
            _optionsProvider.ReorderStatusOptions(defaultStatus);
        }

        /// <summary>
        /// 将席别枚举转换为字符串
        /// </summary>
        private string GetSeatTypeString(DefaultSeatTypeOption option)
        {
            return option switch
            {
                DefaultSeatTypeOption.SecondClass => "二等座",
                DefaultSeatTypeOption.FirstClass => "一等座",
                DefaultSeatTypeOption.BusinessClass => "商务座",
                DefaultSeatTypeOption.PremiumClass => "特等座",
                DefaultSeatTypeOption.NewACHardSeat => "新空调硬座",
                DefaultSeatTypeOption.SoftSeat => "软座",
                DefaultSeatTypeOption.NewACHardSleeper => "新空调硬卧",
                DefaultSeatTypeOption.NewACSoftSleeper => "新空调软卧",
                DefaultSeatTypeOption.HardSleeperAsSeat => "硬卧代硬座",
                _ => "二等座"
            };
        }

        /// <summary>
        /// 将状态枚举转换为字符串
        /// </summary>
        private string GetStatusString(DefaultTicketStatusOption option)
        {
            return option switch
            {
                DefaultTicketStatusOption.Completed => "已完成",
                DefaultTicketStatusOption.NotTraveled => "未出行",
                _ => "已完成"
            };
        }
    }
}
