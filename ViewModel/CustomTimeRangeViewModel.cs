using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace GuiPiao.ViewModel
{
    /// <summary>
    /// 自定义时间范围对话框视图模型
    /// </summary>
    public partial class CustomTimeRangeViewModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ErrorMessage))]
        [NotifyPropertyChangedFor(nameof(IsValid))]
        [NotifyPropertyChangedFor(nameof(TimeGranularityHint))]
        private DateTime? _startDate;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ErrorMessage))]
        [NotifyPropertyChangedFor(nameof(IsValid))]
        [NotifyPropertyChangedFor(nameof(TimeGranularityHint))]
        private DateTime? _endDate;

        /// <summary>
        /// 默认开始日期（从数据库获取的最早车票日期）
        /// </summary>
        public DateTime? DefaultStartDate { get; set; }

        /// <summary>
        /// 默认结束日期（今天）
        /// </summary>
        public DateTime? DefaultEndDate { get; set; }

        /// <summary>
        /// 错误信息文本
        /// </summary>
        public string ErrorMessage => GetValidationErrorMessage();

        /// <summary>
        /// 是否验证通过
        /// </summary>
        public bool IsValid => string.IsNullOrEmpty(ErrorMessage);

        /// <summary>
        /// 时间粒度提示文本
        /// </summary>
        public string TimeGranularityHint => GetTimeGranularityHint();

        /// <summary>
        /// 当前的时间粒度（由外部设置）
        /// </summary>
        public string TimeGranularity { get; set; } = string.Empty;

        /// <summary>
        /// 获取验证错误信息
        /// </summary>
        private string GetValidationErrorMessage()
        {
            if (StartDate == null || EndDate == null)
                return "⚠️ 请选择开始日期和结束日期";

            if (StartDate.Value.Date > EndDate.Value.Date)
                return "⚠️ 开始日期不能晚于结束日期";

            if (EndDate.Value.Date > DateTime.Now.Date)
                return "⚠️ 结束日期不能晚于今天";

            return string.Empty;
        }

        /// <summary>
        /// 根据时间粒度获取提示文本
        /// </summary>
        private string GetTimeGranularityHint()
        {
            return TimeGranularity switch
            {
                "自然月" => "💡 建议：当前时间粒度为【自然月】，建议选择完整的月份以获得最佳展示效果",
                "自然周" => "💡 建议：当前时间粒度为【自然周】，建议选择完整的周（周一至周日）以获得最佳展示效果",
                "季度" => "💡 建议：当前时间粒度为【季度】，建议选择完整的季度（1-3月、4-6月等）以获得最佳展示效果",
                "半年" => "💡 建议：当前时间粒度为【半年】，建议选择完整的半年（1-6月或7-12月）以获得最佳展示效果",
                "自定义" => "💡 当前时间粒度为【自定义】，可自由选择任意时间范围",
                _ => string.Empty
            };
        }
    }
}
