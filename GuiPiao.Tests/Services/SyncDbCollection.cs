using Xunit;

namespace GuiPiao.Tests.Services;

/// <summary>
///     Sync* 测试共用 ConfigManager 连接串覆盖，必须串行。
/// </summary>
[CollectionDefinition("SyncDb", DisableParallelization = true)]
public class SyncDbCollection
{
}
