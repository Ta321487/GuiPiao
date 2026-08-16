using System.Net.Http;
using System.Threading.Tasks;
using GuiPiao.Services;
using Xunit;

namespace GuiPiao.Tests.Services;

public class SyncHttpServerTcpTests
{
    [Fact]
    public async Task Start_Loopback_HealthOk()
    {
        var server = new SyncHttpServer();
        var port = 17890;
        try
        {
            server.Start(port, allowLan: false);
            Assert.True(server.IsRunning);
            Assert.False(server.AllowLan);

            using var http = new HttpClient { Timeout = System.TimeSpan.FromSeconds(5) };
            var json = await http.GetStringAsync($"http://127.0.0.1:{port}/v1/health");
            Assert.Contains("\"ok\":true", json.Replace(" ", string.Empty), System.StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            server.Stop();
            server.Dispose();
        }
    }
}
