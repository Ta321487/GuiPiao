using GuiPiao.Mobile.Data;
using GuiPiao.Mobile.Model;
using GuiPiao.Model.Sync;

namespace GuiPiao.Mobile.Services;

/// <summary>手机侧行程写路径：本地库 + 待推队列（对齐时 push，禁止静默丢变更）。</summary>
public sealed class RideWriteService
{
    private readonly MobileDatabase _db;
    private readonly RideRepository _rides;
    private readonly TagRepository _tags;
    private readonly StationCacheRepository _stations;
    private readonly SyncPushQueue _pushQueue;

    public RideWriteService(
        MobileDatabase db,
        RideRepository rides,
        TagRepository tags,
        StationCacheRepository stations,
        SyncPushQueue pushQueue)
    {
        _db = db;
        _rides = rides;
        _tags = tags;
        _stations = stations;
        _pushQueue = pushQueue;
    }

    public MobileRide SaveUpsert(MobileRide ride, IReadOnlyList<string>? tagSyncIds = null)
    {
        return _db.WithWriteLock(() =>
        {
            if (string.IsNullOrWhiteSpace(ride.SyncId))
                ride.SyncId = Guid.NewGuid().ToString("D");
            ride.UpdatedAt = DateTime.UtcNow.ToString("o");
            ride.DeletedAt = null;
            _rides.Upsert(ride);
            _stations.Upsert(ride.DepartStation, ride.DepartStationCode, ride.DepartStationPinyin);
            _stations.Upsert(ride.ArriveStation, ride.ArriveStationCode, ride.ArriveStationPinyin);
            _pushQueue.Append(
                SyncEntityTypes.Ride, SyncOps.Upsert, ride.SyncId,
                MobileSyncPayloadParser.SerializeRide(ride), ride.UpdatedAt);

            if (tagSyncIds != null)
            {
                _tags.ReplaceRideTags(ride.SyncId, tagSyncIds);
                _pushQueue.Append(
                    SyncEntityTypes.RideTags, SyncOps.Upsert, ride.SyncId,
                    SyncJson.ToJson(new { RideSyncId = ride.SyncId, TagSyncIds = tagSyncIds.ToList() }),
                    ride.UpdatedAt);
            }

            return ride;
        });
    }

    public void SoftDelete(string syncId)
    {
        _db.WithWriteLock(() =>
        {
            var at = DateTime.UtcNow.ToString("o");
            _rides.SoftDelete(syncId, at);
            _pushQueue.Append(SyncEntityTypes.Ride, SyncOps.Delete, syncId, null, at);
        });
    }

    public MobileRide? UpdateStatus(string syncId, int status)
    {
        var ride = _rides.GetBySyncId(syncId);
        if (ride == null) return null;
        ride.Status = status;
        return SaveUpsert(ride);
    }
}
