using System;
using System.IO;
using GuiPiao.Services;
using Microsoft.Data.Sqlite;
using Xunit;

namespace GuiPiao.Tests.Services;

public class DatabaseValidationServiceTests : IDisposable
{
    private readonly string _dbPath;

    public DatabaseValidationServiceTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), "GuiPiao_Validate_" + Guid.NewGuid().ToString("N") + ".db");
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_dbPath)) File.Delete(_dbPath);
        }
        catch
        {
            // ignore
        }
    }

    [Fact]
    public void ValidateDatabase_文件不存在时失败()
    {
        var missing = Path.Combine(Path.GetTempPath(), "GuiPiao_NoSuch_" + Guid.NewGuid().ToString("N") + ".db");
        var sut = new DatabaseValidationService();
        var r = sut.ValidateDatabase(missing);
        Assert.False(r.IsValid);
        Assert.Contains("不存在", r.ErrorMessage ?? "");
    }

    [Fact]
    public void ValidateDatabase_扩展名非法时失败()
    {
        var badExt = Path.ChangeExtension(_dbPath, ".txt");
        File.WriteAllText(badExt, "");
        try
        {
            var sut = new DatabaseValidationService();
            var r = sut.ValidateDatabase(badExt);
            Assert.False(r.IsValid);
            Assert.Contains("格式", r.ErrorMessage ?? "");
        }
        finally
        {
            if (File.Exists(badExt)) File.Delete(badExt);
        }
    }

    [Fact]
    public void ValidateDatabase_缺表时失败()
    {
        using (var conn = new SqliteConnection($"Data Source={_dbPath}"))
        {
            conn.Open();
            using var cmd = new SqliteCommand(
                "CREATE TABLE station_info (id INTEGER, station_name TEXT, province TEXT, city TEXT, district TEXT, station_code TEXT, station_pinyin TEXT, station_level TEXT, railway_bureau TEXT, longitude TEXT, latitude TEXT);",
                conn);
            cmd.ExecuteNonQuery();
        }

        var sut = new DatabaseValidationService();
        var r = sut.ValidateDatabase(_dbPath);
        Assert.False(r.IsValid);
        Assert.Contains("表", r.ErrorMessage ?? "");
    }

    [Fact]
    public void ValidateDatabase_完整最小Schema时通过()
    {
        CreateMinimalSchema(_dbPath);
        var sut = new DatabaseValidationService();
        var r = sut.ValidateDatabase(_dbPath);
        Assert.True(r.IsValid, r.ErrorMessage);
    }

    private static void CreateMinimalSchema(string path)
    {
        if (File.Exists(path)) File.Delete(path);
        using var conn = new SqliteConnection($"Data Source={path}");
        conn.Open();
        foreach (var sql in SchemaSqlStatements)
        {
            using var cmd = new SqliteCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }
    }

    private static readonly string[] SchemaSqlStatements =
    {
        """
        CREATE TABLE station_info (
          id INTEGER PRIMARY KEY,
          station_name TEXT, province TEXT, city TEXT, district TEXT,
          station_code TEXT, station_pinyin TEXT, station_level TEXT, railway_bureau TEXT,
          longitude TEXT, latitude TEXT
        );
        """,
        """
        CREATE TABLE train_ride_info (
          id INTEGER PRIMARY KEY,
          ticket_number TEXT, check_in_location TEXT, depart_station TEXT, train_no TEXT,
          arrive_station TEXT, depart_station_pinyin TEXT, arrive_station_pinyin TEXT, depart_date TEXT,
          depart_time TEXT, coach_no TEXT, seat_no TEXT, money TEXT, seat_type TEXT, additional_info TEXT,
          ticket_purpose TEXT, ticket_modification_type TEXT, ticket_type_flags INTEGER, payment_channel_flags INTEGER,
          hint TEXT, depart_station_code TEXT, arrive_station_code TEXT, status INTEGER
        );
        """,
        """
        CREATE TABLE system_log (
          id INTEGER PRIMARY KEY,
          time TEXT, level INTEGER, module TEXT, content TEXT, created_at TEXT
        );
        """,
        """
        CREATE TABLE ticket_tag (
          id INTEGER PRIMARY KEY,
          name TEXT, color TEXT, text_color TEXT, sort_order INTEGER, created_at TEXT
        );
        """,
        """
        CREATE TABLE train_ride_tag (
          id INTEGER PRIMARY KEY,
          train_ride_id INTEGER, tag_id INTEGER, created_at TEXT
        );
        """
    };
}
