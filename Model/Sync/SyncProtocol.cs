using System.Collections.Generic;

namespace GuiPiao.Model.Sync;

/// <summary>传输层协议 DTO（JSON snake_case，与 SyncPayloadSerializer 一致）。</summary>
public static class SyncProtocol
{
    public const string ApiVersion = "1";
    public const string DeviceIdHeader = "X-GuiPiao-Device-Id";
}

public class SyncPairRequest
{
    public string Code { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
}

public class SyncPairResponse
{
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceToken { get; set; } = string.Empty;
}

public class SyncHealthResponse
{
    public bool Ok { get; set; } = true;
    public string ApiVersion { get; set; } = SyncProtocol.ApiVersion;
    public long MaxSeq { get; set; }
}

public class SyncPullResponse
{
    public List<SyncChangeDto> Changes { get; set; } = new();
    public long MaxSeq { get; set; }
    public bool HasMore { get; set; }
}

public class SyncPushRequest
{
    public List<SyncChangeDto> Changes { get; set; } = new();
}

public class SyncPushResponse
{
    public int Accepted { get; set; }
    public int Skipped { get; set; }
    public long MaxSeq { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class SyncChangeDto
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

public class SyncErrorResponse
{
    public string Error { get; set; } = string.Empty;
}

public class SyncOcrRequest
{
    public string ImageBase64 { get; set; } = string.Empty;
    public string? FileName { get; set; }
}

public class SyncOcrResponse
{
    public string Text { get; set; } = string.Empty;
    public string SourceHint { get; set; } = "图片OCR";
}

public class SyncStationDto
{
    public string StationName { get; set; } = string.Empty;
    public string StationCode { get; set; } = string.Empty;
    public string StationPinyin { get; set; } = string.Empty;
}

public class SyncStationsResponse
{
    public List<SyncStationDto> Stations { get; set; } = new();
}

public class SyncConflictDto
{
    public long Id { get; set; }
    public string Entity { get; set; } = string.Empty;
    public string SyncId { get; set; } = string.Empty;
    public string Field { get; set; } = string.Empty;
    public string? LocalValue { get; set; }
    public string? RemoteValue { get; set; }
    public string? LocalUpdatedAt { get; set; }
    public string? RemoteUpdatedAt { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
}

public class SyncConflictListResponse
{
    public List<SyncConflictDto> Conflicts { get; set; } = new();
}

public class SyncConflictResolveRequest
{
    public long Id { get; set; }
    /// <summary>local = 保留 PC；remote = 采用手机推送稿。</summary>
    public string Keep { get; set; } = "local";
}

public class SyncConflictResolveResponse
{
    public bool Ok { get; set; }
    public string? Error { get; set; }
}
