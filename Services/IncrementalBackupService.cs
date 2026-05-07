using GuiPiao.DataAccess;
using GuiPiao.Model;
using GuiPiao.Utils;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GuiPiao.Services
{
    /// <summary>
    /// 增量备份服务
    /// </summary>
    public class IncrementalBackupService
    {
        private readonly LogService _logService;
        private readonly string _connectionString;
        private const string BackupRecordFileName = "incremental_backups.json";

        public IncrementalBackupService()
        {
            _logService = new LogService();
            _connectionString = ConfigManager.Instance.DatabaseConnectionString;
        }

        /// <summary>
        /// 执行增量备份
        /// </summary>
        /// <param name="lastBackupTime">上次备份时间（null表示从全量备份后开始）</param>
        /// <returns>备份信息</returns>
        public async Task<IncrementalBackupResult> PerformIncrementalBackupAsync(DateTime? lastBackupTime = null)
        {
            try
            {
                // 如果没有指定上次备份时间，尝试从记录中获取
                if (lastBackupTime == null)
                {
                    lastBackupTime = await GetLastBackupTimeAsync();
                }

                var startTime = lastBackupTime ?? DateTime.MinValue;
                var endTime = DateTime.Now;

                // 获取变更的数据
                var changedRecords = await GetChangedRecordsAsync(startTime, endTime);

                if (changedRecords.Count == 0)
                {
                    return new IncrementalBackupResult
                    {
                        Success = true,
                        Message = "没有需要备份的新数据",
                        RecordCount = 0
                    };
                }

                // 创建备份目录
                var config = LoadConfig();
                string backupDir = config.BackupPath;
                if (string.IsNullOrEmpty(backupDir))
                {
                    backupDir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "GuiPiao",
                        "Backups",
                        "Incremental"
                    );
                }

                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }

                // 生成备份文件名
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupFileName = $"incremental_backup_{timestamp}.db";
                string backupPath = Path.Combine(backupDir, backupFileName);

                // 创建增量备份数据库
                await CreateIncrementalBackupDatabaseAsync(backupPath, changedRecords);

                // 压缩备份文件
                bool isCompressed = false;
                if (config.AutoCompress)
                {
                    string compressedPath = CompressBackupFile(backupPath);
                    if (compressedPath != backupPath)
                    {
                        backupPath = compressedPath;
                        isCompressed = true;
                    }
                }

                // 获取文件大小
                var fileInfo = new FileInfo(backupPath);
                long fileSize = fileInfo.Length;

                // 创建备份记录
                var backupInfo = new IncrementalBackupInfo
                {
                    BackupTime = DateTime.Now,
                    BackupPath = backupPath,
                    DataStartTime = startTime,
                    DataEndTime = endTime,
                    RecordCount = changedRecords.Count,
                    FileSize = fileSize,
                    BackupType = "Incremental",
                    IsCompressed = isCompressed,
                    Remark = $"备份了 {changedRecords.Count} 条变更记录"
                };

                // 保存备份记录
                await SaveBackupRecordAsync(backupInfo);

                _logService.Info("IncrementalBackupService", $"增量备份成功: {backupPath}, 记录数: {changedRecords.Count}");

                return new IncrementalBackupResult
                {
                    Success = true,
                    Message = "增量备份成功",
                    BackupPath = backupPath,
                    RecordCount = changedRecords.Count,
                    FileSize = fileSize,
                    BackupInfo = backupInfo
                };
            }
            catch (Exception ex)
            {
                _logService.Error("IncrementalBackupService", $"增量备份失败: {ex.Message}");
                return new IncrementalBackupResult
                {
                    Success = false,
                    Message = $"增量备份失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 获取变更的记录
        /// </summary>
        private async Task<List<TrainRideInfo>> GetChangedRecordsAsync(DateTime startTime, DateTime endTime)
        {
            var records = new List<TrainRideInfo>();

            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();

                // 使用 SQLite 的 last_modified 或基于 ID 的变更检测
                // 这里我们使用 ID 作为变更标识（新记录ID更大）
                string sql = @"
                    SELECT 
                        id AS Id,
                        ticket_number AS TicketNumber,
                        check_in_location AS CheckInLocation,
                        depart_station AS DepartStation,
                        train_no AS TrainNo,
                        arrive_station AS ArriveStation,
                        depart_station_pinyin AS DepartStationPinyin,
                        arrive_station_pinyin AS ArriveStationPinyin,
                        depart_date AS DepartDate,
                        depart_time AS DepartTime,
                        coach_no AS CoachNo,
                        seat_no AS SeatNo,
                        money AS Money,
                        seat_type AS SeatType,
                        additional_info AS AdditionalInfo,
                        ticket_purpose AS TicketPurpose,
                        ticket_modification_type AS TicketModificationType,
                        ticket_type_flags AS TicketTypeFlags,
                        payment_channel_flags AS PaymentChannelFlags,
                        hint AS Hint,
                        depart_station_code AS DepartStationCode,
                        arrive_station_code AS ArriveStationCode,
                        status AS Status
                    FROM train_ride_info
                    WHERE id > (SELECT COALESCE(MAX(id), 0) FROM train_ride_info WHERE rowid <= (
                        SELECT COALESCE(MAX(rowid), 0) FROM train_ride_info 
                        WHERE CAST(id AS INTEGER) <= (SELECT COUNT(*) FROM train_ride_info)
                    ))
                    ORDER BY id
                ";

                // 简化方案：获取所有记录，实际应该根据时间戳或版本号
                // 这里使用行数对比的方式
                var command = new SqliteCommand(
                    "SELECT COUNT(*) FROM train_ride_info", connection);
                var totalCount = Convert.ToInt32(await command.ExecuteScalarAsync());

                // 获取上次备份时的记录数（从备份记录中）
                var lastRecordCount = await GetLastBackupRecordCountAsync();

                if (totalCount > lastRecordCount)
                {
                    // 有新记录，获取新增的记录
                    var newRecordsSql = $@"
                        SELECT * FROM train_ride_info 
                        ORDER BY id DESC 
                        LIMIT {totalCount - lastRecordCount}
                    ";

                    using (var cmd = new SqliteCommand(newRecordsSql, connection))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            records.Add(MapReaderToTrainRideInfo(reader));
                        }
                    }
                }
            }

            return records;
        }

        /// <summary>
        /// 创建增量备份数据库
        /// </summary>
        private async Task CreateIncrementalBackupDatabaseAsync(string backupPath, List<TrainRideInfo> records)
        {
            // 删除已存在的文件
            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
            }

            using (var connection = new SqliteConnection($"Data Source={backupPath}"))
            {
                await connection.OpenAsync();

                // 创建表结构
                string createTableSql = @"
                    CREATE TABLE IF NOT EXISTS train_ride_info (
                        id INTEGER NOT NULL PRIMARY KEY,
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
                        status INTEGER DEFAULT 0
                    );

                    CREATE TABLE IF NOT EXISTS backup_metadata (
                        key TEXT PRIMARY KEY,
                        value TEXT
                    );
                ";

                using (var cmd = new SqliteCommand(createTableSql, connection))
                {
                    await cmd.ExecuteNonQueryAsync();
                }

                // 插入备份元数据
                using (var cmd = new SqliteCommand(
                    "INSERT INTO backup_metadata (key, value) VALUES ('backup_type', 'incremental'), ('backup_time', @time), ('record_count', @count)",
                    connection))
                {
                    cmd.Parameters.AddWithValue("@time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@count", records.Count.ToString());
                    await cmd.ExecuteNonQueryAsync();
                }

                // 插入数据
                foreach (var record in records)
                {
                    string insertSql = @"
                        INSERT OR REPLACE INTO train_ride_info (
                            id, ticket_number, check_in_location, depart_station, train_no, arrive_station,
                            depart_station_pinyin, arrive_station_pinyin, depart_date, depart_time, coach_no,
                            seat_no, money, seat_type, additional_info, ticket_purpose, ticket_modification_type,
                            ticket_type_flags, payment_channel_flags, hint, depart_station_code, arrive_station_code, status
                        ) VALUES (
                            @Id, @TicketNumber, @CheckInLocation, @DepartStation, @TrainNo, @ArriveStation,
                            @DepartStationPinyin, @ArriveStationPinyin, @DepartDate, @DepartTime, @CoachNo,
                            @SeatNo, @Money, @SeatType, @AdditionalInfo, @TicketPurpose, @TicketModificationType,
                            @TicketTypeFlags, @PaymentChannelFlags, @Hint, @DepartStationCode, @ArriveStationCode, @Status
                        )
                    ";

                    using (var cmd = new SqliteCommand(insertSql, connection))
                    {
                        cmd.Parameters.AddWithValue("@Id", record.Id);
                        cmd.Parameters.AddWithValue("@TicketNumber", record.TicketNumber ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@CheckInLocation", record.CheckInLocation ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@DepartStation", record.DepartStation ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@TrainNo", record.TrainNo ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ArriveStation", record.ArriveStation ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@DepartStationPinyin", record.DepartStationPinyin ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ArriveStationPinyin", record.ArriveStationPinyin ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@DepartDate", record.DepartDate ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@DepartTime", record.DepartTime ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@CoachNo", record.CoachNo ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@SeatNo", record.SeatNo ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Money", record.Money);
                        cmd.Parameters.AddWithValue("@SeatType", record.SeatType ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@AdditionalInfo", record.AdditionalInfo ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@TicketPurpose", record.TicketPurpose ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@TicketModificationType", record.TicketModificationType ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@TicketTypeFlags", record.TicketTypeFlags);
                        cmd.Parameters.AddWithValue("@PaymentChannelFlags", record.PaymentChannelFlags);
                        cmd.Parameters.AddWithValue("@Hint", record.Hint ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@DepartStationCode", record.DepartStationCode ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ArriveStationCode", record.ArriveStationCode ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Status", record.Status);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        /// <summary>
        /// 压缩备份文件
        /// </summary>
        private string CompressBackupFile(string backupPath)
        {
            try
            {
                // 强制关闭SQLite连接池中的连接，释放文件句柄
                SqliteConnection.ClearAllPools();
                
                // 等待文件句柄完全释放
                Thread.Sleep(500);
                
                string zipPath = backupPath + ".zip";
                string fileName = Path.GetFileName(backupPath);

                // 确保文件未被占用
                int retryCount = 0;
                const int maxRetries = 5;
                while (retryCount < maxRetries)
                {
                    try
                    {
                        // 尝试打开文件检查是否被占用
                        using (var fs = File.Open(backupPath, FileMode.Open, FileAccess.Read, FileShare.None))
                        {
                            // 文件可以打开，说明未被占用
                            break;
                        }
                    }
                    catch (IOException)
                    {
                        // 文件被占用，等待后重试
                        retryCount++;
                        if (retryCount >= maxRetries)
                        {
                            throw new IOException("文件被占用，无法压缩");
                        }
                        Thread.Sleep(500);
                    }
                }

                using (var zipArchive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                {
                    zipArchive.CreateEntryFromFile(backupPath, fileName, CompressionLevel.Optimal);
                }

                // 再次清理连接池，确保删除操作可以成功
                SqliteConnection.ClearAllPools();
                Thread.Sleep(200);
                
                File.Delete(backupPath);

                return zipPath;
            }
            catch (Exception ex)
            {
                _logService.Error("IncrementalBackupService", $"压缩备份文件失败: {ex.Message}");
                return backupPath;
            }
        }

        /// <summary>
        /// 获取上次备份时间
        /// </summary>
        private async Task<DateTime?> GetLastBackupTimeAsync()
        {
            var records = await LoadBackupRecordsAsync();
            var lastBackup = records.OrderByDescending(r => r.BackupTime).FirstOrDefault();
            return lastBackup?.DataEndTime;
        }

        /// <summary>
        /// 获取上次备份时的记录数
        /// </summary>
        private async Task<int> GetLastBackupRecordCountAsync()
        {
            var records = await LoadBackupRecordsAsync();
            var lastFullBackup = records
                .Where(r => r.BackupType == "Full")
                .OrderByDescending(r => r.BackupTime)
                .FirstOrDefault();

            if (lastFullBackup == null)
            {
                // 如果没有全量备份，返回0
                return 0;
            }

            // 计算全量备份后的所有增量备份记录数
            var incrementalRecords = records
                .Where(r => r.BackupType == "Incremental" && r.BackupTime > lastFullBackup.BackupTime)
                .Sum(r => r.RecordCount);

            return lastFullBackup.RecordCount + incrementalRecords;
        }

        /// <summary>
        /// 保存备份记录
        /// </summary>
        private async Task SaveBackupRecordAsync(IncrementalBackupInfo backupInfo)
        {
            var records = await LoadBackupRecordsAsync();
            records.Add(backupInfo);
            await Task.Run(() =>
            {
                JsonConfigManager.Instance.SaveConfig(BackupRecordFileName, records);
            });
        }

        /// <summary>
        /// 加载备份记录
        /// </summary>
        private async Task<List<IncrementalBackupInfo>> LoadBackupRecordsAsync()
        {
            return await Task.Run(() =>
            {
                return JsonConfigManager.Instance.LoadConfig<List<IncrementalBackupInfo>>(BackupRecordFileName, new List<IncrementalBackupInfo>());
            });
        }

        /// <summary>
        /// 获取所有备份记录
        /// </summary>
        public async Task<List<IncrementalBackupInfo>> GetBackupRecordsAsync()
        {
            return await LoadBackupRecordsAsync();
        }

        /// <summary>
        /// 删除备份记录及文件
        /// </summary>
        public async Task<bool> DeleteBackupAsync(string backupId)
        {
            try
            {
                var records = await LoadBackupRecordsAsync();
                var record = records.FirstOrDefault(r => r.Id == backupId);

                if (record == null)
                {
                    return false;
                }

                // 删除文件
                if (File.Exists(record.BackupPath))
                {
                    File.Delete(record.BackupPath);
                }

                // 删除记录
                records.Remove(record);
                await Task.Run(() =>
                {
                    JsonConfigManager.Instance.SaveConfig(BackupRecordFileName, records);
                });

                return true;
            }
            catch (Exception ex)
            {
                _logService.Error("IncrementalBackupService", $"删除备份失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 清理旧备份
        /// </summary>
        public async Task CleanupOldBackupsAsync(int keepCount)
        {
            try
            {
                var records = await LoadBackupRecordsAsync();
                var orderedRecords = records.OrderByDescending(r => r.BackupTime).ToList();

                if (orderedRecords.Count > keepCount)
                {
                    var recordsToDelete = orderedRecords.Skip(keepCount).ToList();

                    foreach (var record in recordsToDelete)
                    {
                        if (File.Exists(record.BackupPath))
                        {
                            File.Delete(record.BackupPath);
                        }
                        records.Remove(record);
                    }

                    await Task.Run(() =>
                    {
                        JsonConfigManager.Instance.SaveConfig(BackupRecordFileName, records);
                    });

                    _logService.Info("IncrementalBackupService", $"清理旧备份: {recordsToDelete.Count}个");
                }
            }
            catch (Exception ex)
            {
                _logService.Error("IncrementalBackupService", $"清理旧备份失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载配置
        /// </summary>
        private DatabaseConfig LoadConfig()
        {
            return JsonConfigManager.Instance.LoadConfig<DatabaseConfig>("databasesettings.json", new DatabaseConfig());
        }

        /// <summary>
        /// 映射数据库读取器到对象
        /// </summary>
        private TrainRideInfo MapReaderToTrainRideInfo(SqliteDataReader reader)
        {
            return new TrainRideInfo
            {
                Id = reader.GetInt32(0),
                TicketNumber = reader.IsDBNull(1) ? null : reader.GetString(1),
                CheckInLocation = reader.IsDBNull(2) ? null : reader.GetString(2),
                DepartStation = reader.IsDBNull(3) ? null : reader.GetString(3),
                TrainNo = reader.IsDBNull(4) ? null : reader.GetString(4),
                ArriveStation = reader.IsDBNull(5) ? null : reader.GetString(5),
                DepartStationPinyin = reader.IsDBNull(6) ? null : reader.GetString(6),
                ArriveStationPinyin = reader.IsDBNull(7) ? null : reader.GetString(7),
                DepartDate = reader.IsDBNull(8) ? null : reader.GetString(8),
                DepartTime = reader.IsDBNull(9) ? null : reader.GetString(9),
                CoachNo = reader.IsDBNull(10) ? null : reader.GetString(10),
                SeatNo = reader.IsDBNull(11) ? null : reader.GetString(11),
                Money = reader.IsDBNull(12) ? 0 : reader.GetDecimal(12),
                SeatType = reader.IsDBNull(13) ? null : reader.GetString(13),
                AdditionalInfo = reader.IsDBNull(14) ? null : reader.GetString(14),
                TicketPurpose = reader.IsDBNull(15) ? null : reader.GetString(15),
                TicketModificationType = reader.IsDBNull(16) ? null : reader.GetString(16),
                TicketTypeFlags = reader.IsDBNull(17) ? 0 : reader.GetInt32(17),
                PaymentChannelFlags = reader.IsDBNull(18) ? 0 : reader.GetInt32(18),
                Hint = reader.IsDBNull(19) ? null : reader.GetString(19),
                DepartStationCode = reader.IsDBNull(20) ? null : reader.GetString(20),
                ArriveStationCode = reader.IsDBNull(21) ? null : reader.GetString(21),
                Status = reader.IsDBNull(22) ? 0 : reader.GetInt32(22)
            };
        }
    }

    /// <summary>
    /// 增量备份结果
    /// </summary>
    public class IncrementalBackupResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? BackupPath { get; set; }
        public int RecordCount { get; set; }
        public long FileSize { get; set; }
        public IncrementalBackupInfo? BackupInfo { get; set; }
    }
}
