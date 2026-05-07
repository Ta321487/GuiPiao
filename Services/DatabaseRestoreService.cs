using System;
using System.IO;
using GuiPiao.Utils;
using Microsoft.Data.Sqlite;

namespace GuiPiao.Services;

/// <summary>
///     数据库恢复服务
/// </summary>
public class DatabaseRestoreService
{
    private readonly DatabaseBackupService _backupService;
    private readonly LogService _logService;

    public DatabaseRestoreService()
    {
        _logService = new LogService();
        _backupService = new DatabaseBackupService();
    }

    /// <summary>
    ///     验证备份文件是否可用
    /// </summary>
    public RestoreValidationResult ValidateBackupFile(string backupPath)
    {
        try
        {
            // 检查文件是否存在
            if (!File.Exists(backupPath)) return RestoreValidationResult.Failed("备份文件不存在");

            // 检查文件扩展名
            if (!backupPath.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
                return RestoreValidationResult.Failed("请选择有效的数据库备份文件(.db)");

            // 检查文件大小
            var fileInfo = new FileInfo(backupPath);
            if (fileInfo.Length == 0) return RestoreValidationResult.Failed("备份文件为空");

            // 尝试打开验证数据库完整性
            try
            {
                using (var connection = new SqliteConnection($"Data Source={backupPath}"))
                {
                    connection.Open();

                    // 执行完整性检查
                    using (var command = new SqliteCommand("PRAGMA integrity_check;", connection))
                    {
                        var result = command.ExecuteScalar()?.ToString();
                        if (result != "ok") return RestoreValidationResult.Failed($"备份文件已损坏: {result}");
                    }

                    // 检查是否包含必要的表
                    using (var command = new SqliteCommand(
                               "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='train_ride_info';",
                               connection))
                    {
                        var count = Convert.ToInt32(command.ExecuteScalar());
                        if (count == 0) return RestoreValidationResult.Failed("备份文件不包含有效的票务数据表");
                    }
                }
            }
            catch (Exception ex)
            {
                return RestoreValidationResult.Failed($"无法打开备份文件: {ex.Message}");
            }

            return RestoreValidationResult.Success(fileInfo.Length);
        }
        catch (Exception ex)
        {
            _logService.Error("DatabaseRestoreService", $"验证备份文件失败: {ex.Message}");
            return RestoreValidationResult.Failed($"验证失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     从备份文件恢复数据库
    /// </summary>
    public RestoreResult RestoreFromBackup(string backupPath, bool backupCurrent = true)
    {
        var currentDbPath = ConfigManager.Instance.DatabaseConnectionString
            .Replace("Data Source=", "")
            .Replace(";", "");

        string? currentBackupPath = null;

        try
        {
            _logService.Info("DatabaseRestoreService", $"开始从备份恢复: {backupPath}");

            // 1. 备份当前数据库（可选）
            if (backupCurrent && File.Exists(currentDbPath))
            {
                currentBackupPath = _backupService.AutoBackup();
                if (string.IsNullOrEmpty(currentBackupPath))
                    _logService.Info("DatabaseRestoreService", "备份当前数据库失败，继续恢复操作");
                else
                    _logService.Info("DatabaseRestoreService", $"当前数据库已备份: {currentBackupPath}");
            }

            // 2. 复制备份文件到当前数据库位置
            File.Copy(backupPath, currentDbPath, true);

            // 3. 验证恢复后的数据库
            var verifySuccess = VerifyDatabase(currentDbPath);
            if (!verifySuccess) return RestoreResult.Failed("恢复后的数据库验证失败");

            _logService.Info("DatabaseRestoreService", "数据库恢复成功");

            return RestoreResult.Success(currentBackupPath);
        }
        catch (Exception ex)
        {
            _logService.Error("DatabaseRestoreService", $"恢复数据库失败: {ex.Message}");
            return RestoreResult.Failed($"恢复失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     验证数据库是否可用
    /// </summary>
    private bool VerifyDatabase(string dbPath)
    {
        try
        {
            using (var connection = new SqliteConnection($"Data Source={dbPath}"))
            {
                connection.Open();

                // 执行完整性检查
                using (var command = new SqliteCommand("PRAGMA integrity_check;", connection))
                {
                    var result = command.ExecuteScalar()?.ToString();
                    if (result != "ok")
                    {
                        _logService.Error("DatabaseRestoreService", $"数据库完整性检查失败: {result}");
                        return false;
                    }
                }

                // 尝试读取记录数
                using (var command = new SqliteCommand("SELECT COUNT(*) FROM train_ride_info;", connection))
                {
                    var count = Convert.ToInt32(command.ExecuteScalar());
                    _logService.Info("DatabaseRestoreService", $"恢复后的数据库包含 {count} 条记录");
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logService.Error("DatabaseRestoreService", $"验证数据库失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    ///     格式化文件大小
    /// </summary>
    private string FormatFileSize(long bytes)
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

/// <summary>
///     恢复验证结果
/// </summary>
public class RestoreValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public long FileSize { get; set; }
    public string FormattedFileSize => FormatFileSize(FileSize);

    public static RestoreValidationResult Success(long fileSize)
    {
        return new RestoreValidationResult { IsValid = true, FileSize = fileSize };
    }

    public static RestoreValidationResult Failed(string message)
    {
        return new RestoreValidationResult { IsValid = false, ErrorMessage = message };
    }

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

/// <summary>
///     恢复结果
/// </summary>
public class RestoreResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public string? CurrentBackupPath { get; set; }
    public bool HasCurrentBackup => !string.IsNullOrEmpty(CurrentBackupPath);

    public static RestoreResult Success(string? currentBackupPath)
    {
        return new RestoreResult
        {
            IsSuccess = true,
            CurrentBackupPath = currentBackupPath
        };
    }

    public static RestoreResult Failed(string message)
    {
        return new RestoreResult
        {
            IsSuccess = false,
            ErrorMessage = message
        };
    }
}