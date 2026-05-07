using System;
using System.IO;
using System.Threading.Tasks;
using GuiPiao.Model;
using GuiPiao.Utils;

namespace GuiPiao.Services;

/// <summary>
///     数据库生命周期服务 - 处理程序启动和退出时的数据库操作
/// </summary>
public class DatabaseLifecycleService
{
    private readonly DatabaseBackupService _backupService;
    private readonly string _configFileName = "databasesettings.json";
    private readonly LogService _logService;
    private readonly DatabaseMaintenanceService _maintenanceService;

    public DatabaseLifecycleService()
    {
        _backupService = new DatabaseBackupService();
        _maintenanceService = new DatabaseMaintenanceService();
        _logService = new LogService();
    }

    /// <summary>
    ///     程序启动时执行的操作
    /// </summary>
    public async Task OnStartupAsync()
    {
        try
        {
            var config = LoadConfig();

            // 1. 执行启动时自动备份
            if (config.AutoBackupEnabled)
            {
                // 检查是否需要执行启动时备份
                var shouldBackup = ShouldExecuteBackupOnStartup(config.BackupTiming);

                if (shouldBackup)
                {
                    _logService.Info("DatabaseLifecycleService", $"执行启动时自动备份，时机: {config.BackupTiming}");
                    var backupPath = _backupService.AutoBackup();

                    if (!string.IsNullOrEmpty(backupPath))
                    {
                        _logService.Info("DatabaseLifecycleService", $"启动时自动备份成功: {backupPath}");
                    }
                    else
                    {
                        _logService.Error("DatabaseLifecycleService", "启动时自动备份失败");
                        if (config.ShowErrorOnFail)
                            // 记录错误，但不阻塞启动
                            _logService.Error("DatabaseLifecycleService", "启动时自动备份失败，请检查日志");
                    }
                }
                else
                {
                    _logService.Info("DatabaseLifecycleService", "当前不满足备份时机条件，跳过启动时备份");
                }
            }

            // 2. 执行每月自动碎片整理
            if (config.AutoDefragmentMonthly && ShouldExecuteMonthlyDefragment())
            {
                _logService.Info("DatabaseLifecycleService", "执行每月自动碎片整理");
                var result = await _maintenanceService.DefragmentAsync();

                if (result.Success)
                {
                    _logService.Info("DatabaseLifecycleService", $"每月自动碎片整理完成，减少空间: {result.FormattedReducedSize}");
                    // 记录上次执行时间
                    RecordLastDefragmentTime();
                }
                else
                {
                    _logService.Error("DatabaseLifecycleService", $"每月自动碎片整理失败: {result.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            _logService.Error("DatabaseLifecycleService", $"启动时操作失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     程序退出时执行的操作
    /// </summary>
    public async Task OnExitAsync()
    {
        try
        {
            var config = LoadConfig();

            // 1. 执行退出时自动备份
            if (config.AutoBackupEnabled && config.BackupTiming == "OnExit")
            {
                _logService.Info("DatabaseLifecycleService", "执行退出时自动备份");
                var backupPath = _backupService.AutoBackup();

                if (!string.IsNullOrEmpty(backupPath))
                {
                    _logService.Info("DatabaseLifecycleService", $"退出时自动备份成功: {backupPath}");
                }
                else
                {
                    _logService.Error("DatabaseLifecycleService", "退出时自动备份失败");
                    if (config.ShowErrorOnFail)
                        // 退出时无法显示弹窗，只能记录日志
                        _logService.Error("DatabaseLifecycleService", "退出时自动备份失败，请检查日志");
                }
            }

            // 2. 执行退出时自动校验
            if (config.AutoVerifyOnExit)
            {
                _logService.Info("DatabaseLifecycleService", "执行退出时数据完整性校验");
                var result = await _maintenanceService.VerifyIntegrityAsync();

                if (result.Success)
                    _logService.Info("DatabaseLifecycleService", "退出时数据完整性校验通过");
                else
                    _logService.Error("DatabaseLifecycleService", $"退出时数据完整性校验失败: {result.Message}");
            }
        }
        catch (Exception ex)
        {
            _logService.Error("DatabaseLifecycleService", $"退出时操作失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     检查是否应该执行启动时备份
    /// </summary>
    private bool ShouldExecuteBackupOnStartup(string backupTiming)
    {
        return backupTiming switch
        {
            "OnStartup" => true, // 每次启动都备份
            "Weekly" => DateTime.Now.DayOfWeek == DayOfWeek.Monday, // 每周一
            "Monthly" => DateTime.Now.Day == 1, // 每月1号
            _ => false
        };
    }

    /// <summary>
    ///     检查是否应该执行每月碎片整理（每月1号且本月未执行过）
    /// </summary>
    private bool ShouldExecuteMonthlyDefragment()
    {
        try
        {
            // 只在每月1号执行
            if (DateTime.Now.Day != 1) return false;

            // 检查本月是否已执行过
            var recordFile = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GuiPiao",
                "Config",
                "last_defragment.txt"
            );

            if (File.Exists(recordFile))
            {
                var lastRecord = File.ReadAllText(recordFile).Trim();
                if (DateTime.TryParse(lastRecord, out var lastDefragmentTime))
                    // 如果本月已经执行过，则跳过
                    if (lastDefragmentTime.Year == DateTime.Now.Year &&
                        lastDefragmentTime.Month == DateTime.Now.Month)
                    {
                        _logService.Info("DatabaseLifecycleService", "本月已执行过碎片整理，跳过");
                        return false;
                    }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logService.Error("DatabaseLifecycleService", $"检查碎片整理条件失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    ///     记录上次碎片整理时间
    /// </summary>
    private void RecordLastDefragmentTime()
    {
        try
        {
            var recordFile = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GuiPiao",
                "Config",
                "last_defragment.txt"
            );

            var directory = Path.GetDirectoryName(recordFile);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) Directory.CreateDirectory(directory);

            File.WriteAllText(recordFile, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            _logService.Info("DatabaseLifecycleService", "已记录碎片整理时间");
        }
        catch (Exception ex)
        {
            _logService.Error("DatabaseLifecycleService", $"记录碎片整理时间失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     加载配置
    /// </summary>
    private DatabaseConfig LoadConfig()
    {
        try
        {
            return JsonConfigManager.Instance.LoadConfig<DatabaseConfig>(_configFileName, new DatabaseConfig());
        }
        catch
        {
            return new DatabaseConfig();
        }
    }

    /// <summary>
    ///     记录上次启动备份时间（用于Weekly/Monthly判断）
    /// </summary>
    public void RecordLastStartupBackupTime()
    {
        try
        {
            var config = LoadConfig();
            // 可以扩展配置来记录上次备份时间
            // 这里简化处理，实际可以通过备份文件的时间戳来判断
        }
        catch (Exception ex)
        {
            _logService.Error("DatabaseLifecycleService", $"记录备份时间失败: {ex.Message}");
        }
    }
}