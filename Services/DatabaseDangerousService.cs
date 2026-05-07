using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using GuiPiao.DataAccess;

namespace GuiPiao.Services;

/// <summary>
///     数据库高危操作服务 - 提供危险的数据库操作功能
/// </summary>
public class DatabaseDangerousService
{
    private readonly DatabaseBackupService _backupService;
    private readonly DatabaseDangerousRepository _dangerousRepository;
    private readonly LogService _logService;

    public DatabaseDangerousService()
    {
        _dangerousRepository = new DatabaseDangerousRepository();
        _backupService = new DatabaseBackupService();
        _logService = new LogService();
    }

    #region 重置数据库操作

    /// <summary>
    ///     重置数据库到初始状态
    /// </summary>
    public async Task<DangerousOperationResult> ResetDatabaseAsync(bool backupFirst = true)
    {
        try
        {
            _logService.Info("DatabaseDangerousService", "准备重置数据库到初始状态");

            // 先备份（可选）
            string? backupPath = null;
            if (backupFirst)
            {
                backupPath = _backupService.AutoBackup();
                if (!string.IsNullOrEmpty(backupPath))
                    _logService.Info("DatabaseDangerousService", $"已自动备份: {backupPath}");
            }

            // 删除所有表
            await _dangerousRepository.DropAllTablesAsync();
            _logService.Info("DatabaseDangerousService", "已删除所有表");

            // 重新创建初始表结构
            await _dangerousRepository.CreateInitialSchemaAsync();
            _logService.Info("DatabaseDangerousService", "已重新创建初始表结构");

            return new DangerousOperationResult
            {
                Success = true,
                Message = "数据库已重置到初始状态",
                BackupPath = backupPath
            };
        }
        catch (Exception ex)
        {
            _logService.Error("DatabaseDangerousService", $"重置数据库失败: {ex.Message}");
            return new DangerousOperationResult
            {
                Success = false,
                Message = $"重置失败: {ex.Message}"
            };
        }
    }

    #endregion

    #region 清空数据操作

    /// <summary>
    ///     清空全部票务记录
    /// </summary>
    public async Task<DangerousOperationResult> ClearAllTrainRidesAsync(bool backupFirst = true)
    {
        try
        {
            _logService.Info("DatabaseDangerousService", "准备清空全部票务记录");

            // 获取当前记录数
            var count = await _dangerousRepository.GetTrainRideCountAsync();

            if (count == 0)
                return new DangerousOperationResult
                {
                    Success = true,
                    Message = "当前没有票务记录，无需清空",
                    AffectedRows = 0
                };

            // 先备份（可选）
            string? backupPath = null;
            if (backupFirst)
            {
                backupPath = _backupService.AutoBackup();
                if (!string.IsNullOrEmpty(backupPath))
                    _logService.Info("DatabaseDangerousService", $"已自动备份: {backupPath}");
            }

            // 执行清空
            var deletedCount = await _dangerousRepository.ClearAllTrainRidesAsync();

            _logService.Info("DatabaseDangerousService", $"已清空 {deletedCount} 条票务记录");

            return new DangerousOperationResult
            {
                Success = true,
                Message = $"成功清空 {deletedCount} 条票务记录",
                AffectedRows = deletedCount,
                BackupPath = backupPath
            };
        }
        catch (Exception ex)
        {
            _logService.Error("DatabaseDangerousService", $"清空票务记录失败: {ex.Message}");
            return new DangerousOperationResult
            {
                Success = false,
                Message = $"清空失败: {ex.Message}",
                AffectedRows = 0
            };
        }
    }

    /// <summary>
    ///     获取当前票务记录数量
    /// </summary>
    public async Task<int> GetTrainRideCountAsync()
    {
        try
        {
            return await _dangerousRepository.GetTrainRideCountAsync();
        }
        catch (Exception ex)
        {
            _logService.Error("DatabaseDangerousService", $"获取记录数失败: {ex.Message}");
            return 0;
        }
    }

    #endregion

    #region 导出SQL功能

    /// <summary>
    ///     导出数据库表结构SQL
    /// </summary>
    public async Task<ExportSqlResult> ExportSchemaSqlAsync(string? targetPath = null)
    {
        try
        {
            _logService.Info("DatabaseDangerousService", "开始导出数据库表结构SQL");

            // 生成SQL
            var sql = await _dangerousRepository.GenerateSchemaSqlAsync();

            // 如果指定了路径，保存到文件
            string? savedPath = null;
            if (!string.IsNullOrEmpty(targetPath))
            {
                var directory = Path.GetDirectoryName(targetPath) ?? Directory.GetCurrentDirectory();
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

                await File.WriteAllTextAsync(targetPath, sql, Encoding.UTF8);
                savedPath = targetPath;

                _logService.Info("DatabaseDangerousService", $"SQL已保存到: {savedPath}");
            }

            return new ExportSqlResult
            {
                Success = true,
                Sql = sql,
                SavedPath = savedPath,
                Message = savedPath != null ? $"SQL已导出到: {savedPath}" : "SQL已生成"
            };
        }
        catch (Exception ex)
        {
            _logService.Error("DatabaseDangerousService", $"导出SQL失败: {ex.Message}");
            return new ExportSqlResult
            {
                Success = false,
                Message = $"导出失败: {ex.Message}"
            };
        }
    }

    /// <summary>
    ///     导出表数据SQL
    /// </summary>
    public async Task<ExportSqlResult> ExportTableDataSqlAsync(string tableName, string targetPath, int limit = 1000)
    {
        try
        {
            _logService.Info("DatabaseDangerousService", $"开始导出表 {tableName} 的数据SQL");

            // 生成SQL
            var sql = await _dangerousRepository.GenerateTableDataSqlAsync(tableName, limit);

            // 保存到文件
            var directory = Path.GetDirectoryName(targetPath) ?? Directory.GetCurrentDirectory();
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            await File.WriteAllTextAsync(targetPath, sql, Encoding.UTF8);

            _logService.Info("DatabaseDangerousService", $"表 {tableName} 的数据SQL已保存到: {targetPath}");

            return new ExportSqlResult
            {
                Success = true,
                Sql = sql,
                SavedPath = targetPath,
                Message = $"表 {tableName} 的数据已导出到: {targetPath}"
            };
        }
        catch (Exception ex)
        {
            _logService.Error("DatabaseDangerousService", $"导出表数据失败: {ex.Message}");
            return new ExportSqlResult
            {
                Success = false,
                Message = $"导出失败: {ex.Message}"
            };
        }
    }

    #endregion
}

/// <summary>
///     高危操作结果
/// </summary>
public class DangerousOperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int AffectedRows { get; set; }
    public string? BackupPath { get; set; }
    public bool HasBackup => !string.IsNullOrEmpty(BackupPath);
}

/// <summary>
///     SQL导出结果
/// </summary>
public class ExportSqlResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Sql { get; set; } = string.Empty;
    public string? SavedPath { get; set; }
}