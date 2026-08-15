namespace GuiPiao.Model.Sync;

/// <summary>同步变更实体类型（协议契约，两端共用）。</summary>
public static class SyncEntityTypes
{
    public const string Ride = "ride";
    public const string Tag = "tag";

    /// <summary>行程标签全集替换（payload 含 ride_sync_id + tag_sync_ids）。</summary>
    public const string RideTags = "ride_tags";
}

public static class SyncOps
{
    public const string Upsert = "upsert";
    public const string Delete = "delete";
}

/// <summary>变更日志行（PC 权威 seq）。</summary>
public class SyncChangeRecord
{
    public string ChangeId { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public string SyncId { get; set; } = string.Empty;
    public string Op { get; set; } = string.Empty;
    public string? Payload { get; set; }
    public string UpdatedAt { get; set; } = string.Empty;
    public long Seq { get; set; }
    public string? DeviceId { get; set; }
}

/// <summary>已配对设备（token 仅存哈希）。</summary>
public class SyncPairedDevice
{
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string? LastSeenAt { get; set; }
    public bool Revoked { get; set; }
}
