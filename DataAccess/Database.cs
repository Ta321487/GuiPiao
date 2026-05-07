using GuiPiao.Services;
using GuiPiao.Utils;
using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace GuiPiao.DataAccess
{
    public static class Database
    {
        private static readonly LogService _logService = new LogService();

        public static void Initialize()
        {
            string dbPath = Path.Combine(Directory.GetCurrentDirectory(), "guipiao.db");
            string dbDir = Path.GetDirectoryName(dbPath);
            if (!Directory.Exists(dbDir))
            {
                Directory.CreateDirectory(dbDir);
                _logService.Info("Database", $"创建数据库目录: {dbDir}");
            }

            CreateTables();
        }

        private static void CreateTables()
        {
            using (var connection = new SqliteConnection(ConfigManager.Instance.DatabaseConnectionString))
            {
                connection.Open();

                string createStationTable = @"
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

                string createTrainTable = @"
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

                string createLogTable = @"
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

                string createTagTable = @"
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

                string createRideTagTable = @"
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

                _logService.Info("Database", "数据库表创建完成");
            }
        }

        /// <summary>
        /// 数据库迁移：为现有表添加新列
        /// </summary>
        private static void MigrateDatabase(SqliteConnection connection)
        {
            try
            {
                // 检查并添加 status 列到 train_ride_info 表
                string checkStatusColumn = @"
                    SELECT COUNT(*) FROM pragma_table_info('train_ride_info') WHERE name = 'status';
                ";
                using (var command = new SqliteCommand(checkStatusColumn, connection))
                {
                    int count = Convert.ToInt32(command.ExecuteScalar());
                    if (count == 0)
                    {
                        string addStatusColumn = @"
                            ALTER TABLE train_ride_info ADD COLUMN status INTEGER DEFAULT 0;
                        ";
                        using (var alterCommand = new SqliteCommand(addStatusColumn, connection))
                        {
                            alterCommand.ExecuteNonQuery();
                        }
                        _logService.Info("Database", "已添加 status 列到 train_ride_info 表");
                    }
                }

                // 检查并添加 idx_status 索引
                string checkStatusIndex = @"
                    SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = 'idx_status';
                ";
                using (var command = new SqliteCommand(checkStatusIndex, connection))
                {
                    int count = Convert.ToInt32(command.ExecuteScalar());
                    if (count == 0)
                    {
                        string createStatusIndex = @"
                            CREATE INDEX IF NOT EXISTS idx_status ON train_ride_info (status);
                        ";
                        using (var indexCommand = new SqliteCommand(createStatusIndex, connection))
                        {
                            indexCommand.ExecuteNonQuery();
                        }
                        _logService.Info("Database", "已创建 idx_status 索引");
                    }
                }
            }
            catch (Exception ex)
            {
                _logService.Error("Database", $"数据库迁移失败: {ex.Message}");
                throw;
            }
        }

        public static SqliteConnection GetConnection()
        {
            return new SqliteConnection(ConfigManager.Instance.DatabaseConnectionString);
        }
    }
}