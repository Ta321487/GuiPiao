using System.Collections.Generic;

namespace GuiPiao.Model
{
    /// <summary>
    /// 图表数据模型 - 用于绑定到 LiveCharts
    /// </summary>
    public class ChartData
    {
        /// <summary>
        /// X轴标签（如：1月, 2月, 3月 或 G字头, D字头）
        /// </summary>
        public string[] Labels { get; set; } = System.Array.Empty<string>();

        /// <summary>
        /// Y轴数值
        /// </summary>
        public double[] Values { get; set; } = System.Array.Empty<double>();

        /// <summary>
        /// 数据系列名称
        /// </summary>
        public string SeriesName { get; set; } = string.Empty;

        /// <summary>
        /// 每个数据项的颜色（可选）
        /// </summary>
        public string[]? Colors { get; set; }

        /// <summary>
        /// 百分比值（用于饼图显示）
        /// </summary>
        public double[]? Percentages { get; set; }

        /// <summary>
        /// 文本列表项（用于文本列表显示）
        /// </summary>
        public List<TextListItem>? TextListItems { get; set; }

        /// <summary>
        /// 对比数值（用于年度出行总结等对比功能）
        /// </summary>
        public double[]? ComparisonValues { get; set; }
    }

    /// <summary>
    /// 文本列表项
    /// </summary>
    public class TextListItem
    {
        /// <summary>
        /// 标签
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// 数值
        /// </summary>
        public string Value { get; set; } = string.Empty;
    }
}
