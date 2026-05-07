using GuiPiao.Model;
using GuiPiao.Utils;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GuiPiao.DataAccess
{
    public class LogRepository
    {
        private readonly string _connectionString;

        public LogRepository()
        {
            _connectionString = ConfigManager.Instance.DatabaseConnectionString;
        }

        public async Task<int> AddLogAsync(LogItem log)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = @"
                    INSERT INTO system_log (time, level, module, content, created_at)
                    VALUES (@time, @level, @module, @content, @createdAt);
                    SELECT last_insert_rowid();
                ";

                using (var command = new SqliteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@time", log.Time);
                    command.Parameters.AddWithValue("@level", (int)log.Level);
                    command.Parameters.AddWithValue("@module", log.Module ?? "");
                    command.Parameters.AddWithValue("@content", log.Content);
                    command.Parameters.AddWithValue("@createdAt", log.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));

                    var result = await command.ExecuteScalarAsync();
                    return Convert.ToInt32(result);
                }
            }
        }

        public async Task<IEnumerable<LogItem>> GetLogsAsync(
            LogLevel? level = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string keyword = null,
            int limit = 1000)
        {
            var logs = new List<LogItem>();
            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();

                string sql = "SELECT id, time, level, module, content, created_at FROM system_log WHERE 1=1";

                if (level.HasValue && level.Value != LogLevel.ALL)
                {
                    sql += " AND level = @level";
                }

                if (startDate.HasValue)
                {
                    sql += " AND created_at >= @startDate";
                }

                if (endDate.HasValue)
                {
                    sql += " AND created_at <= @endDate";
                }

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    sql += " AND (content LIKE @keyword OR module LIKE @keyword)";
                }

                sql += " ORDER BY created_at DESC LIMIT @limit";

                using (var command = new SqliteCommand(sql, connection))
                {
                    if (level.HasValue && level.Value != LogLevel.ALL)
                    {
                        command.Parameters.AddWithValue("@level", (int)level.Value);
                    }

                    if (startDate.HasValue)
                    {
                        command.Parameters.AddWithValue("@startDate", startDate.Value.ToString("yyyy-MM-dd 00:00:00"));
                    }

                    if (endDate.HasValue)
                    {
                        command.Parameters.AddWithValue("@endDate", endDate.Value.ToString("yyyy-MM-dd 23:59:59"));
                    }

                    if (!string.IsNullOrWhiteSpace(keyword))
                    {
                        command.Parameters.AddWithValue("@keyword", $"%{keyword}%");
                    }

                    command.Parameters.AddWithValue("@limit", limit);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            logs.Add(new LogItem
                            {
                                Id = reader.GetInt32(0),
                                Time = reader.GetString(1),
                                Level = (LogLevel)reader.GetInt32(2),
                                Module = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                Content = reader.GetString(4),
                                CreatedAt = DateTime.Parse(reader.GetString(5))
                            });
                        }
                    }
                }
            }
            return logs;
        }

        public async Task<int> GetLogCountAsync(
            LogLevel? level = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string keyword = null)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();

                string sql = "SELECT COUNT(*) FROM system_log WHERE 1=1";

                if (level.HasValue && level.Value != LogLevel.ALL)
                {
                    sql += " AND level = @level";
                }

                if (startDate.HasValue)
                {
                    sql += " AND created_at >= @startDate";
                }

                if (endDate.HasValue)
                {
                    sql += " AND created_at <= @endDate";
                }

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    sql += " AND (content LIKE @keyword OR module LIKE @keyword)";
                }

                using (var command = new SqliteCommand(sql, connection))
                {
                    if (level.HasValue && level.Value != LogLevel.ALL)
                    {
                        command.Parameters.AddWithValue("@level", (int)level.Value);
                    }

                    if (startDate.HasValue)
                    {
                        command.Parameters.AddWithValue("@startDate", startDate.Value.ToString("yyyy-MM-dd 00:00:00"));
                    }

                    if (endDate.HasValue)
                    {
                        command.Parameters.AddWithValue("@endDate", endDate.Value.ToString("yyyy-MM-dd 23:59:59"));
                    }

                    if (!string.IsNullOrWhiteSpace(keyword))
                    {
                        command.Parameters.AddWithValue("@keyword", $"%{keyword}%");
                    }

                    var result = await command.ExecuteScalarAsync();
                    return Convert.ToInt32(result);
                }
            }
        }

        public async Task<int> DeleteLogsOlderThanAsync(int days)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();
                string cutoffDate = DateTime.Now.AddDays(-days).ToString("yyyy-MM-dd HH:mm:ss");
                string sql = "DELETE FROM system_log WHERE created_at < @cutoffDate";

                using (var command = new SqliteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@cutoffDate", cutoffDate);
                    return await command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<int> DeleteAllLogsAsync()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "DELETE FROM system_log";

                using (var command = new SqliteCommand(sql, connection))
                {
                    return await command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<int> DeleteLogsByIdsAsync(IEnumerable<int> ids)
        {
            if (ids == null || !ids.Any())
                return 0;

            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();
                string idList = string.Join(",", ids);
                string sql = $"DELETE FROM system_log WHERE id IN ({idList})";

                using (var command = new SqliteCommand(sql, connection))
                {
                    return await command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<int> KeepRecentLogsAsync(int count)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = @"
                    DELETE FROM system_log 
                    WHERE id NOT IN (
                        SELECT id FROM system_log 
                        ORDER BY created_at DESC 
                        LIMIT @count
                    )";

                using (var command = new SqliteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@count", count);
                    return await command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task ExportLogsToCsvAsync(string filePath, IEnumerable<int> ids = null)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();

                string sql;
                if (ids != null && ids.Any())
                {
                    string idList = string.Join(",", ids);
                    sql = $"SELECT id, time, level, module, content, created_at FROM system_log WHERE id IN ({idList}) ORDER BY created_at DESC";
                }
                else
                {
                    sql = "SELECT id, time, level, module, content, created_at FROM system_log ORDER BY created_at DESC";
                }

                var logs = new List<LogItem>();
                using (var command = new SqliteCommand(sql, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        logs.Add(new LogItem
                        {
                            Id = reader.GetInt32(0),
                            Time = reader.GetString(1),
                            Level = (LogLevel)reader.GetInt32(2),
                            Module = reader.IsDBNull(3) ? "" : reader.GetString(3),
                            Content = reader.GetString(4),
                            CreatedAt = DateTime.Parse(reader.GetString(5))
                        });
                    }
                }

                using (var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8))
                {
                    await writer.WriteLineAsync("序号,时间,级别,模块,日志内容");
                    foreach (var log in logs)
                    {
                        await writer.WriteLineAsync($"{log.Id},{log.Time},{log.LevelDisplay},{log.Module},{log.Content}");
                    }
                }
            }
        }
    }
}
