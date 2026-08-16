using GuiPiao.Model.Sync;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace GuiPiao.Mobile.Services;

/// <summary>与 PC SyncPayloadSerializer 相同的 snake_case JSON 约定。</summary>
public static class SyncJson
{
    private static readonly JsonSerializerSettings Settings = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new SnakeCaseNamingStrategy()
        }
    };

    public static string ToJson(object value) => JsonConvert.SerializeObject(value, Settings);

    public static T? FromJson<T>(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return default;
        return JsonConvert.DeserializeObject<T>(json, Settings);
    }
}
