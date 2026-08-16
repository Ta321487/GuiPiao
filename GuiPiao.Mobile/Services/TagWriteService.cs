using GuiPiao.Mobile.Data;
using GuiPiao.Mobile.Model;
using GuiPiao.Model.Sync;

namespace GuiPiao.Mobile.Services;

/// <summary>手机侧标签写路径：本地库 + 待推队列。</summary>
public sealed class TagWriteService
{
    private readonly MobileDatabase _db;
    private readonly TagRepository _tags;
    private readonly SyncPushQueue _pushQueue;

    public TagWriteService(MobileDatabase db, TagRepository tags, SyncPushQueue pushQueue)
    {
        _db = db;
        _tags = tags;
        _pushQueue = pushQueue;
    }

    public MobileTag SaveUpsert(MobileTag tag)
    {
        return _db.WithWriteLock(() =>
        {
            if (string.IsNullOrWhiteSpace(tag.SyncId))
                tag.SyncId = Guid.NewGuid().ToString("D");
            tag.UpdatedAt = DateTime.UtcNow.ToString("o");
            tag.DeletedAt = null;
            _tags.Upsert(tag);
            _pushQueue.Append(
                SyncEntityTypes.Tag, SyncOps.Upsert, tag.SyncId,
                MobileSyncPayloadParser.SerializeTag(tag), tag.UpdatedAt);
            return tag;
        });
    }

    public void SoftDelete(string syncId)
    {
        _db.WithWriteLock(() =>
        {
            var at = DateTime.UtcNow.ToString("o");
            _tags.SoftDelete(syncId, at);
            _pushQueue.Append(SyncEntityTypes.Tag, SyncOps.Delete, syncId, null, at);
        });
    }
}
