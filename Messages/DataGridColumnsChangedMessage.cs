using GuiPiao.Model;
using System.Collections.Generic;

namespace GuiPiao.Messages
{
    /// <summary>
    /// DataGrid列配置变更消息
    /// </summary>
    public class DataGridColumnsChangedMessage
    {
        /// <summary>
        /// 列配置列表
        /// </summary>
        public List<DataGridColumnConfig> ColumnConfigs { get; }

        public DataGridColumnsChangedMessage(List<DataGridColumnConfig> columnConfigs)
        {
            ColumnConfigs = columnConfigs;
        }
    }
}
