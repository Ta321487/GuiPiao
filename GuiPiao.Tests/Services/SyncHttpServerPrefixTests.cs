using GuiPiao.Services;
using Xunit;

namespace GuiPiao.Tests.Services;

public class SyncHttpServerPrefixTests
{
    [Fact]
    public void BuildPrefixes_Default_IsLoopbackOnly()
    {
        var prefixes = SyncHttpServer.BuildPrefixes(17880, allowLan: false);
        Assert.Single(prefixes);
        Assert.Equal("http://127.0.0.1:17880", prefixes[0]);
    }

    [Fact]
    public void BuildPrefixes_AllowLan_IncludesLoopback()
    {
        var prefixes = SyncHttpServer.BuildPrefixes(17880, allowLan: true);
        Assert.Contains("http://127.0.0.1:17880", prefixes);
        Assert.True(prefixes.Count >= 1);
    }

    [Fact]
    public void GetPreferredBaseUrl_WithoutLan_IsLoopback()
    {
        Assert.Equal("http://127.0.0.1:17880", SyncHttpServer.GetPreferredBaseUrl(17880, preferLan: false));
    }
}
