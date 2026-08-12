using System;
using System.Collections.Generic;
using System.IO;
using GuiPiao.Services;
using GuiPiao.Utils;
using Microsoft.Data.Sqlite;

namespace GuiPiao.DataAccess;

public static class Database
{
    private static readonly Lazy<LogService> _logService = new(() => new LogService());

    public static void Initialize()
    {
        try
        {
            var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "guipiao.db");
            var dbDir = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(dbDir) && !Directory.Exists(dbDir))
            {
                Directory.CreateDirectory(dbDir);
                _logService.Value.Info("Database", $"创建数据库目录: {dbDir}");
            }

            CreateTables();
        }
        catch (Exception ex)
        {
            _logService.Value.Error("Database", $"数据库初始化失败: {ex.Message}");
            throw;
        }
    }

    private static void CreateTables()
    {
        using (var connection = new SqliteConnection(ConfigManager.Instance.DatabaseConnectionString))
        {
            try
            {
                connection.Open();
            }
            catch (Exception ex)
            {
                _logService.Value.Error("Database", $"数据库连接失败: {ex.Message}");
                throw;
            }

            try
            {
                var createStationTable = @"
                    CREATE TABLE IF NOT EXISTS station_info (
                        id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                        station_name TEXT,
                        province TEXT,
                        city TEXT,
                        district TEXT,
                        station_code TEXT,
                        station_pinyin TEXT,
                        station_level INTEGER,
                        railway_bureau TEXT,
                        longitude TEXT,
                        latitude TEXT
                    );

                    CREATE INDEX IF NOT EXISTS idx_station_name ON station_info (station_name);
                    CREATE INDEX IF NOT EXISTS idx_station_code ON station_info (station_code);
                    CREATE INDEX IF NOT EXISTS idx_station_pinyin ON station_info (station_pinyin);
                ";

                var createTrainTable = @"
                    CREATE TABLE IF NOT EXISTS train_ride_info (
                        id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                        ticket_number TEXT,
                        check_in_location TEXT,
                        depart_station TEXT,
                        train_no TEXT,
                        arrive_station TEXT,
                        depart_station_pinyin TEXT,
                        arrive_station_pinyin TEXT,
                        depart_date TEXT,
                        depart_time TEXT,
                        coach_no TEXT,
                        seat_no TEXT,
                        money REAL,
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
                        arrive_time TEXT,
                        arrive_day_offset INTEGER DEFAULT 0,
                        FOREIGN KEY (arrive_station_code) REFERENCES station_info (station_code) ON DELETE CASCADE ON UPDATE CASCADE,
                        FOREIGN KEY (depart_station_code) REFERENCES station_info (station_code) ON DELETE CASCADE ON UPDATE CASCADE
                    );

                    CREATE INDEX IF NOT EXISTS idx_depart_station_code ON train_ride_info (depart_station_code);
                    CREATE INDEX IF NOT EXISTS idx_arrive_station_code ON train_ride_info (arrive_station_code);
                    CREATE INDEX IF NOT EXISTS idx_train_no_date ON train_ride_info (train_no, depart_date);
                    CREATE INDEX IF NOT EXISTS idx_depart_station_date ON train_ride_info (depart_station, depart_date);
                ";

                using (var command = new SqliteCommand(createStationTable, connection))
                {
                    command.ExecuteNonQuery();
                }

                using (var command = new SqliteCommand(createTrainTable, connection))
                {
                    command.ExecuteNonQuery();
                }

                var createLogTable = @"
                    CREATE TABLE IF NOT EXISTS system_log (
                        id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                        time TEXT NOT NULL,
                        level INTEGER NOT NULL DEFAULT 1,
                        module TEXT,
                        content TEXT NOT NULL,
                        created_at TEXT NOT NULL
                    );

                    CREATE INDEX IF NOT EXISTS idx_log_time ON system_log (time);
                    CREATE INDEX IF NOT EXISTS idx_log_level ON system_log (level);
                    CREATE INDEX IF NOT EXISTS idx_log_module ON system_log (module);
                    CREATE INDEX IF NOT EXISTS idx_log_created_at ON system_log (created_at);
                ";

                using (var command = new SqliteCommand(createLogTable, connection))
                {
                    command.ExecuteNonQuery();
                }

                var createTagTable = @"
                    CREATE TABLE IF NOT EXISTS ticket_tag (
                        id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                        name TEXT NOT NULL,
                        color TEXT,
                        text_color TEXT,
                        sort_order INTEGER DEFAULT 0,
                        created_at TEXT
                    );

                    CREATE INDEX IF NOT EXISTS idx_tag_sort_order ON ticket_tag (sort_order);
                ";

                using (var command = new SqliteCommand(createTagTable, connection))
                {
                    command.ExecuteNonQuery();
                }

                var createRideTagTable = @"
                    CREATE TABLE IF NOT EXISTS train_ride_tag (
                        id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                        train_ride_id INTEGER NOT NULL,
                        tag_id INTEGER NOT NULL,
                        created_at TEXT,
                        FOREIGN KEY (train_ride_id) REFERENCES train_ride_info (id) ON DELETE CASCADE,
                        FOREIGN KEY (tag_id) REFERENCES ticket_tag (id) ON DELETE CASCADE,
                        UNIQUE(train_ride_id, tag_id)
                    );

                    CREATE INDEX IF NOT EXISTS idx_ride_tag_ride_id ON train_ride_tag (train_ride_id);
                    CREATE INDEX IF NOT EXISTS idx_ride_tag_tag_id ON train_ride_tag (tag_id);
                ";

                using (var command = new SqliteCommand(createRideTagTable, connection))
                {
                    command.ExecuteNonQuery();
                }

                // 迁移：为已存在的表添加新列
                MigrateDatabase(connection);

                _logService.Value.Info("Database", "数据库表创建完成");
            }
            catch (Exception ex)
            {
                _logService.Value.Error("Database", $"数据库表创建失败: {ex.Message}");
                throw;
            }
        }
    }

    /// <summary>
    ///     数据库迁移：为现有表添加新列
    /// </summary>
    private static void MigrateDatabase(SqliteConnection connection)
    {
        try
        {
            // 检查并添加 status 列到 train_ride_info 表
            var checkStatusColumn = @"
                    SELECT COUNT(*) FROM pragma_table_info('train_ride_info') WHERE name = 'status';
                ";
            using (var command = new SqliteCommand(checkStatusColumn, connection))
            {
                var count = Convert.ToInt32(command.ExecuteScalar());
                if (count == 0)
                {
                    var addStatusColumn = @"
                            ALTER TABLE train_ride_info ADD COLUMN status INTEGER DEFAULT 0;
                        ";
                    using (var alterCommand = new SqliteCommand(addStatusColumn, connection))
                    {
                        alterCommand.ExecuteNonQuery();
                    }

                    _logService.Value.Info("Database", "已添加 status 列到 train_ride_info 表");
                }
            }

            // 检查并添加 idx_status 索引
            var checkStatusIndex = @"
                    SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = 'idx_status';
                ";
            using (var command = new SqliteCommand(checkStatusIndex, connection))
            {
                var count = Convert.ToInt32(command.ExecuteScalar());
                if (count == 0)
                {
                    var createStatusIndex = @"
                            CREATE INDEX IF NOT EXISTS idx_status ON train_ride_info (status);
                        ";
                    using (var indexCommand = new SqliteCommand(createStatusIndex, connection))
                    {
                        indexCommand.ExecuteNonQuery();
                    }

                    _logService.Value.Info("Database", "已创建 idx_status 索引");
                }
            }

            // 检查并添加 arrive_time 列
            var checkArriveTimeColumn = @"
                    SELECT COUNT(*) FROM pragma_table_info('train_ride_info') WHERE name = 'arrive_time';
                ";
            using (var command = new SqliteCommand(checkArriveTimeColumn, connection))
            {
                var count = Convert.ToInt32(command.ExecuteScalar());
                if (count == 0)
                {
                    var addArriveTimeColumn = @"
                            ALTER TABLE train_ride_info ADD COLUMN arrive_time TEXT;
                        ";
                    using (var alterCommand = new SqliteCommand(addArriveTimeColumn, connection))
                    {
                        alterCommand.ExecuteNonQuery();
                    }

                    _logService.Value.Info("Database", "已添加 arrive_time 列到 train_ride_info 表");
                }
            }

            // 检查并添加 arrive_day_offset 列（相对出发日的跨天数）
            var checkArriveDayOffsetColumn = @"
                    SELECT COUNT(*) FROM pragma_table_info('train_ride_info') WHERE name = 'arrive_day_offset';
                ";
            using (var command = new SqliteCommand(checkArriveDayOffsetColumn, connection))
            {
                var count = Convert.ToInt32(command.ExecuteScalar());
                if (count == 0)
                {
                    var addArriveDayOffsetColumn = @"
                            ALTER TABLE train_ride_info ADD COLUMN arrive_day_offset INTEGER DEFAULT 0;
                        ";
                    using (var alterCommand = new SqliteCommand(addArriveDayOffsetColumn, connection))
                    {
                        alterCommand.ExecuteNonQuery();
                    }

                    _logService.Value.Info("Database", "已添加 arrive_day_offset 列到 train_ride_info 表");
                }
            }

            // 将已有行程的日期/时间归一化为 yyyy-MM-dd / HH:mm
            NormalizeExistingRideDateTimes(connection);
        }
        catch (Exception ex)
        {
            _logService.Value.Error("Database", $"数据库迁移失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    ///     把历史杂乱的日期/时间字符串规范为 yyyy-MM-dd / HH:mm（幂等）。
    /// </summary>
    private static void NormalizeExistingRideDateTimes(SqliteConnection connection)
    {
        using var select = new SqliteCommand(
            "SELECT id, depart_date, depart_time, arrive_time FROM train_ride_info",
            connection);
        using var reader = select.ExecuteReader();
        var updates = new List<(long Id, string DepartDate, string DepartTime, string ArriveTime)>();
        while (reader.Read())
        {
            var id = reader.GetInt64(0);
            var departDate = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
            var departTime = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
            var arriveTime = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);

            var normDate = RideDateTime.NormalizeDate(departDate);
            var normDepartTime = RideDateTime.NormalizeTime(departTime);
            var normArriveTime = RideDateTime.NormalizeTime(arriveTime);

            if (normDate != departDate || normDepartTime != departTime || normArriveTime != arriveTime)
                updates.Add((id, normDate, normDepartTime, normArriveTime));
        }

        reader.Close();

        if (updates.Count == 0)
            return;

        using var update = connection.CreateCommand();
        update.CommandText =
            @"UPDATE train_ride_info
              SET depart_date = @DepartDate, depart_time = @DepartTime, arrive_time = @ArriveTime
              WHERE id = @Id";
        var pId = update.Parameters.Add("@Id", SqliteType.Integer);
        var pDate = update.Parameters.Add("@DepartDate", SqliteType.Text);
        var pDepartTime = update.Parameters.Add("@DepartTime", SqliteType.Text);
        var pArriveTime = update.Parameters.Add("@ArriveTime", SqliteType.Text);

        foreach (var row in updates)
        {
            pId.Value = row.Id;
            pDate.Value = row.DepartDate;
            pDepartTime.Value = string.IsNullOrEmpty(row.DepartTime) ? (object)DBNull.Value : row.DepartTime;
            pArriveTime.Value = string.IsNullOrEmpty(row.ArriveTime) ? (object)DBNull.Value : row.ArriveTime;
            update.ExecuteNonQuery();
        }

        _logService.Value.Info("Database", $"已归一化 {updates.Count} 条行程的日期/时间格式");
    }

    public static SqliteConnection GetConnection()
    {
        return new SqliteConnection(ConfigManager.Instance.DatabaseConnectionString);
    }
}