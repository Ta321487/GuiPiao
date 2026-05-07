using System.Collections.Generic;
using GuiPiao.Model;

namespace GuiPiao.Messages;

/// <summary>
///     DataGrid列配置变更消息
/// </summary>
public class DataGridColumnsChangedMessage
{
    public DataGridColumnsChangedMessage(List<DataGridColumnConfig> columnConfigs)
    {
        ColumnConfigs = columnConfigs;
    }

    /// <summary>
    ///     列配置列表
    /// </summary>
    public List<DataGridColumnConfig> ColumnConfigs { get; }
}