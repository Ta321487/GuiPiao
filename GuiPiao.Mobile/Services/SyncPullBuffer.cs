using GuiPiao.Model.Sync;

namespace GuiPiao.Mobile.Services;

/// <summary>拉取缓冲：对齐中断时可重试应用；成功入库后清空。</summary>
public sealed class SyncPullBuffer
{
    private readonly string _path;

    public SyncPullBuffer()
    {
        var dir = Path.Combine(FileSystem.AppDataDirectory, "Sync");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "pulled_changes.json");
    }

    public void Append(IEnumerable<SyncChangeDto> changes)
    {
        var existing = Load();
        var byId = existing.ToDictionary(c => c.ChangeId, StringComparer.OrdinalIgnoreCase);
        foreach (var c in changes)
        {
            if (string.IsNullOrWhiteSpace(c.ChangeId)) continue;
            byId[c.ChangeId] = c;
        }

        var merged = byId.Values.OrderBy(c => c.Seq).ToList();
        File.WriteAllText(_path, SyncJson.ToJson(merged));
    }

    public IReadOnlyList<SyncChangeDto> Load()
    {
        if (!File.Exists(_path)) return Array.Empty<SyncChangeDto>();
        try
        {
            return SyncJson.FromJson<List<SyncChangeDto>>(File.ReadAllText(_path))
                   ?? new List<SyncChangeDto>();
        }
        catch
        {
            return Array.Empty<SyncChangeDto>();
        }
    }

    public void Clear()
    {
        if (File.Exists(_path))
            File.Delete(_path);
    }

    public int Count => Load().Count;
}
