using GuiPiao.Mobile.Data;
using GuiPiao.Mobile.Model;
using GuiPiao.Model.Sync;

namespace GuiPiao.Mobile.Services;

/// <summary>将 pull 到的变更写入手机 SQLite（副本，不回写 PC change_id）。</summary>
public sealed class MobileSyncIngressService
{
    private readonly MobileDatabase _db;
    private readonly RideRepository _rides;
    private readonly TagRepository _tags;
    private readonly StationCacheRepository _stations;

    public MobileSyncIngressService(
        MobileDatabase db,
        RideRepository rides,
        TagRepository tags,
        StationCacheRepository stations)
    {
        _db = db;
        _rides = rides;
        _tags = tags;
        _stations = stations;
    }

    public ApplyResult Apply(IEnumerable<SyncChangeDto> changes)
    {
        return _db.WithWriteLock(() =>
        {
            var result = new ApplyResult();
            foreach (var change in changes.OrderBy(c => c.Seq))
            {
                try
                {
                    ApplyOne(change);
                    result.Applied++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"{change.ChangeId}: {ex.Message}");
                }
            }

            return result;
        });
    }

    private void ApplyOne(SyncChangeDto change)
    {
        if (string.IsNullOrWhiteSpace(change.Entity) ||
            string.IsNullOrWhiteSpace(change.SyncId) ||
            string.IsNullOrWhiteSpace(change.Op))
            throw new InvalidOperationException("变更缺少 entity/sync_id/op");

        var entity = change.Entity.Trim().ToLowerInvariant();
        var op = change.Op.Trim().ToLowerInvariant();

        if (entity == SyncEntityTypes.Ride)
        {
            if (op == SyncOps.Delete)
            {
                _rides.SoftDelete(change.SyncId, change.UpdatedAt);
                return;
            }

            var ride = MobileSyncPayloadParser.ParseRide(change.Payload)
                       ?? throw new InvalidOperationException("ride payload 无效");
            if (!string.Equals(ride.SyncId, change.SyncId, StringComparison.Ordinal))
                ride.SyncId = change.SyncId;
            if (string.IsNullOrWhiteSpace(ride.UpdatedAt))
                ride.UpdatedAt = string.IsNullOrWhiteSpace(change.UpdatedAt)
                    ? DateTime.UtcNow.ToString("o")
                    : change.UpdatedAt;
            _rides.Upsert(ride);
            _stations.Upsert(ride.DepartStation, ride.DepartStationCode, ride.DepartStationPinyin);
            _stations.Upsert(ride.ArriveStation, ride.ArriveStationCode, ride.ArriveStationPinyin);
            return;
        }

        if (entity == SyncEntityTypes.Tag)
        {
            if (op == SyncOps.Delete)
            {
                _tags.SoftDelete(change.SyncId, change.UpdatedAt);
                return;
            }

            var tag = MobileSyncPayloadParser.ParseTag(change.Payload)
                      ?? throw new InvalidOperationException("tag payload 无效");
            if (!string.Equals(tag.SyncId, change.SyncId, StringComparison.Ordinal))
                tag.SyncId = change.SyncId;
            if (string.IsNullOrWhiteSpace(tag.UpdatedAt))
                tag.UpdatedAt = string.IsNullOrWhiteSpace(change.UpdatedAt)
                    ? DateTime.UtcNow.ToString("o")
                    : change.UpdatedAt;
            _tags.Upsert(tag);
            return;
        }

        if (entity == SyncEntityTypes.RideTags)
        {
            var payload = MobileSyncPayloadParser.ParseRideTags(change.Payload)
                          ?? throw new InvalidOperationException("ride_tags payload 无效");
            var rideSyncId = string.IsNullOrWhiteSpace(payload.RideSyncId)
                ? change.SyncId
                : payload.RideSyncId!;
            _tags.ReplaceRideTags(rideSyncId, payload.TagSyncIds ?? new List<string>());
            return;
        }

        throw new InvalidOperationException($"未知实体类型: {change.Entity}");
    }

    public sealed class ApplyResult
    {
        public int Applied { get; set; }
        public List<string> Errors { get; } = new();
    }
}
