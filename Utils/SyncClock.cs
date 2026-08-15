using System;

namespace GuiPiao.Utils;

/// <summary>同步用时钟与 ID（UTC + UUID）。</summary>
public static class SyncClock
{
    public static string UtcNowIso() => DateTime.UtcNow.ToString("o");

    public static string NewSyncId() => Guid.NewGuid().ToString("D");

    public static string NewChangeId() => Guid.NewGuid().ToString("D");
}
