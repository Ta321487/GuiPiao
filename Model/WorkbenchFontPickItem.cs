namespace GuiPiao.Model;

/// <summary>票面工作台字体下拉项（短列表：常用 / 当前 / 推荐）。</summary>
public sealed class WorkbenchFontPickItem
{
    public WorkbenchFontPickItem(string source, string display, string group)
    {
        Source = source ?? string.Empty;
        Display = display ?? string.Empty;
        Group = group ?? string.Empty;
    }

    /// <summary>写入布局的 FontFamily.Source；空串表示使用默认字体。</summary>
    public string Source { get; }

    public string Display { get; }

    /// <summary>分组标题：常用 / 当前 / 推荐。</summary>
    public string Group { get; }

    public override string ToString() => Display;
}
