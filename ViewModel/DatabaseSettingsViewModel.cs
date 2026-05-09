using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GuiPiao.Messages;
using GuiPiao.Model;
using GuiPiao.Services;
using GuiPiao.Utils;
using GuiPiao.View;
using Application = System.Windows.Application;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace GuiPiao.ViewModel;

/// <summary>
///     数据库设置视图模型
/// </summary>
public partial class DatabaseSettingsViewModel : ObservableObject, ISettingsViewModel
{
    private readonly DatabaseBackupService _backupService;
    private readonly DatabaseDangerousService _dangerousService;
    private readonly DatabaseInfoService _databaseInfoService;
    private readonly DatabaseMaintenanceService _maintenanceService;
    private readonly DatabaseMigrationService _migrationService;
    private readonly DatabaseRestoreService _restoreService;
    private readonly DatabaseValidationService _validationService;
    private DatabaseConfig _config = new();
    private DatabaseConfig _originalConfig = new();

    public DatabaseSettingsViewModel()
    {
        _databaseInfoService = new DatabaseInfoService();
        _migrationService = new DatabaseMigrationService();
        _backupService = new DatabaseBackupService();
        _restoreService = new DatabaseRestoreService();
        _maintenanceService = new DatabaseMaintenanceService();
        _dangerousService = new DatabaseDangerousService();
        _validationService = new DatabaseValidationService();

        if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            return;

        // 加载配置
        LoadSettings();

        // 加载数据库信息
        _ = LoadDatabaseInfoAsync();
    }

    /// <summary>
    ///     是否有未保存的更改
    /// </summary>
    public bool HasUnsavedChanges
    {
        get
        {
            if (_originalConfig == null)
                return false;

            return AutoBackupEnabled != _originalConfig.AutoBackupEnabled ||
                   BackupType != _originalConfig.BackupType ||
                   BackupTiming != _originalConfig.BackupTiming ||
                   MaxBackupCount != _originalConfig.MaxBackupCount ||
                   FullBackupFrequency != _originalConfig.FullBackupFrequency ||
                   MaxFullBackupCount != _originalConfig.MaxFullBackupCount ||
                   IncrementalBackupFrequency != _originalConfig.IncrementalBackupFrequency ||
                   MaxIncrementalBackupCount != _originalConfig.MaxIncrementalBackupCount ||
                   BackupPath != _originalConfig.BackupPath ||
                   AutoCompress != _originalConfig.AutoCompress ||
                   ShowErrorOnFail != _originalConfig.ShowErrorOnFail ||
                   AutoVerifyOnExit != _originalConfig.AutoVerifyOnExit ||
                   AutoDefragmentMonthly != _originalConfig.AutoDefragmentMonthly;
        }
    }

    /// <summary>
    ///     加载设置
    /// </summary>
    private void LoadSettings()
    {
        try
        {
            _config = JsonConfigManager.Instance.LoadConfig<DatabaseConfig>("databasesettings.json",
                new DatabaseConfig());
            _originalConfig = new DatabaseConfig
            {
                AutoBackupEnabled = _config.AutoBackupEnabled,
                BackupType = _config.BackupType,
                BackupTiming = _config.BackupTiming,
                MaxBackupCount = _config.MaxBackupCount,
                FullBackupFrequency = _config.FullBackupFrequency,
                MaxFullBackupCount = _config.MaxFullBackupCount,
                IncrementalBackupFrequency = _config.IncrementalBackupFrequency,
                MaxIncrementalBackupCount = _config.MaxIncrementalBackupCount,
                BackupPath = _config.BackupPath,
                AutoCompress = _config.AutoCompress,
                ShowErrorOnFail = _config.ShowErrorOnFail,
                AutoVerifyOnExit = _config.AutoVerifyOnExit,
                AutoDefragmentMonthly = _config.AutoDefragmentMonthly
            };

            // 应用配置到属性
            AutoBackupEnabled = _config.AutoBackupEnabled;
            BackupType = _config.BackupType;
            BackupTiming = _config.BackupTiming;
            MaxBackupCount = _config.MaxBackupCount;
            FullBackupFrequency = _config.FullBackupFrequency;
            MaxFullBackupCount = _config.MaxFullBackupCount;
            IncrementalBackupFrequency = _config.IncrementalBackupFrequency;
            MaxIncrementalBackupCount = _config.MaxIncrementalBackupCount;
            BackupPath = string.IsNullOrEmpty(_config.BackupPath)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GuiPiao",
                    "Backups")
                : _config.BackupPath;
            AutoCompress = _config.AutoCompress;
            ShowErrorOnFail = _config.ShowErrorOnFail;
            AutoVerifyOnExit = _config.AutoVerifyOnExit;
            AutoDefragmentMonthly = _config.AutoDefragmentMonthly;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"加载设置失败: {ex.Message}");
            // 使用默认值
            BackupPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GuiPiao",
                "Backups"
            );
        }
    }

    /// <summary>
    ///     保存设置到配置
    /// </summary>
    private void SaveConfig()
    {
        _config.AutoBackupEnabled = AutoBackupEnabled;
        _config.BackupType = BackupType;
        _config.BackupTiming = BackupTiming;
        _config.MaxBackupCount = MaxBackupCount;
        _config.FullBackupFrequency = FullBackupFrequency;
        _config.MaxFullBackupCount = MaxFullBackupCount;
        _config.IncrementalBackupFrequency = IncrementalBackupFrequency;
        _config.MaxIncrementalBackupCount = MaxIncrementalBackupCount;
        _config.BackupPath = BackupPath;
        _config.AutoCompress = AutoCompress;
        _config.ShowErrorOnFail = ShowErrorOnFail;
        _config.AutoVerifyOnExit = AutoVerifyOnExit;
        _config.AutoDefragmentMonthly = AutoDefragmentMonthly;

        JsonConfigManager.Instance.SaveConfig("databasesettings.json", _config);

        WeakReferenceMessenger.Default.Send(new SettingsChangedMessage("Database"));

        // 更新原始配置
        _originalConfig = new DatabaseConfig
        {
            AutoBackupEnabled = _config.AutoBackupEnabled,
            BackupType = _config.BackupType,
            BackupTiming = _config.BackupTiming,
            MaxBackupCount = _config.MaxBackupCount,
            FullBackupFrequency = _config.FullBackupFrequency,
            MaxFullBackupCount = _config.MaxFullBackupCount,
            IncrementalBackupFrequency = _config.IncrementalBackupFrequency,
            MaxIncrementalBackupCount = _config.MaxIncrementalBackupCount,
            BackupPath = _config.BackupPath,
            AutoCompress = _config.AutoCompress,
            ShowErrorOnFail = _config.ShowErrorOnFail,
            AutoVerifyOnExit = _config.AutoVerifyOnExit,
            AutoDefragmentMonthly = _config.AutoDefragmentMonthly
        };
    }

    /// <summary>
    ///     加载数据库信息
    /// </summary>
    private async Task LoadDatabaseInfoAsync()
    {
        try
        {
            // 获取数据库文件信息
            var fileInfo = _databaseInfoService.GetDatabaseFileInfo();
            DatabasePath = fileInfo.Path;
            FileSize = fileInfo.Size;
            CreateTime = fileInfo.CreateTime;
            LastBackupTime = fileInfo.LastBackupTime;

            // 获取票务记录数
            TicketCount = await _databaseInfoService.GetTicketCountAsync();
        }
        catch (Exception ex)
        {
            // 静默处理，避免影响UI加载
            Debug.WriteLine($"加载数据库信息失败: {ex.Message}");
        }
    }

    #region 数据库基础信息

    [ObservableProperty] private string _databasePath = string.Empty;

    [ObservableProperty] private string _fileSize = "-";

    [ObservableProperty] private string _createTime = "-";

    [ObservableProperty] private string _lastBackupTime = "-";

    [ObservableProperty] private int _ticketCount;

    #endregion

    #region 自动备份策略配置

    [ObservableProperty] private bool _autoBackupEnabled = true;

    [ObservableProperty] private string _backupType = "Full";

    [ObservableProperty] private string _backupTiming = "OnExit";

    [ObservableProperty] private int _maxBackupCount = 10;

    [ObservableProperty] private string _fullBackupFrequency = "Weekly";

    [ObservableProperty] private int _maxFullBackupCount = 5;

    [ObservableProperty] private string _incrementalBackupFrequency = "Daily";

    [ObservableProperty] private int _maxIncrementalBackupCount = 30;

    [ObservableProperty] private string _backupPath = string.Empty;

    [ObservableProperty] private bool _autoCompress = true;

    [ObservableProperty] private bool _showErrorOnFail = true;

    // 备份类型选项
    public bool IsFullBackup
    {
        get => BackupType == "Full";
        set
        {
            if (value) BackupType = "Full";
        }
    }

    public bool IsIncrementalBackup
    {
        get => BackupType == "Incremental";
        set
        {
            if (value) BackupType = "Incremental";
        }
    }

    public bool IsSmartBackup
    {
        get => BackupType == "Smart";
        set
        {
            if (value) BackupType = "Smart";
        }
    }

    // 显示控制属性
    public bool ShowFullBackupSettings => BackupType == "Full" || BackupType == "Smart";
    public bool ShowIncrementalSettings => BackupType == "Incremental" || BackupType == "Smart";
    public bool ShowTraditionalSettings => BackupType == "Full";

    #endregion

    #region 数据库维护与优化

    [ObservableProperty] private bool _autoVerifyOnExit = true;

    [ObservableProperty] private bool _autoDefragmentMonthly = true;

    #endregion

    #region 命令

    /// <summary>
    ///     打开数据库目录
    /// </summary>
    [RelayCommand]
    private void OpenDirectory()
    {
        try
        {
            var success = _databaseInfoService.OpenDatabaseDirectory();
            if (!success)
            {
                var owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
                MessageBoxWindow.Show(owner,
                    "数据库文件不存在，无法打开目录。\n\n请检查：\n1. 数据库文件是否被移动或删除\n2. 当前路径是否正确\n3. 如需更改存储位置，请使用[修改存储路径]功能",
                    "文件不存在", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            var owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
            MessageBoxWindow.Show(owner, $"打开目录失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    ///     打开已有数据库
    /// </summary>
    [RelayCommand]
    private async Task OpenExistingDatabase()
    {
        var owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);

        try
        {
            // 显示文件选择对话框
            var dialog = new OpenFileDialog
            {
                Title = "选择要打开的数据库文件",
                Filter = "SQLite数据库文件|*.db;*.sqlite;*.sqlite3|所有文件|*.*",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (dialog.ShowDialog() != true) return;

            var selectedPath = dialog.FileName;

            // 检查是否与当前数据库相同
            var currentPath = _databaseInfoService.GetDatabasePath();
            if (currentPath.Equals(selectedPath, StringComparison.OrdinalIgnoreCase))
            {
                MessageBoxWindow.Show(owner, "选择的数据库文件与当前使用的数据库相同。");
                return;
            }

            // 验证数据库
            var validationResult = _validationService.ValidateDatabase(selectedPath);
            if (!validationResult.IsValid)
            {
                MessageBoxWindow.Show(
                    owner,
                    $"无法打开该数据库文件：\n\n{validationResult.ErrorMessage}",
                    "数据库验证失败",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                return;
            }

            // 获取数据库基本信息
            var dbInfo = _validationService.GetDatabaseBasicInfo(selectedPath);

            // 确认切换
            var confirmMessage = "数据库验证通过！\n\n";
            confirmMessage += $"文件路径: {selectedPath}\n";
            confirmMessage += $"文件大小: {FormatFileSize(dbInfo.FileSize)}\n";
            confirmMessage += $"表数量: {dbInfo.TableCount}\n";
            confirmMessage += $"车站数据: {dbInfo.StationCount} 条\n";
            confirmMessage += $"票务记录: {dbInfo.TicketCount} 条\n\n";
            confirmMessage += "⚠ 切换数据库前会自动备份当前数据库。\n";
            confirmMessage += "切换完成后需要重启程序才能生效。\n\n";
            confirmMessage += "是否继续？";

            var confirmResult = MessageBoxWindow.Show(
                owner,
                confirmMessage,
                "确认打开数据库",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (confirmResult != MessageBoxResult.Yes) return;

            // 备份当前数据库
            var backupPath = await Task.Run(() => _backupService.AutoBackup());
            if (string.IsNullOrEmpty(backupPath))
            {
                var backupFailResult = MessageBoxWindow.Show(
                    owner,
                    "自动备份当前数据库失败。\n\n是否仍要继续切换数据库？",
                    "备份失败",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (backupFailResult != MessageBoxResult.Yes) return;
            }

            // 保存新数据库路径到配置
            var config = new DatabaseConfig
            {
                DatabasePath = selectedPath,
                UseCustomPath = true,
                AutoBackupEnabled = AutoBackupEnabled,
                BackupType = BackupType,
                BackupTiming = BackupTiming,
                MaxBackupCount = MaxBackupCount,
                FullBackupFrequency = FullBackupFrequency,
                MaxFullBackupCount = MaxFullBackupCount,
                IncrementalBackupFrequency = IncrementalBackupFrequency,
                MaxIncrementalBackupCount = MaxIncrementalBackupCount,
                BackupPath = BackupPath,
                AutoCompress = AutoCompress,
                ShowErrorOnFail = ShowErrorOnFail,
                AutoVerifyOnExit = AutoVerifyOnExit,
                AutoDefragmentMonthly = AutoDefragmentMonthly
            };
            _migrationService.SaveConfig(config);

            // 显示成功消息
            var successMessage = "数据库切换成功！\n\n";
            successMessage += $"新数据库路径: {selectedPath}\n";
            if (!string.IsNullOrEmpty(backupPath)) successMessage += $"原数据库备份: {backupPath}\n";
            successMessage += "\n点击确定后将自动重启程序。";

            MessageBoxWindow.Show(owner, successMessage, "切换成功");

            // 自动重启
            RestartApplication();
        }
        catch (Exception ex)
        {
            MessageBoxWindow.Show(owner, $"打开数据库时发生错误:\n{ex.Message}", "错误", MessageBoxButton.OK,
                MessageBoxImage.Error);
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

    /// <summary>
    ///     修改存储路径
    /// </summary>
    [RelayCommand]
    private async Task ChangePath()
    {
        var owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);

        try
        {
            // 显示文件夹选择对话框
            var dialog = new SaveFileDialog
            {
                Title = "选择新的数据库存储位置",
                FileName = "guipiao.db",
                DefaultExt = ".db",
                Filter = "SQLite数据库文件|*.db|所有文件|*.*",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (dialog.ShowDialog() != true) return;

            var targetPath = dialog.FileName;

            // 验证目标路径
            var validationResult = _migrationService.ValidateTargetPath(targetPath);
            if (!validationResult.IsValid)
            {
                MessageBoxWindow.Show(owner, validationResult.ErrorMessage, "验证失败", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // 确认迁移
            var confirmResult = MessageBoxWindow.Show(
                owner,
                $"即将把数据库迁移到:\n{targetPath}\n\n迁移前会自动备份原数据库。\n迁移完成后需要重启程序才能生效。\n\n是否继续？",
                "确认迁移",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (confirmResult != MessageBoxResult.Yes) return;

            // 执行迁移
            var result = await _migrationService.MigrateAsync(targetPath);

            if (result.IsSuccess)
            {
                var message = $"数据库迁移成功！\n\n新路径: {result.NewPath}";
                if (!string.IsNullOrEmpty(result.BackupPath)) message += $"\n备份位置: {result.BackupPath}";
                message += "\n\n点击确定后将自动重启程序。";

                MessageBoxWindow.Show(owner, message, "迁移成功");

                // 更新显示的路径
                DatabasePath = result.NewPath!;

                // 自动重启程序
                RestartApplication();
            }
            else
            {
                MessageBoxWindow.Show(owner, $"迁移失败:\n{result.ErrorMessage}", "迁移失败", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBoxWindow.Show(owner, $"修改存储路径时发生错误:\n{ex.Message}", "错误", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    /// <summary>
    ///     选择备份路径
    /// </summary>
    [RelayCommand]
    private void SelectBackupPath()
    {
        var owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);

        try
        {
            var dialog = new FolderBrowserDialog
            {
                Description = "选择备份文件保存位置",
                ShowNewFolderButton = true
            };

            if (!string.IsNullOrEmpty(BackupPath) && Directory.Exists(BackupPath)) dialog.SelectedPath = BackupPath;

            if (dialog.ShowDialog() == DialogResult.OK) BackupPath = dialog.SelectedPath;
        }
        catch (Exception ex)
        {
            MessageBoxWindow.Show(owner, $"选择备份路径失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    ///     立即执行全量备份
    /// </summary>
    [RelayCommand]
    private async Task ImmediateBackup()
    {
        var owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);

        try
        {
            // 先执行备份（在后台线程）
            var backupPath = await Task.Run(() => _backupService.AutoBackup());

            if (!string.IsNullOrEmpty(backupPath))
            {
                MessageBoxWindow.Show(owner, $"全量备份成功！\n\n备份文件: {backupPath}", "备份成功");

                // 刷新最近备份时间显示
                var fileInfo = _databaseInfoService.GetDatabaseFileInfo();
                LastBackupTime = fileInfo.LastBackupTime;
            }
            else
            {
                if (ShowErrorOnFail)
                    MessageBoxWindow.Show(owner, "备份失败，请检查日志了解详情。", "备份失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            if (ShowErrorOnFail)
                MessageBoxWindow.Show(owner, $"备份失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    ///     立即执行增量备份
    /// </summary>
    [RelayCommand]
    private async Task ImmediateIncrementalBackup()
    {
        var owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
        MessageBoxWindow? progressWindow = null;

        try
        {
            // 显示进度对话框
            progressWindow = MessageBoxWindow.ShowProgress("正在执行增量备份，请稍候...", "增量备份中");
            await Task.Delay(100);

            var incrementalService = new IncrementalBackupService();
            var result = await incrementalService.PerformIncrementalBackupAsync();

            // 关闭进度对话框
            progressWindow?.Close();
            progressWindow = null;

            if (result.Success)
            {
                if (result.RecordCount == 0)
                {
                    MessageBoxWindow.Show(owner, "没有需要备份的新数据。", "增量备份");
                }
                else
                {
                    var message = "增量备份成功！\n\n";
                    message += $"备份文件: {result.BackupPath}\n";
                    message += $"备份记录数: {result.RecordCount} 条\n";
                    message += $"文件大小: {FormatFileSize(result.FileSize)}\n";
                    message += $"备份时间: {result.BackupInfo?.BackupTime:yyyy-MM-dd HH:mm:ss}";

                    MessageBoxWindow.Show(owner, message, "增量备份成功");
                }
            }
            else
            {
                MessageBoxWindow.Show(owner, result.Message, "增量备份失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            progressWindow?.Close();
            progressWindow = null;

            MessageBoxWindow.Show(owner, $"增量备份时发生错误:\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    ///     从备份文件恢复
    /// </summary>
    [RelayCommand]
    private async Task RestoreFromBackup()
    {
        var owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);

        try
        {
            // 显示文件选择对话框
            var dialog = new OpenFileDialog
            {
                Title = "选择备份文件",
                Filter = "SQLite数据库文件|*.db|所有文件|*.*",
                InitialDirectory = BackupPath
            };

            if (dialog.ShowDialog() != true) return;

            var backupPath = dialog.FileName;

            // 验证备份文件
            var validationResult = _restoreService.ValidateBackupFile(backupPath);
            if (!validationResult.IsValid)
            {
                MessageBoxWindow.Show(owner, validationResult.ErrorMessage, "验证失败", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // 显示确认对话框
            var confirmMessage = $"即将从以下备份文件恢复数据库:\n{backupPath}\n";
            confirmMessage += $"文件大小: {validationResult.FormattedFileSize}\n\n";
            confirmMessage += "⚠ 恢复操作将完全覆盖当前所有数据，且无法撤销！\n\n";
            confirmMessage += "恢复前会自动备份当前数据库。\n\n是否继续？";

            var confirmResult = MessageBoxWindow.Show(
                owner,
                confirmMessage,
                "确认恢复",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (confirmResult != MessageBoxResult.Yes) return;

            // 执行恢复
            var result = await Task.Run(() => _restoreService.RestoreFromBackup(backupPath));

            if (result.IsSuccess)
            {
                var successMessage = "数据库恢复成功！\n\n";
                if (result.HasCurrentBackup) successMessage += $"恢复前已自动备份当前数据库:\n{result.CurrentBackupPath}\n\n";
                successMessage += "点击确定后将自动重启程序以应用更改。";

                MessageBoxWindow.Show(owner, successMessage, "恢复成功");

                // 自动重启
                RestartApplication();
            }
            else
            {
                MessageBoxWindow.Show(owner, $"❌ {result.ErrorMessage}", "恢复失败", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBoxWindow.Show(owner, $"恢复过程中发生错误:\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    ///     打开备份目录
    /// </summary>
    [RelayCommand]
    private void OpenBackupFolder()
    {
        try
        {
            if (!Directory.Exists(BackupPath)) Directory.CreateDirectory(BackupPath);

            Process.Start(new ProcessStartInfo
            {
                FileName = BackupPath,
                UseShellExecute = true,
                Verb = "open"
            });
        }
        catch (Exception ex)
        {
            var owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
            MessageBoxWindow.Show(owner, $"打开备份目录失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    ///     执行数据完整性校验
    /// </summary>
    [RelayCommand]
    private async Task VerifyIntegrity()
    {
        var owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);

        try
        {
            var result = await _maintenanceService.VerifyIntegrityAsync();

            if (result.Success)
                MessageBoxWindow.Show(owner, $"✅ {result.Message}\n\n详细信息:\n{result.Details}", "校验通过");
            else
                MessageBoxWindow.Show(owner, $"❌ {result.Message}\n\n详细信息:\n{result.Details}", "校验失败",
                    MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            MessageBoxWindow.Show(owner, $"执行完整性校验时发生错误:\n{ex.Message}", "错误", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    /// <summary>
    ///     执行碎片整理
    /// </summary>
    [RelayCommand]
    private async Task Defragment()
    {
        var owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);

        try
        {
            // 先获取统计信息
            var stats = await _maintenanceService.GetDatabaseStatsAsync();
            if (stats.Success)
            {
                var confirmMsg = "即将执行数据库碎片整理。\n\n";
                confirmMsg += "当前数据库信息:\n";
                confirmMsg += $"- 表数量: {stats.TableCount}\n";
                confirmMsg += $"- 索引数量: {stats.IndexCount}\n";
                confirmMsg += $"- 数据页数: {stats.PageCount}\n";
                confirmMsg += $"- 空闲页数: {stats.FreePages} ({stats.FormattedFragmentationRatio})\n\n";
                confirmMsg += "碎片整理可能需要一些时间，期间数据库将被锁定。\n\n是否继续？";

                var confirmResult = MessageBoxWindow.Show(owner, confirmMsg, "确认碎片整理", MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (confirmResult != MessageBoxResult.Yes) return;
            }

            // 执行碎片整理
            var result = await _maintenanceService.DefragmentAsync();

            if (result.Success)
            {
                var successMsg = "✅ 碎片整理完成！\n\n";
                successMsg += $"原大小: {result.FormattedOriginalSize}\n";
                successMsg += $"新大小: {result.FormattedNewSize}\n";
                successMsg += $"减少: {result.FormattedReducedSize}\n";
                successMsg += $"耗时: {result.Duration.TotalSeconds:F1} 秒";

                MessageBoxWindow.Show(owner, successMsg, "整理完成");

                // 刷新数据库信息显示
                var fileInfo = _databaseInfoService.GetDatabaseFileInfo();
                FileSize = fileInfo.Size;
            }
            else
            {
                MessageBoxWindow.Show(owner, $"❌ {result.Message}", "整理失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBoxWindow.Show(owner, $"执行碎片整理时发生错误:\n{ex.Message}", "错误", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    /// <summary>
    ///     清空全部票务记录
    /// </summary>
    [RelayCommand]
    private async Task ClearAllTickets()
    {
        var owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);

        try
        {
            // 获取当前记录数
            var count = await _dangerousService.GetTrainRideCountAsync();

            if (count == 0)
            {
                MessageBoxWindow.Show(owner, "当前没有票务记录，无需清空。");
                return;
            }

            // 双重确认
            var confirmMsg = "⚠️ 危险操作警告！\n\n";
            confirmMsg += $"即将清空全部 {count} 条票务记录！\n\n";
            confirmMsg += "此操作不可撤销，所有票务数据将被永久删除！\n\n";
            confirmMsg += "操作前会自动备份数据库。\n\n";
            confirmMsg += "请输入 \"确认清空\" 以继续：";

            // 使用自定义确认对话框
            var firstConfirm = MessageBoxWindow.Show(
                owner,
                confirmMsg,
                "危险操作确认",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (firstConfirm != MessageBoxResult.Yes) return;

            // 第二次确认
            var secondConfirm = MessageBoxWindow.Show(
                owner,
                "最后确认：您确定要清空所有票务记录吗？\n\n此操作不可恢复！",
                "最终确认",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (secondConfirm != MessageBoxResult.Yes) return;

            // 执行清空
            var result = await _dangerousService.ClearAllTrainRidesAsync();

            if (result.Success)
            {
                var successMsg = $"✅ {result.Message}\n\n";
                if (result.HasBackup) successMsg += $"已自动备份到:\n{result.BackupPath}\n\n";
                successMsg += "票务记录已清空。";

                MessageBoxWindow.Show(owner, successMsg, "清空完成");

                // 刷新显示
                TicketCount = 0;
                var fileInfo = _databaseInfoService.GetDatabaseFileInfo();
                FileSize = fileInfo.Size;
            }
            else
            {
                MessageBoxWindow.Show(owner, $"❌ {result.Message}", "清空失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBoxWindow.Show(owner, $"清空过程中发生错误:\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    ///     重置数据库
    /// </summary>
    [RelayCommand]
    private async Task ResetDatabase()
    {
        var owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);

        try
        {
            // 双重确认
            var confirmMsg = "⚠️ 极度危险操作警告！\n\n";
            confirmMsg += "即将重置数据库到初始状态！\n\n";
            confirmMsg += "此操作将：\n";
            confirmMsg += "- 删除所有票务记录\n";
            confirmMsg += "- 删除所有车站信息\n";
            confirmMsg += "- 删除所有日志记录\n";
            confirmMsg += "- 重新创建空的数据库表\n\n";
            confirmMsg += "此操作不可撤销！所有数据将被永久删除！\n\n";
            confirmMsg += "操作前会自动备份数据库。\n\n";
            confirmMsg += "是否继续？";

            var firstConfirm = MessageBoxWindow.Show(
                owner,
                confirmMsg,
                "极度危险操作确认",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (firstConfirm != MessageBoxResult.Yes) return;

            // 第二次确认
            var secondConfirm = MessageBoxWindow.Show(
                owner,
                "最后确认：您确定要重置整个数据库吗？\n\n这将删除所有数据且不可恢复！",
                "最终确认",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (secondConfirm != MessageBoxResult.Yes) return;

            // 执行重置
            var result = await _dangerousService.ResetDatabaseAsync();

            if (result.Success)
            {
                var successMsg = $"✅ {result.Message}\n\n";
                if (result.HasBackup) successMsg += $"已自动备份到:\n{result.BackupPath}\n\n";
                successMsg += "点击确定后将自动重启程序。";

                MessageBoxWindow.Show(owner, successMsg, "重置完成");

                // 自动重启
                RestartApplication();
            }
            else
            {
                MessageBoxWindow.Show(owner, $"❌ {result.Message}", "重置失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBoxWindow.Show(owner, $"重置过程中发生错误:\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    ///     导出表结构SQL
    /// </summary>
    [RelayCommand]
    private async Task ExportSchema()
    {
        var owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);

        try
        {
            // 选择保存路径
            var dialog = new SaveFileDialog
            {
                Title = "导出数据库表结构SQL",
                FileName = $"guipiao_schema_{DateTime.Now:yyyyMMdd_HHmmss}.sql",
                DefaultExt = ".sql",
                Filter = "SQL文件|*.sql|所有文件|*.*",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (dialog.ShowDialog() != true) return;

            // 执行导出
            var result = await _dangerousService.ExportSchemaSqlAsync(dialog.FileName);

            if (result.Success)
            {
                var successMsg = $"✅ {result.Message}\n\n";
                successMsg += "是否打开文件所在目录？";

                var openDirResult = MessageBoxWindow.Show(
                    owner,
                    successMsg,
                    "导出成功",
                    MessageBoxButton.YesNo
                );

                if (openDirResult == MessageBoxResult.Yes && !string.IsNullOrEmpty(result.SavedPath))
                {
                    var directory = Path.GetDirectoryName(result.SavedPath);
                    if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = directory,
                            UseShellExecute = true,
                            Verb = "open"
                        });
                }
            }
            else
            {
                MessageBoxWindow.Show(owner, $"❌ {result.Message}", "导出失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBoxWindow.Show(owner, $"导出过程中发生错误:\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    ///     保存设置
    /// </summary>
    [RelayCommand]
    private async Task SaveSettings()
    {
        await SaveSettingsInternalAsync(true);
    }

    /// <summary>
    ///     保存设置内部实现
    /// </summary>
    private async Task SaveSettingsInternalAsync(bool showMessage)
    {
        // 获取设置窗口作为父窗口
        var settingsWindow = Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(w => w.DataContext is SettingsViewModel);

        // 校验最大备份数量
        if (MaxBackupCount < 1 || MaxBackupCount > 100)
        {
            MessageBoxWindow.Show(settingsWindow, "最大备份数量必须在1-100之间", "校验失败", MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        try
        {
            SaveConfig();

            if (showMessage)
                await Task.Run(() =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBoxWindow.Show(settingsWindow, "设置已保存", "成功");
                    });
                });
        }
        catch (Exception ex)
        {
            await Task.Run(() =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBoxWindow.Show(settingsWindow, $"保存设置失败: {ex.Message}", "错误", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                });
            });
        }
    }

    /// <summary>
    ///     恢复默认设置
    /// </summary>
    [RelayCommand]
    private async Task RestoreDefaults()
    {
        // 获取设置窗口作为父窗口
        var settingsWindow = Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(w => w.DataContext is SettingsViewModel);

        try
        {
            // 恢复默认设置
            AutoBackupEnabled = true;
            BackupTiming = "OnExit";
            MaxBackupCount = 10;
            AutoCompress = true;
            ShowErrorOnFail = true;
            AutoVerifyOnExit = true;
            AutoDefragmentMonthly = true;

            // 保存到配置
            SaveConfig();

            // 重新加载数据库信息
            _ = LoadDatabaseInfoAsync();

            await Task.Run(() =>
            {
                Application.Current.Dispatcher.Invoke(() => { MessageBoxWindow.Show(settingsWindow, "已恢复默认设置"); });
            });
        }
        catch (Exception ex)
        {
            await Task.Run(() =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBoxWindow.Show(settingsWindow, $"恢复默认设置失败: {ex.Message}", "错误", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                });
            });
        }
    }

    /// <summary>
    ///     重启应用程序
    /// </summary>
    private void RestartApplication()
    {
        try
        {
            // 获取当前程序路径
            var executablePath = Process.GetCurrentProcess().MainModule?.FileName
                                 ?? Assembly.GetExecutingAssembly().Location;

            // 启动新实例
            Process.Start(new ProcessStartInfo
            {
                FileName = executablePath,
                UseShellExecute = true
            });

            // 关闭当前程序
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            var owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
            MessageBoxWindow.Show(owner, $"自动重启失败: {ex.Message}\n请手动重启程序。", "重启失败", MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    /// <summary>
    ///     重新加载设置（放弃更改）
    /// </summary>
    public void ReloadSettings()
    {
        LoadSettings();
    }

    /// <summary>
    ///     保存设置（接口实现）
    /// </summary>
    async Task ISettingsViewModel.SaveSettingsAsync(bool showMessage)
    {
        await SaveSettingsInternalAsync(showMessage);
    }

    #endregion
}