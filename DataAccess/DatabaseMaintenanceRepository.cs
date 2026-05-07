using System;
using System.IO;
using System.Threading.Tasks;
using Dapper;
using GuiPiao.Utils;
using Microsoft.Data.Sqlite;

namespace GuiPiao.DataAccess;

/// <summary>
///     数据库维护Repository - 负责数据库维护相关操作
/// </summary>
public class DatabaseMaintenanceRepository
{
    private readonly string _connectionString = ConfigManager.Instance.DatabaseConnectionString;

    /// <summary>
    ///     执行数据完整性校验
    /// </summary>
    public async Task<IntegrityCheckResult> CheckIntegrityAsync()
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();

            // 执行PRAGMA integrity_check
            var result = await connection.QueryFirstOrDefaultAsync<string>(
                "PRAGMA integrity_check;"
            );

            var isOk = result == "ok";
            var message = isOk ? "数据库完整性检查通过" : $"发现问题: {result}";

            return new IntegrityCheckResult
            {
                IsOk = isOk,
                Message = message,
                Details = result ?? "未知错误"
            };
        }
    }

    /// <summary>
    ///     获取数据库统计信息
    /// </summary>
    public async Task<DatabaseStats> GetDatabaseStatsAsync()
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();

            var stats = new DatabaseStats();

            // 获取表数量
            stats.TableCount = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM sqlite_master WHERE type='table';"
            );

            // 获取索引数量
            stats.IndexCount = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM sqlite_master WHERE type='index';"
            );

            // 获取train_ride_info表记录数
            stats.TicketCount = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM train_ride_info;"
            );

            // 获取数据库页大小
            stats.PageSize = await connection.ExecuteScalarAsync<int>(
                "PRAGMA page_size;"
            );

            // 获取数据库页数
            stats.PageCount = await connection.ExecuteScalarAsync<int>(
                "PRAGMA page_count;"
            );

            // 获取空闲页数
            stats.FreePages = await connection.ExecuteScalarAsync<int>(
                "PRAGMA freelist_count;"
            );

            // 计算碎片率
            if (stats.PageCount > 0) stats.FragmentationRatio = (double)stats.FreePages / stats.PageCount;

            return stats;
        }
    }

    /// <summary>
    ///     执行VACUUM（碎片整理）
    /// </summary>
    public async Task<VacuumResult> VacuumAsync()
    {
        var startTime = DateTime.Now;
        long originalSize = 0;
        long newSize = 0;

        try
        {
            // 获取原始文件大小
            originalSize = GetDatabaseFileSize();

            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();

                // 执行VACUUM
                await connection.ExecuteAsync("VACUUM;");
            }

            // 获取整理后文件大小
            newSize = GetDatabaseFileSize();

            var duration = DateTime.Now - startTime;

            return new VacuumResult
            {
                Success = true,
                OriginalSize = originalSize,
                NewSize = newSize,
                ReducedSize = originalSize - newSize,
                Duration = duration,
                Message = $"碎片整理完成，耗时 {duration.TotalSeconds:F1} 秒"
            };
        }
        catch (Exception ex)
        {
            return new VacuumResult
            {
                Success = false,
                Message = $"碎片整理失败: {ex.Message}"
            };
        }
    }

    /// <summary>
    ///     获取数据库文件大小
    /// </summary>
    private long GetDatabaseFileSize()
    {
        try
        {
            var dbPath = _connectionString
                .Replace("Data Source=", "")
                .Replace(";", "");

            if (File.Exists(dbPath)) return new FileInfo(dbPath).Length;
        }
        catch
        {
        }

        return 0;
    }
}

/// <summary>
///     完整性校验结果
/// </summary>
public class IntegrityCheckResult
{
    public bool IsOk { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}

/// <summary>
///     数据库统计信息
/// </summary>
public class DatabaseStats
{
    public int TableCount { get; set; }
    public int IndexCount { get; set; }
    public int TicketCount { get; set; }
    public int PageSize { get; set; }
    public int PageCount { get; set; }
    public int FreePages { get; set; }
    public double FragmentationRatio { get; set; }

    public string FormattedFragmentationRatio => $"{FragmentationRatio:P1}";
}

/// <summary>
///     VACUUM结果
/// </summary>
public class VacuumResult
{
    public bool Success { get; set; }
    public long OriginalSize { get; set; }
    public long NewSize { get; set; }
    public long ReducedSize { get; set; }
    public TimeSpan Duration { get; set; }
    public string Message { get; set; } = string.Empty;

    public string FormattedOriginalSize => FormatFileSize(OriginalSize);
    public string FormattedNewSize => FormatFileSize(NewSize);
    public string FormattedReducedSize => FormatFileSize(ReducedSize);

    private static string FormatFileSize(long bytes)
    {
        const long KB = 1024;
        const long MB = KB * 1024;
        const long GB = MB * 1024;

        if (bytes >= GB)
            return $"{bytes / (double)GB:F2} GB";
        if (bytes >= MB)
            return $"{bytes / (double)MB:F2} MB";
        if (bytes >= KB)
            return $"{bytes / (double)KB:F2} KB";
        return $"{bytes} B";
    }
}