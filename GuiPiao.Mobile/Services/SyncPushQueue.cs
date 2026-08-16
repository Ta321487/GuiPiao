using GuiPiao.Model.Sync;

namespace GuiPiao.Mobile.Services;

/// <summary>待推送到 PC 的本地变更队列（AppData）。</summary>
public sealed class SyncPushQueue
{
    private readonly string _path;

    public SyncPushQueue()
    {
        var dir = Path.Combine(FileSystem.AppDataDirectory, "Sync");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "pending_push.json");
    }

    public void Append(SyncChangeDto change)
    {
        if (string.IsNullOrWhiteSpace(change.ChangeId)) return;
        var existing = Load().ToList();
        existing.RemoveAll(c =>
            string.Equals(c.ChangeId, change.ChangeId, StringComparison.OrdinalIgnoreCase));
        // 同实体后续写覆盖未推送的同 sync_id（含 upsert→delete）
        existing.RemoveAll(c =>
            string.Equals(c.Entity, change.Entity, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(c.SyncId, change.SyncId, StringComparison.OrdinalIgnoreCase));
        existing.Add(change);
        File.WriteAllText(_path, SyncJson.ToJson(existing));
    }

    public void Append(string entity, string op, string syncId, string? payload, string updatedAt) =>
        Append(new SyncChangeDto
        {
            ChangeId = Guid.NewGuid().ToString("D"),
            Entity = entity,
            SyncId = syncId,
            Op = op,
            Payload = payload,
            UpdatedAt = updatedAt
        });


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
