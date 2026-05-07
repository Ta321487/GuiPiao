using GuiPiao.Utils;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GuiPiao.Services
{
    /// <summary>
    /// 数据库验证服务 - 验证数据库表结构是否符合本项目要求
    /// </summary>
    public class DatabaseValidationService
    {
        private readonly LogService _logService;

        // 必需的表及其列定义
        private readonly Dictionary<string, List<string>> _requiredTables = new()
        {
            ["station_info"] = new List<string>
            {
                "id", "station_name", "province", "city", "district",
                "station_code", "station_pinyin", "station_level", "railway_bureau",
                "longitude", "latitude"
            },
            ["train_ride_info"] = new List<string>
            {
                "id", "ticket_number", "check_in_location", "depart_station", "train_no",
                "arrive_station", "depart_station_pinyin", "arrive_station_pinyin", "depart_date",
                "depart_time", "coach_no", "seat_no", "money", "seat_type", "additional_info",
                "ticket_purpose", "ticket_modification_type", "ticket_type_flags", "payment_channel_flags",
                "hint", "depart_station_code", "arrive_station_code", "status"
            },
            ["system_log"] = new List<string>
            {
                "id", "time", "level", "module", "content", "created_at"
            },
            ["ticket_tag"] = new List<string>
            {
                "id", "name", "color", "text_color", "sort_order", "created_at"
            },
            ["train_ride_tag"] = new List<string>
            {
                "id", "train_ride_id", "tag_id", "created_at"
            }
        };

        public DatabaseValidationService()
        {
            _logService = new LogService();
        }

        /// <summary>
        /// 验证数据库是否符合本项目要求
        /// </summary>
        /// <param name="dbPath">数据库文件路径</param>
        /// <returns>验证结果</returns>
        public DatabaseValidationResult ValidateDatabase(string dbPath)
        {
            try
            {
                // 检查文件是否存在
                if (!System.IO.File.Exists(dbPath))
                {
                    return DatabaseValidationResult.Failed("数据库文件不存在");
                }

                // 检查文件扩展名
                string extension = System.IO.Path.GetExtension(dbPath).ToLower();
                if (extension != ".db" && extension != ".sqlite" && extension != ".sqlite3")
                {
                    return DatabaseValidationResult.Failed("文件格式不正确，请选择 .db、.sqlite 或 .sqlite3 格式的数据库文件");
                }

                // 尝试连接数据库
                string connectionString = $"Data Source={dbPath}";
                using (var connection = new SqliteConnection(connectionString))
                {
                    connection.Open();

                    // 1. 检查数据库完整性
                    var integrityResult = CheckIntegrity(connection);
                    if (!integrityResult.IsValid)
                    {
                        return integrityResult;
                    }

                    // 2. 检查必需的表是否存在
                    var tablesResult = CheckRequiredTables(connection);
                    if (!tablesResult.IsValid)
                    {
                        return tablesResult;
                    }

                    // 3. 检查必需的列是否存在
                    var columnsResult = CheckRequiredColumns(connection);
                    if (!columnsResult.IsValid)
                    {
                        return columnsResult;
                    }

                    // 4. 检查外键约束
                    var foreignKeyResult = CheckForeignKeys(connection);
                    if (!foreignKeyResult.IsValid)
                    {
                        return foreignKeyResult;
                    }
                }

                _logService.Info("DatabaseValidationService", $"数据库验证通过: {dbPath}");
                return DatabaseValidationResult.Success();
            }
            catch (SqliteException ex)
            {
                _logService.Error("DatabaseValidationService", $"SQLite错误: {ex.Message}");
                return DatabaseValidationResult.Failed($"数据库文件损坏或格式不正确: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logService.Error("DatabaseValidationService", $"验证数据库失败: {ex.Message}");
                return DatabaseValidationResult.Failed($"验证失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查数据库完整性
        /// </summary>
        private DatabaseValidationResult CheckIntegrity(SqliteConnection connection)
        {
            try
            {
                using (var command = new SqliteCommand("PRAGMA integrity_check;", connection))
                {
                    var result = command.ExecuteScalar()?.ToString();
                    if (result?.ToLower() != "ok")
                    {
                        return DatabaseValidationResult.Failed($"数据库完整性检查失败: {result}");
                    }
                }
                return DatabaseValidationResult.Success();
            }
            catch (Exception ex)
            {
                return DatabaseValidationResult.Failed($"完整性检查异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查必需的表是否存在
        /// </summary>
        private DatabaseValidationResult CheckRequiredTables(SqliteConnection connection)
        {
            try
            {
                var missingTables = new List<string>();

                foreach (var tableName in _requiredTables.Keys)
                {
                    string sql = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = @tableName;";
                    using (var command = new SqliteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@tableName", tableName);
                        int count = Convert.ToInt32(command.ExecuteScalar());
                        if (count == 0)
                        {
                            missingTables.Add(tableName);
                        }
                    }
                }

                if (missingTables.Count > 0)
                {
                    string tables = string.Join(", ", missingTables);
                    return DatabaseValidationResult.Failed(
                        $"数据库缺少必需的表: {tables}\n\n" +
                        $"该数据库可能不是本项目的有效数据库文件，或者版本不兼容。");
                }

                return DatabaseValidationResult.Success();
            }
            catch (Exception ex)
            {
                return DatabaseValidationResult.Failed($"检查表结构异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查必需的列是否存在
        /// </summary>
        private DatabaseValidationResult CheckRequiredColumns(SqliteConnection connection)
        {
            try
            {
                var missingColumns = new List<string>();

                foreach (var table in _requiredTables)
                {
                    string tableName = table.Key;
                    var requiredColumns = table.Value;

                    // 获取表中实际存在的列
                    var existingColumns = new List<string>();
                    string sql = $"PRAGMA table_info({tableName});";
                    using (var command = new SqliteCommand(sql, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string columnName = reader["name"].ToString()?.ToLower() ?? "";
                                existingColumns.Add(columnName);
                            }
                        }
                    }

                    // 检查必需的列是否存在
                    foreach (var column in requiredColumns)
                    {
                        if (!existingColumns.Contains(column.ToLower()))
                        {
                            missingColumns.Add($"{tableName}.{column}");
                        }
                    }
                }

                if (missingColumns.Count > 0)
                {
                    string columns = string.Join(", ", missingColumns.Take(5));
                    if (missingColumns.Count > 5)
                    {
                        columns += $" 等共 {missingColumns.Count} 个列";
                    }
                    return DatabaseValidationResult.Failed(
                        $"数据库缺少必需的列: {columns}\n\n" +
                        $"该数据库版本可能过旧或与当前版本不兼容。");
                }

                return DatabaseValidationResult.Success();
            }
            catch (Exception ex)
            {
                return DatabaseValidationResult.Failed($"检查列结构异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查外键约束
        /// </summary>
        private DatabaseValidationResult CheckForeignKeys(SqliteConnection connection)
        {
            try
            {
                // 检查 train_ride_info 表的外键约束
                string sql = @"
                    SELECT sql FROM sqlite_master 
                    WHERE type = 'table' AND name = 'train_ride_info';";

                using (var command = new SqliteCommand(sql, connection))
                {
                    var createSql = command.ExecuteScalar()?.ToString() ?? "";

                    // 检查是否包含外键定义（简单检查）
                    if (!createSql.ToLower().Contains("foreign key"))
                    {
                        // 外键约束可能是在迁移后添加的，通过检查索引来确认
                        string indexSql = @"
                            SELECT COUNT(*) FROM sqlite_master 
                            WHERE type = 'index' AND name IN ('idx_depart_station_code', 'idx_arrive_station_code');";

                        using (var indexCommand = new SqliteCommand(indexSql, connection))
                        {
                            int indexCount = Convert.ToInt32(indexCommand.ExecuteScalar());
                            if (indexCount < 2)
                            {
                                // 缺少必要的索引，可能是旧版本数据库
                                _logService.Warn("DatabaseValidationService", "数据库缺少部分索引，可能是旧版本");
                            }
                        }
                    }
                }

                return DatabaseValidationResult.Success();
            }
            catch (Exception ex)
            {
                _logService.Warn("DatabaseValidationService", $"检查外键约束异常: {ex.Message}");
                // 外键检查失败不阻止验证通过，只是警告
                return DatabaseValidationResult.Success();
            }
        }

        /// <summary>
        /// 获取数据库基本信息
        /// </summary>
        public DatabaseBasicInfo GetDatabaseBasicInfo(string dbPath)
        {
            try
            {
                string connectionString = $"Data Source={dbPath}";
                using (var connection = new SqliteConnection(connectionString))
                {
                    connection.Open();

                    // 获取表数量
                    int tableCount = 0;
                    using (var command = new SqliteCommand(
                        "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table';", connection))
                    {
                        tableCount = Convert.ToInt32(command.ExecuteScalar());
                    }

                    // 获取票务记录数
                    int ticketCount = 0;
                    try
                    {
                        using (var command = new SqliteCommand(
                            "SELECT COUNT(*) FROM train_ride_info;", connection))
                        {
                            ticketCount = Convert.ToInt32(command.ExecuteScalar());
                        }
                    }
                    catch { }

                    // 获取车站数量
                    int stationCount = 0;
                    try
                    {
                        using (var command = new SqliteCommand(
                            "SELECT COUNT(*) FROM station_info;", connection))
                        {
                            stationCount = Convert.ToInt32(command.ExecuteScalar());
                        }
                    }
                    catch { }

                    return new DatabaseBasicInfo
                    {
                        TableCount = tableCount,
                        TicketCount = ticketCount,
                        StationCount = stationCount,
                        FileSize = new System.IO.FileInfo(dbPath).Length
                    };
                }
            }
            catch (Exception ex)
            {
                _logService.Error("DatabaseValidationService", $"获取数据库信息失败: {ex.Message}");
                return new DatabaseBasicInfo();
            }
        }
    }

    /// <summary>
    /// 数据库验证结果
    /// </summary>
    public class DatabaseValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }

        public static DatabaseValidationResult Success()
        {
            return new DatabaseValidationResult { IsValid = true };
        }

        public static DatabaseValidationResult Failed(string message)
        {
            return new DatabaseValidationResult { IsValid = false, ErrorMessage = message };
        }
    }

    /// <summary>
    /// 数据库基本信息
    /// </summary>
    public class DatabaseBasicInfo
    {
        public int TableCount { get; set; }
        public int TicketCount { get; set; }
        public int StationCount { get; set; }
        public long FileSize { get; set; }
    }
}
