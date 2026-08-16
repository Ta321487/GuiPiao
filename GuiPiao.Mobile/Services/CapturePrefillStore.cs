using GuiPiao.Model;

namespace GuiPiao.Mobile.Services;

/// <summary>采集预填稿：打开表单前暂存，表单读取后清空（不落库）。</summary>
public sealed class CapturePrefillStore
{
    private TicketImportDraft? _draft;

    public void Set(TicketImportDraft draft) => _draft = draft;

    public TicketImportDraft? Take()
    {
        var d = _draft;
        _draft = null;
        return d;
    }

    public bool HasDraft => _draft != null;
}
