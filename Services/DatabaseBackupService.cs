using GuiPiao.Model;
using GuiPiao.Utils;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace GuiPiao.Services
{
    /// <summary>
    /// 数据库备份服务
    /// </summary>
    public class DatabaseBackupService
    {
        private readonly LogService _logService;
        private const string ConfigFileName = "databasesettings.json";

        public DatabaseBackupService()
        {
            _logService = new LogService();
        }

        /// <summary>
        /// 自动备份数据库
        /// </summary>
        public string AutoBackup()
        {
            try
            {
                string dbPath = ConfigManager.Instance.DatabaseConnectionString
                    .Replace("Data Source=", "")
                    .Replace(";", "");

                if (!File.Exists(dbPath))
                {
                    _logService.Error("DatabaseBackupService", "数据库文件不存在");
                    throw new FileNotFoundException("数据库文件不存在", dbPath);
                }

                // 加载配置获取备份路径和最大保留数量
                var config = LoadConfig();
                string backupDir = config.BackupPath;

                if (string.IsNullOrEmpty(backupDir))
                {
                    backupDir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "GuiPiao",
                        "Backups"
                    );
                }

                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupFileName = $"database_backup_{timestamp}.db";
                string backupPath = Path.Combine(backupDir, backupFileName);

                File.Copy(dbPath, backupPath, true);
                _logService.Info("DatabaseBackupService", $"数据库备份成功: {backupPath}");

                // 根据配置进行压缩
                if (config.AutoCompress)
                {
                    string compressedPath = CompressBackupFile(backupPath);
                    if (!string.IsNullOrEmpty(compressedPath))
                    {
                        backupPath = compressedPath;
                    }
                }

                // 根据配置清理旧备份
                CleanupOldBackups(backupDir, config.MaxBackupCount);

                return backupPath;
            }
            catch (Exception ex)
            {
                _logService.Error("DatabaseBackupService", $"自动备份失败: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 压缩备份文件
        /// </summary>
        private string CompressBackupFile(string backupPath)
        {
            try
            {
                string zipPath = backupPath + ".zip";
                string fileName = Path.GetFileName(backupPath);

                using (var zipArchive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                {
                    zipArchive.CreateEntryFromFile(backupPath, fileName, CompressionLevel.Optimal);
                }

                // 删除原始未压缩文件
                File.Delete(backupPath);

                _logService.Info("DatabaseBackupService", $"备份文件已压缩: {zipPath}");

                return zipPath;
            }
            catch (Exception ex)
            {
                _logService.Error("DatabaseBackupService", $"压缩备份文件失败: {ex.Message}");
                return backupPath; // 压缩失败返回原路径
            }
        }

        /// <summary>
        /// 手动备份到指定路径
        /// </summary>
        public string BackupToPath(string targetPath)
        {
            try
            {
                string dbPath = ConfigManager.Instance.DatabaseConnectionString
                    .Replace("Data Source=", "")
                    .Replace(";", "");

                if (!File.Exists(dbPath))
                {
                    _logService.Error("DatabaseBackupService", "数据库文件不存在");
                    throw new FileNotFoundException("数据库文件不存在", dbPath);
                }

                string? targetDir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                File.Copy(dbPath, targetPath, true);
                _logService.Info("DatabaseBackupService", $"数据库手动备份成功: {targetPath}");

                return targetPath;
            }
            catch (Exception ex)
            {
                _logService.Error("DatabaseBackupService", $"手动备份失败: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 清理旧备份文件（按数量限制）
        /// </summary>
        private void CleanupOldBackups(string backupDir, int maxKeepCount)
        {
            try
            {
                if (maxKeepCount <= 0)
                {
                    maxKeepCount = 10; // 默认保留10个
                }

                // 获取所有备份文件（包括.db和.zip）
                var dbFiles = Directory.GetFiles(backupDir, "database_backup_*.db")
                    .Select(f => new FileInfo(f));
                var zipFiles = Directory.GetFiles(backupDir, "database_backup_*.zip")
                    .Select(f => new FileInfo(f));

                var allBackupFiles = dbFiles.Concat(zipFiles)
                    .OrderByDescending(f => f.CreationTime)
                    .ToList();

                if (allBackupFiles.Count > maxKeepCount)
                {
                    var filesToDelete = allBackupFiles.Skip(maxKeepCount).ToList();
                    int deletedCount = 0;

                    foreach (var file in filesToDelete)
                    {
                        try
                        {
                            File.Delete(file.FullName);
                            deletedCount++;
                        }
                        catch (Exception ex)
                        {
                            _logService.Error("DatabaseBackupService", $"删除旧备份失败 {file.FullName}: {ex.Message}");
                        }
                    }

                    if (deletedCount > 0)
                    {
                        _logService.Info("DatabaseBackupService", $"清理旧备份文件: {deletedCount}个，保留最新{maxKeepCount}个");
                    }
                }
            }
            catch (Exception ex)
            {
                _logService.Error("DatabaseBackupService", $"清理旧备份失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 清理旧备份文件（按天数）
        /// </summary>
        public void CleanupOldBackupsByDays(string backupDir, int keepDays)
        {
            try
            {
                if (!Directory.Exists(backupDir))
                {
                    return;
                }

                var cutoffDate = DateTime.Now.AddDays(-keepDays);
                var backupFiles = Directory.GetFiles(backupDir, "database_backup_*.db");
                int deletedCount = 0;

                foreach (var file in backupFiles)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTime < cutoffDate)
                    {
                        try
                        {
                            File.Delete(file);
                            deletedCount++;
                        }
                        catch (Exception ex)
                        {
                            _logService.Error("DatabaseBackupService", $"删除过期备份失败 {file}: {ex.Message}");
                        }
                    }
                }

                if (deletedCount > 0)
                {
                    _logService.Info("DatabaseBackupService", $"清理过期备份文件: {deletedCount}个");
                }
            }
            catch (Exception ex)
            {
                _logService.Error("DatabaseBackupService", $"清理旧备份失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载配置
        /// </summary>
        private DatabaseConfig LoadConfig()
        {
            return JsonConfigManager.Instance.LoadConfig<DatabaseConfig>(ConfigFileName, new DatabaseConfig());
        }
    }
}
