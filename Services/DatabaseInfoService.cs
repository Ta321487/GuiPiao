using Dapper;
using GuiPiao.Utils;
using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GuiPiao.Services
{
    /// <summary>
    /// 数据库信息服务 - 提供数据库基础信息查询
    /// </summary>
    public class DatabaseInfoService
    {
        private readonly string _connectionString = ConfigManager.Instance.DatabaseConnectionString;
        private readonly LogService _logService;

        public DatabaseInfoService()
        {
            _logService = new LogService();
        }

        /// <summary>
        /// 获取数据库文件路径
        /// </summary>
        public string GetDatabasePath()
        {
            return _connectionString
                .Replace("Data Source=", "")
                .Replace(";", "");
        }

        /// <summary>
        /// 获取数据库文件信息
        /// </summary>
        public DatabaseFileInfo GetDatabaseFileInfo()
        {
            try
            {
                string dbPath = GetDatabasePath();
                var fileInfo = new FileInfo(dbPath);

                if (!fileInfo.Exists)
                {
                    return new DatabaseFileInfo
                    {
                        Path = dbPath,
                        Size = "文件不存在",
                        CreateTime = "-",
                        LastBackupTime = GetLastBackupTime()
                    };
                }

                return new DatabaseFileInfo
                {
                    Path = dbPath,
                    Size = FormatFileSize(fileInfo.Length),
                    CreateTime = fileInfo.CreationTime.ToString("yyyy-MM-dd HH:mm"),
                    LastBackupTime = GetLastBackupTime()
                };
            }
            catch (Exception ex)
            {
                _logService.Error("DatabaseInfoService", $"获取数据库文件信息失败: {ex.Message}");
                return new DatabaseFileInfo
                {
                    Path = GetDatabasePath(),
                    Size = "获取失败",
                    CreateTime = "-",
                    LastBackupTime = "-"
                };
            }
        }

        /// <summary>
        /// 获取票务记录总数
        /// </summary>
        public async Task<int> GetTicketCountAsync()
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    string sql = "SELECT COUNT(*) FROM train_ride_info";
                    return await connection.QuerySingleAsync<int>(sql);
                }
            }
            catch (Exception ex)
            {
                _logService.Error("DatabaseInfoService", $"获取票务记录数失败: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 获取最近备份时间
        /// </summary>
        private string GetLastBackupTime()
        {
            try
            {
                string backupDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "GuiPiao",
                    "Backups"
                );

                if (!Directory.Exists(backupDir))
                {
                    return "无备份记录";
                }

                var dbFiles = Directory.GetFiles(backupDir, "database_backup_*.db");
                var zipFiles = Directory.GetFiles(backupDir, "database_backup_*.zip");
                var backupFiles = dbFiles.Concat(zipFiles).ToArray();
                if (backupFiles.Length == 0)
                {
                    return "无备份记录";
                }

                DateTime latestBackup = DateTime.MinValue;
                foreach (var file in backupFiles)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTime > latestBackup)
                    {
                        latestBackup = fileInfo.CreationTime;
                    }
                }

                return latestBackup.ToString("yyyy-MM-dd HH:mm");
            }
            catch (Exception ex)
            {
                _logService.Error("DatabaseInfoService", $"获取最近备份时间失败: {ex.Message}");
                return "获取失败";
            }
        }

        /// <summary>
        /// 格式化文件大小
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

        /// <summary>
        /// 打开数据库所在目录
        /// </summary>
        /// <returns>是否成功打开，文件不存在时返回false</returns>
        public bool OpenDatabaseDirectory()
        {
            try
            {
                string dbPath = GetDatabasePath();
                string? directory = Path.GetDirectoryName(dbPath);

                // 检查数据库文件是否存在
                if (!File.Exists(dbPath))
                {
                    _logService.Error("DatabaseInfoService", $"数据库文件不存在，无法打开目录: {dbPath}");
                    return false;
                }

                if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = directory,
                        UseShellExecute = true,
                        Verb = "open"
                    });
                    _logService.Info("DatabaseInfoService", $"打开数据库目录: {directory}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logService.Error("DatabaseInfoService", $"打开数据库目录失败: {ex.Message}");
                throw;
            }
        }
    }

    /// <summary>
    /// 数据库文件信息
    /// </summary>
    public class DatabaseFileInfo
    {
        public string Path { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string CreateTime { get; set; } = string.Empty;
        public string LastBackupTime { get; set; } = string.Empty;
    }
}
