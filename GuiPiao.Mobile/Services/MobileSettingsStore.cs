using System.Text.Json;
using GuiPiao.Mobile.Model;

namespace GuiPiao.Mobile.Services;

/// <summary>本机 JSON 配置（AppData；主题/同步地址不同步）。</summary>
public sealed class MobileSettingsStore
{
    private const string AppearanceFile = "appearance.json";
    private const string SyncFile = "sync_client.json";

    private readonly string _dir;

    public MobileSettingsStore()
    {
        _dir = Path.Combine(FileSystem.AppDataDirectory, "Config");
        Directory.CreateDirectory(_dir);
    }

    public AppearanceConfig LoadAppearance() => Load<AppearanceConfig>(AppearanceFile) ?? new AppearanceConfig();

    public void SaveAppearance(AppearanceConfig config) => Save(AppearanceFile, config);

    public SyncClientConfig LoadSync() => Load<SyncClientConfig>(SyncFile) ?? new SyncClientConfig();

    public void SaveSync(SyncClientConfig config) => Save(SyncFile, config);

    private T? Load<T>(string fileName)
    {
        var path = Path.Combine(_dir, fileName);
        if (!File.Exists(path)) return default;
        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path));
        }
        catch
        {
            return default;
        }
    }

    private void Save<T>(string fileName, T value)
    {
        var path = Path.Combine(_dir, fileName);
        var json = JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }
}
