using GuiPiao.Model;
using Microsoft.Data.Sqlite;

namespace GuiPiao.Mobile.Data;

/// <summary>本地车站缓存（从行程字段积累；供表单联想与代码/拼音回填）。</summary>
public sealed class StationCacheRepository
{
    private readonly MobileDatabase _db;

    public StationCacheRepository(MobileDatabase db) => _db = db;

    public void Upsert(string name, string? code, string? pinyin)
    {
        var n = StationFormRules.ToStoredName(name);
        if (string.IsNullOrWhiteSpace(n)) return;

        using var connection = _db.OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            INSERT INTO station_cache (station_name, station_code, station_pinyin, updated_at)
            VALUES (@name, @code, @pinyin, @at)
            ON CONFLICT(station_name) DO UPDATE SET
                station_code = COALESCE(NULLIF(excluded.station_code, ''), station_cache.station_code),
                station_pinyin = COALESCE(NULLIF(excluded.station_pinyin, ''), station_cache.station_pinyin),
                updated_at = excluded.updated_at
            """;
        cmd.Parameters.AddWithValue("@name", n);
        cmd.Parameters.AddWithValue("@code", code ?? "");
        cmd.Parameters.AddWithValue("@pinyin", pinyin ?? "");
        cmd.Parameters.AddWithValue("@at", DateTime.UtcNow.ToString("o"));
        cmd.ExecuteNonQuery();
    }

    public void UpsertMany(IEnumerable<(string Name, string Code, string Pinyin)> stations)
    {
        using var connection = _db.OpenConnection();
        using var tx = connection.BeginTransaction();
        foreach (var s in stations)
        {
            var n = StationFormRules.ToStoredName(s.Name);
            if (string.IsNullOrWhiteSpace(n)) continue;

            using var cmd = connection.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText =
                """
                INSERT INTO station_cache (station_name, station_code, station_pinyin, updated_at)
                VALUES (@name, @code, @pinyin, @at)
                ON CONFLICT(station_name) DO UPDATE SET
                    station_code = COALESCE(NULLIF(excluded.station_code, ''), station_cache.station_code),
                    station_pinyin = COALESCE(NULLIF(excluded.station_pinyin, ''), station_cache.station_pinyin),
                    updated_at = excluded.updated_at
                """;
            cmd.Parameters.AddWithValue("@name", n);
            cmd.Parameters.AddWithValue("@code", s.Code ?? "");
            cmd.Parameters.AddWithValue("@pinyin", s.Pinyin ?? "");
            cmd.Parameters.AddWithValue("@at", DateTime.UtcNow.ToString("o"));
            cmd.ExecuteNonQuery();
        }

        tx.Commit();
    }

    public int Count()
    {
        using var connection = _db.OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(1) FROM station_cache";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public IReadOnlyList<StationCacheItem> Search(string keyword, int limit = 12)
    {
        var q = (keyword ?? "").Trim();
        if (q.Length == 0) return Array.Empty<StationCacheItem>();

        using var connection = _db.OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT station_name, station_code, station_pinyin FROM station_cache
            WHERE station_name LIKE @Q
               OR station_code LIKE @Q
               OR station_pinyin LIKE @Q
            ORDER BY
              CASE WHEN station_name = @Exact OR station_name = @ExactZhan THEN 0 ELSE 1 END,
              length(station_name),
              station_name
            LIMIT @Limit
            """;
        cmd.Parameters.AddWithValue("@Q", "%" + q + "%");
        cmd.Parameters.AddWithValue("@Exact", q);
        cmd.Parameters.AddWithValue("@ExactZhan", StationFormRules.ToStoredName(q));
        cmd.Parameters.AddWithValue("@Limit", limit);
        var list = new List<StationCacheItem>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new StationCacheItem
            {
                StationName = reader["station_name"]?.ToString() ?? "",
                StationCode = reader["station_code"]?.ToString() ?? "",
                StationPinyin = reader["station_pinyin"]?.ToString() ?? ""
            });
        }

        return list;
    }

    public StationCacheItem? FindExact(string name)
    {
        var n = (name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(n)) return null;
        var withZhan = StationFormRules.ToStoredName(n);
        using var connection = _db.OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT station_name, station_code, station_pinyin FROM station_cache
            WHERE station_name = @A OR station_name = @B
            LIMIT 1
            """;
        cmd.Parameters.AddWithValue("@A", n);
        cmd.Parameters.AddWithValue("@B", withZhan);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        return new StationCacheItem
        {
            StationName = reader["station_name"]?.ToString() ?? "",
            StationCode = reader["station_code"]?.ToString() ?? "",
            StationPinyin = reader["station_pinyin"]?.ToString() ?? ""
        };
    }
}

public sealed class StationCacheItem
{
    public string StationName { get; set; } = string.Empty;
    public string StationCode { get; set; } = string.Empty;
    public string StationPinyin { get; set; } = string.Empty;

    public string DisplayName => StationFormRules.ToNameBody(StationName);
}
