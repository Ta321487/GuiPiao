using Microsoft.Data.Sqlite;

namespace GuiPiao.Mobile.Data;

/// <summary>手机侧 SQLite（AppData）；结构覆盖同步所需行程/标签字段。</summary>
public sealed class MobileDatabase
{
    private readonly string _path;
    private readonly object _initLock = new();
    private readonly SemaphoreSlim _writeGate = new(1, 1);
    private bool _initialized;

    public MobileDatabase()
    {
        var dir = Path.Combine(FileSystem.AppDataDirectory, "Data");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "guipiao_mobile.db");
    }

    /// <summary>Default Timeout 对应 busy 等待，避免对齐写库与列表刷新撞车抛 SQLITE_BUSY。</summary>
    public string ConnectionString =>
        $"Data Source={_path};Mode=ReadWriteCreate;Cache=Shared;Default Timeout=5";

    public void EnsureCreated()
    {
        if (_initialized) return;
        lock (_initLock)
        {
            if (_initialized) return;
            SQLitePCL.Batteries_V2.Init();

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            using (var pragma = connection.CreateCommand())
            {
                pragma.CommandText =
                    """
                    PRAGMA journal_mode=WAL;
                    PRAGMA synchronous=NORMAL;
                    """;
                pragma.ExecuteNonQuery();
            }

            using var cmd = connection.CreateCommand();
            cmd.CommandText =
                """
                CREATE TABLE IF NOT EXISTS train_ride_info (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    sync_id TEXT NOT NULL UNIQUE,
                    ticket_number TEXT,
                    check_in_location TEXT,
                    depart_station TEXT,
                    train_no TEXT,
                    arrive_station TEXT,
                    depart_station_pinyin TEXT,
                    arrive_station_pinyin TEXT,
                    depart_date TEXT,
                    depart_time TEXT,
                    arrive_time TEXT,
                    arrive_day_offset INTEGER DEFAULT 0,
                    coach_no TEXT,
                    seat_no TEXT,
                    money REAL DEFAULT 0,
                    seat_type TEXT,
                    additional_info TEXT,
                    ticket_purpose TEXT,
                    ticket_modification_type TEXT,
                    ticket_type_flags INTEGER DEFAULT 0,
                    payment_channel_flags INTEGER DEFAULT 0,
                    hint TEXT,
                    depart_station_code TEXT,
                    arrive_station_code TEXT,
                    status INTEGER DEFAULT 0,
                    updated_at TEXT,
                    deleted_at TEXT
                );
                CREATE INDEX IF NOT EXISTS idx_ride_depart_date ON train_ride_info(depart_date DESC);
                CREATE INDEX IF NOT EXISTS idx_ride_deleted ON train_ride_info(deleted_at);

                CREATE TABLE IF NOT EXISTS ticket_tag (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    sync_id TEXT NOT NULL UNIQUE,
                    name TEXT,
                    color TEXT,
                    text_color TEXT,
                    sort_order INTEGER DEFAULT 0,
                    is_default INTEGER DEFAULT 0,
                    updated_at TEXT,
                    deleted_at TEXT
                );

                CREATE TABLE IF NOT EXISTS ride_tag (
                    ride_sync_id TEXT NOT NULL,
                    tag_sync_id TEXT NOT NULL,
                    PRIMARY KEY (ride_sync_id, tag_sync_id)
                );

                CREATE TABLE IF NOT EXISTS station_cache (
                    station_name TEXT NOT NULL PRIMARY KEY,
                    station_code TEXT,
                    station_pinyin TEXT,
                    updated_at TEXT
                );
                """;
            cmd.ExecuteNonQuery();
            _initialized = true;
        }
    }

    public SqliteConnection OpenConnection()
    {
        EnsureCreated();
        var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        return connection;
    }

    /// <summary>串行化写路径；与 EnsureCreated 分锁，避免重入死锁。</summary>
    public T WithWriteLock<T>(Func<T> action)
    {
        _writeGate.Wait();
        try
        {
            EnsureCreated();
            return action();
        }
        finally
        {
            _writeGate.Release();
        }
    }

    public void WithWriteLock(Action action) =>
        WithWriteLock(() =>
        {
            action();
            return 0;
        });
}
