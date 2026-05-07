using System;
using System.Threading.Tasks;
using GuiPiao.DataAccess;

namespace GuiPiao.Services;

/// <summary>
///     数据库维护服务 - 提供数据库维护和优化功能
/// </summary>
public class DatabaseMaintenanceService
{
    private readonly LogService _logService;
    private readonly DatabaseMaintenanceRepository _maintenanceRepository;

    public DatabaseMaintenanceService()
    {
        _maintenanceRepository = new DatabaseMaintenanceRepository();
        _logService = new LogService();
    }

    /// <summary>
    ///     执行数据完整性校验
    /// </summary>
    public async Task<MaintenanceResult> VerifyIntegrityAsync()
    {
        try
        {
            _logService.Info("DatabaseMaintenanceService", "开始执行数据完整性校验");

            var result = await _maintenanceRepository.CheckIntegrityAsync();

            if (result.IsOk)
                _logService.Info("DatabaseMaintenanceService", "数据完整性校验通过");
            else
                _logService.Error("DatabaseMaintenanceService", $"数据完整性校验失败: {result.Details}");

            return new MaintenanceResult
            {
                Success = result.IsOk,
                Message = result.Message,
                Details = result.Details
            };
        }
        catch (Exception ex)
        {
            _logService.Error("DatabaseMaintenanceService", $"执行完整性校验时发生错误: {ex.Message}");
            return new MaintenanceResult
            {
                Success = false,
                Message = "执行完整性校验失败",
                Details = ex.Message
            };
        }
    }

    /// <summary>
    ///     执行数据库碎片整理（VACUUM）
    /// </summary>
    public async Task<DefragmentResult> DefragmentAsync()
    {
        try
        {
            _logService.Info("DatabaseMaintenanceService", "开始执行数据库碎片整理");

            var result = await _maintenanceRepository.VacuumAsync();

            if (result.Success)
                _logService.Info("DatabaseMaintenanceService",
                    $"碎片整理完成，原大小: {result.FormattedOriginalSize}, " +
                    $"新大小: {result.FormattedNewSize}, " +
                    $"减少: {result.FormattedReducedSize}, " +
                    $"耗时: {result.Duration.TotalSeconds:F1}秒");
            else
                _logService.Error("DatabaseMaintenanceService", $"碎片整理失败: {result.Message}");

            return new DefragmentResult
            {
                Success = result.Success,
                Message = result.Message,
                OriginalSize = result.OriginalSize,
                NewSize = result.NewSize,
                ReducedSize = result.ReducedSize,
                Duration = result.Duration,
                FormattedOriginalSize = result.FormattedOriginalSize,
                FormattedNewSize = result.FormattedNewSize,
                FormattedReducedSize = result.FormattedReducedSize
            };
        }
        catch (Exception ex)
        {
            _logService.Error("DatabaseMaintenanceService", $"执行碎片整理时发生错误: {ex.Message}");
            return new DefragmentResult
            {
                Success = false,
                Message = $"碎片整理失败: {ex.Message}"
            };
        }
    }

    /// <summary>
    ///     获取数据库统计信息
    /// </summary>
    public async Task<DatabaseStatsResult> GetDatabaseStatsAsync()
    {
        try
        {
            var stats = await _maintenanceRepository.GetDatabaseStatsAsync();

            return new DatabaseStatsResult
            {
                Success = true,
                TableCount = stats.TableCount,
                IndexCount = stats.IndexCount,
                TicketCount = stats.TicketCount,
                PageSize = stats.PageSize,
                PageCount = stats.PageCount,
                FreePages = stats.FreePages,
                FragmentationRatio = stats.FragmentationRatio,
                FormattedFragmentationRatio = stats.FormattedFragmentationRatio
            };
        }
        catch (Exception ex)
        {
            _logService.Error("DatabaseMaintenanceService", $"获取数据库统计信息失败: {ex.Message}");
            return new DatabaseStatsResult
            {
                Success = false,
                Message = $"获取统计信息失败: {ex.Message}"
            };
        }
    }

    /// <summary>
    ///     检查是否需要执行碎片整理
    /// </summary>
    public async Task<bool> ShouldDefragmentAsync(double threshold = 0.1)
    {
        try
        {
            var stats = await _maintenanceRepository.GetDatabaseStatsAsync();
            return stats.FragmentationRatio > threshold;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
///     维护操作结果
/// </summary>
public class MaintenanceResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}

/// <summary>
///     碎片整理结果
/// </summary>
public class DefragmentResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public long OriginalSize { get; set; }
    public long NewSize { get; set; }
    public long ReducedSize { get; set; }
    public TimeSpan Duration { get; set; }
    public string FormattedOriginalSize { get; set; } = string.Empty;
    public string FormattedNewSize { get; set; } = string.Empty;
    public string FormattedReducedSize { get; set; } = string.Empty;
}

/// <summary>
///     数据库统计结果
/// </summary>
public class DatabaseStatsResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int TableCount { get; set; }
    public int IndexCount { get; set; }
    public int TicketCount { get; set; }
    public int PageSize { get; set; }
    public int PageCount { get; set; }
    public int FreePages { get; set; }
    public double FragmentationRatio { get; set; }
    public string FormattedFragmentationRatio { get; set; } = string.Empty;
}