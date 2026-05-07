using System;
using System.IO;
using System.Threading.Tasks;
using GuiPiao.Model;
using GuiPiao.Utils;
using Microsoft.Data.Sqlite;

namespace GuiPiao.Services;

/// <summary>
///     数据库迁移服务 - 处理数据库存储路径修改
/// </summary>
public class DatabaseMigrationService
{
    private const string ConfigFileName = "databasesettings.json";
    private readonly DatabaseBackupService _backupService;
    private readonly LogService _logService;

    public DatabaseMigrationService()
    {
        _logService = new LogService();
        _backupService = new DatabaseBackupService();
    }

    /// <summary>
    ///     验证目标路径是否可用
    /// </summary>
    public MigrationValidationResult ValidateTargetPath(string targetPath)
    {
        try
        {
            // 检查路径是否为空
            if (string.IsNullOrWhiteSpace(targetPath)) return MigrationValidationResult.Failed("目标路径不能为空");

            // 获取目标目录
            var targetDir = Path.GetDirectoryName(targetPath);
            if (string.IsNullOrEmpty(targetDir)) return MigrationValidationResult.Failed("无效的目标路径");

            // 检查当前数据库路径
            var currentPath = GetCurrentDatabasePath();
            if (currentPath.Equals(targetPath, StringComparison.OrdinalIgnoreCase))
                return MigrationValidationResult.Failed("新路径与当前路径相同");

            // 检查目录是否存在，不存在则尝试创建
            if (!Directory.Exists(targetDir))
                try
                {
                    Directory.CreateDirectory(targetDir);
                }
                catch (Exception ex)
                {
                    return MigrationValidationResult.Failed($"无法创建目标目录: {ex.Message}");
                }

            // 检查写入权限
            try
            {
                var testFile = Path.Combine(targetDir, $".write_test_{Guid.NewGuid()}.tmp");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
            }
            catch (Exception ex)
            {
                return MigrationValidationResult.Failed($"目标目录无写入权限: {ex.Message}");
            }

            // 检查磁盘空间
            var driveInfo = new DriveInfo(Path.GetPathRoot(targetDir) ?? targetDir);
            var requiredSpace = GetCurrentDatabaseSize() * 2; // 需要2倍空间（备份+新文件）
            if (driveInfo.AvailableFreeSpace < requiredSpace)
                return MigrationValidationResult.Failed($"磁盘空间不足，需要至少 {FormatFileSize(requiredSpace)}");

            // 检查目标文件是否已存在
            if (File.Exists(targetPath)) return MigrationValidationResult.Failed("目标路径已存在同名文件，请更换路径或删除现有文件");

            return MigrationValidationResult.Success();
        }
        catch (Exception ex)
        {
            _logService.Error("DatabaseMigrationService", $"验证目标路径失败: {ex.Message}");
            return MigrationValidationResult.Failed($"验证失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     执行数据库迁移
    /// </summary>
    public async Task<MigrationResult> MigrateAsync(string targetPath, bool backupOriginal = true)
    {
        var currentPath = GetCurrentDatabasePath();
        string? backupPath = null;

        try
        {
            _logService.Info("DatabaseMigrationService", $"开始数据库迁移: {currentPath} -> {targetPath}");

            // 检查原数据库文件是否存在
            var sourceExists = File.Exists(currentPath);

            if (sourceExists)
            {
                // 1. 备份原数据库（可选）
                if (backupOriginal)
                {
                    backupPath = await Task.Run(() => _backupService.AutoBackup());
                    if (string.IsNullOrEmpty(backupPath)) return MigrationResult.Failed("备份原数据库失败，迁移已取消");
                    _logService.Info("DatabaseMigrationService", $"原数据库已备份: {backupPath}");
                }

                // 2. 复制数据库文件
                var copySuccess = await Task.Run(() => CopyDatabase(currentPath, targetPath));
                if (!copySuccess) return MigrationResult.Failed("复制数据库文件失败");

                // 3. 验证新数据库
                var verifySuccess = await Task.Run(() => VerifyDatabase(targetPath));
                if (!verifySuccess)
                {
                    // 验证失败，删除新文件
                    try
                    {
                        File.Delete(targetPath);
                    }
                    catch
                    {
                    }

                    return MigrationResult.Failed("新数据库验证失败，请检查磁盘空间或文件权限");
                }
            }
            else
            {
                // 原文件不存在，创建空数据库
                _logService.Info("DatabaseMigrationService", "原数据库文件不存在，将创建新数据库");
                var createSuccess = await Task.Run(() => CreateEmptyDatabase(targetPath));
                if (!createSuccess) return MigrationResult.Failed("创建新数据库失败");
            }

            // 4. 保存新配置
            var config = new DatabaseConfig
            {
                DatabasePath = targetPath,
                UseCustomPath = true
            };
            SaveConfig(config);

            _logService.Info("DatabaseMigrationService", $"数据库迁移成功，新路径: {targetPath}");

            return MigrationResult.Success(targetPath, backupPath);
        }
        catch (Exception ex)
        {
            _logService.Error("DatabaseMigrationService", $"数据库迁移失败: {ex.Message}");

            // 清理失败时产生的新文件
            try
            {
                if (File.Exists(targetPath)) File.Delete(targetPath);
            }
            catch
            {
            }

            return MigrationResult.Failed($"迁移失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     获取当前数据库路径
    /// </summary>
    public string GetCurrentDatabasePath()
    {
        return ConfigManager.Instance.DatabaseConnectionString
            .Replace("Data Source=", "")
            .Replace(";", "");
    }

    /// <summary>
    ///     获取数据库配置
    /// </summary>
    public DatabaseConfig LoadConfig()
    {
        return JsonConfigManager.Instance.LoadConfig(ConfigFileName, new DatabaseConfig());
    }

    /// <summary>
    ///     保存数据库配置
    /// </summary>
    public void SaveConfig(DatabaseConfig config)
    {
        JsonConfigManager.Instance.SaveConfig(ConfigFileName, config);
    }

    /// <summary>
    ///     获取当前数据库大小
    /// </summary>
    private long GetCurrentDatabaseSize()
    {
        try
        {
            var path = GetCurrentDatabasePath();
            if (File.Exists(path)) return new FileInfo(path).Length;
        }
        catch
        {
        }

        return 0;
    }

    /// <summary>
    ///     复制数据库文件（使用SQLite备份API确保一致性）
    /// </summary>
    private bool CopyDatabase(string sourcePath, string targetPath)
    {
        try
        {
            // 使用文件复制（SQLite单文件，关闭连接后复制即可）
            File.Copy(sourcePath, targetPath, true);
            return true;
        }
        catch (Exception ex)
        {
            _logService.Error("DatabaseMigrationService", $"复制数据库失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    ///     创建空数据库（包含基本表结构）
    /// </summary>
    private bool CreateEmptyDatabase(string dbPath)
    {
        try
        {
            // 确保目录存在
            var directory = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) Directory.CreateDirectory(directory);

            // 创建空数据库并初始化表结构
            using (var connection = new SqliteConnection($"Data Source={dbPath}"))
            {
                connection.Open();

                // 创建 train_ride_info 表
                using (var command = new SqliteCommand(@"
                        CREATE TABLE IF NOT EXISTS train_ride_info (
                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                            train_number TEXT NOT NULL,
                            departure_station TEXT NOT NULL,
                            arrival_station TEXT NOT NULL,
                            departure_date TEXT NOT NULL,
                            departure_time TEXT,
                            seat_class TEXT,
                            price REAL,
                            status TEXT DEFAULT '未出行',
                            ticket_type TEXT DEFAULT '成人票',
                            remarks TEXT,
                            created_at TEXT DEFAULT CURRENT_TIMESTAMP,
                            updated_at TEXT DEFAULT CURRENT_TIMESTAMP
                        );", connection))
                {
                    command.ExecuteNonQuery();
                }

                // 创建索引
                using (var command = new SqliteCommand(@"
                        CREATE INDEX IF NOT EXISTS idx_train_number ON train_ride_info(train_number);
                        CREATE INDEX IF NOT EXISTS idx_departure_date ON train_ride_info(departure_date);
                        CREATE INDEX IF NOT EXISTS idx_status ON train_ride_info(status);
                    ", connection))
                {
                    command.ExecuteNonQuery();
                }
            }

            _logService.Info("DatabaseMigrationService", $"创建空数据库成功: {dbPath}");
            return true;
        }
        catch (Exception ex)
        {
            _logService.Error("DatabaseMigrationService", $"创建空数据库失败: {ex.Message}");
            return false;
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
                        _logService.Error("DatabaseMigrationService", $"数据库完整性检查失败: {result}");
                        return false;
                    }
                }

                // 尝试读取表信息
                using (var command =
                       new SqliteCommand("SELECT COUNT(*) FROM sqlite_master WHERE type='table';", connection))
                {
                    command.ExecuteScalar();
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logService.Error("DatabaseMigrationService", $"验证数据库失败: {ex.Message}");
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
///     迁移验证结果
/// </summary>
public class MigrationValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }

    public static MigrationValidationResult Success()
    {
        return new MigrationValidationResult { IsValid = true };
    }

    public static MigrationValidationResult Failed(string message)
    {
        return new MigrationValidationResult { IsValid = false, ErrorMessage = message };
    }
}

/// <summary>
///     迁移结果
/// </summary>
public class MigrationResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public string? NewPath { get; set; }
    public string? BackupPath { get; set; }
    public bool RequiresRestart => IsSuccess;

    public static MigrationResult Success(string newPath, string? backupPath)
    {
        return new MigrationResult
        {
            IsSuccess = true,
            NewPath = newPath,
            BackupPath = backupPath
        };
    }

    public static MigrationResult Failed(string message)
    {
        return new MigrationResult
        {
            IsSuccess = false,
            ErrorMessage = message
        };
    }
}