using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GuiPiao.DataAccess;
using GuiPiao.Messages;
using GuiPiao.Model;
using GuiPiao.Services;
using GuiPiao.View;
using GuiPiao.Views;
using Microsoft.Win32;

namespace GuiPiao.ViewModel;

public partial class MenuViewModel : ObservableObject
{
    private readonly DatabaseBackupService _backupService;
    private readonly DatabaseDangerousService _dangerousService;
    private readonly DatabaseInfoService _databaseInfoService;
    private readonly IncrementalBackupService _incrementalBackupService;
    private readonly LogService _logService;
    private readonly DatabaseMaintenanceService _maintenanceService;
    private readonly DatabaseRestoreService _restoreService;
    private readonly TicketTagRepository _ticketTagRepository;
    private readonly TrainTicketService _trainTicketService;

    public MenuViewModel()
    {
        _ticketTagRepository = new TicketTagRepository();
        _trainTicketService = new TrainTicketService();
        _backupService = new DatabaseBackupService();
        _restoreService = new DatabaseRestoreService();
        _maintenanceService = new DatabaseMaintenanceService();
        _dangerousService = new DatabaseDangerousService();
        _databaseInfoService = new DatabaseInfoService();
        _incrementalBackupService = new IncrementalBackupService();
        _logService = ServiceManager.Instance.LogService;
    }

    [RelayCommand]
    public async Task StorageMenuCommand(string action)
    {
        var owner = Application.Current.MainWindow;

        switch (action)
        {
            case "Import":
                await ImportFromExcelOrCsvAsync();
                break;

            case "BackupFull":
                await FullBackupDatabaseAsync();
                break;

            case "BackupIncremental":
                await IncrementalBackupDatabaseAsync();
                break;

            case "Restore":
                await RestoreDatabaseAsync();
                break;

            case "OpenStorageDir":
                OpenStorageDirectory();
                break;

            case "VerifyData":
                await VerifyDataIntegrityAsync();
                break;

            case "ClearAllTickets":
                await ClearAllTicketsAsync();
                break;

            case "Exit":
                ExitApplication();
                break;

            default:
                MessageBoxWindow.Show(owner, $"存储菜单 - {action}");
                break;
        }
    }

    #region 数据导入

    private async Task ImportFromExcelOrCsvAsync()
    {
        var owner = Application.Current.MainWindow;

        try
        {
            var dialog = new OpenFileDialog
            {
                Title = "选择要导入的文件",
                Filter = "Excel文件|*.xlsx;*.xls|CSV文件|*.csv|所有文件|*.*",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (dialog.ShowDialog() != true) return;

            var filePath = dialog.FileName;
            var extension = Path.GetExtension(filePath).ToLower();

            var importedCount = 0;

            if (extension == ".csv")
            {
                importedCount = await _trainTicketService.ImportFromCsvAsync(filePath);
            }
            else if (extension == ".xlsx" || extension == ".xls")
            {
                MessageBoxWindow.Show(owner, "Excel导入功能即将推出，请先将Excel另存为CSV格式后导入。");
                return;
            }
            else
            {
                MessageBoxWindow.Show(owner, "不支持的文件格式，请选择Excel或CSV文件。", "提示", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (importedCount > 0)
            {
                MessageBoxWindow.Show(owner, $"成功导入 {importedCount} 条票务记录！", "导入成功");
                _logService?.Info("MenuViewModel", $"从 {filePath} 导入 {importedCount} 条记录");

                // 发送刷新消息
                WeakReferenceMessenger.Default.Send(new RefreshTripListMessage());
            }
            else
            {
                MessageBoxWindow.Show(owner, "导入失败或未找到有效数据，请检查文件格式。", "导入失败", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBoxWindow.Show(owner, $"导入过程中发生错误:\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            _logService?.Error("MenuViewModel", $"导入失败: {ex.Message}");
        }
    }

    #endregion

    #region 数据完整性校验

    private async Task VerifyDataIntegrityAsync()
    {
        var owner = Application.Current.MainWindow;
        MessageBoxWindow? progressWindow = null;

        try
        {
            // 显示进度对话框
            progressWindow = MessageBoxWindow.ShowProgress("正在执行数据完整性校验，请稍候...", "数据校验中");

            // 等待窗口完全渲染（100ms）
            await Task.Delay(100);

            var result = await _maintenanceService.VerifyIntegrityAsync();

            // 关闭进度对话框
            progressWindow?.Close();
            progressWindow = null;

            if (result.Success)
                MessageBoxWindow.Show(owner, $"✅ {result.Message}\n\n详细信息:\n{result.Details}", "校验通过");
            else
                MessageBoxWindow.Show(owner, $"❌ {result.Message}\n\n详细信息:\n{result.Details}", "校验失败",
                    MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            // 确保关闭进度对话框
            progressWindow?.Close();
            progressWindow = null;

            MessageBoxWindow.Show(owner, $"执行完整性校验时发生错误:\n{ex.Message}", "错误", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    #endregion

    #region 清空票务记录

    private async Task ClearAllTicketsAsync()
    {
        var owner = Application.Current.MainWindow;

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
            confirmMsg += "是否继续？";

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
                _logService?.Info("MenuViewModel", "清空所有票务记录");

                // 发送刷新消息
                WeakReferenceMessenger.Default.Send(new RefreshTripListMessage());
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

    #endregion

    #region 退出应用程序

    private void ExitApplication()
    {
        var owner = Application.Current.MainWindow;

        var result = MessageBoxWindow.Show(
            owner,
            "确定要退出程序吗？",
            "确认退出",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question
        );

        if (result == MessageBoxResult.Yes)
        {
            _logService?.Info("MenuViewModel", "用户退出应用程序");
            Application.Current.Shutdown();
        }
    }

    #endregion

    #region 辅助方法

    private void RestartApplication()
    {
        try
        {
            var executablePath = Process.GetCurrentProcess().MainModule?.FileName
                                 ?? Assembly.GetExecutingAssembly().Location;

            Process.Start(new ProcessStartInfo
            {
                FileName = executablePath,
                UseShellExecute = true
            });

            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            var owner = Application.Current.MainWindow;
            MessageBoxWindow.Show(owner, $"自动重启失败: {ex.Message}\n请手动重启程序。", "重启失败", MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    #endregion

    [RelayCommand]
    public void TicketMenuCommand(string action)
    {
        switch (action)
        {
            case "BatchUpdateStatus":
                // 批量修改状态功能已在 MainViewModel 中直接处理
                break;
            case "BatchUpdateSeat":
                MessageBoxWindow.Show(Application.Current.MainWindow, "批量更新席别功能即将推出");
                break;
            case "BatchDelete":
                MessageBoxWindow.Show(Application.Current.MainWindow, "批量删除功能即将推出");
                break;
            case "NewTag":
            {
                var newTagDialog = new TagEditWindow(null);
                newTagDialog.Owner = Application.Current.MainWindow;
                newTagDialog.ShowDialog();
            }
                break;
            case "ManageTags":
            {
                var manageDialog = new TagManagerWindow();
                manageDialog.Owner = Application.Current.MainWindow;
                manageDialog.ShowDialog();
            }
                break;
            default:
                MessageBoxWindow.Show(Application.Current.MainWindow, $"票务菜单 - {action}");
                break;
        }
    }

    [RelayCommand]
    public async Task TripMenuCommandAsync(string action)
    {
        switch (action)
        {
            // ========== 按时间筛选 ==========
            case "FilterThisYear":
                ApplyTimeFilter("今年");
                break;

            case "FilterLastYear":
                ApplyTimeFilter("去年");
                break;

            case "FilterCustomDate":
                await ShowCustomDateFilterAsync();
                break;

            case "FilterUpcoming":
                ApplyUpcomingFilter();
                break;

            // ========== 按车次/站点筛选 ==========
            case "FilterTrainStation":
                await ShowTrainStationFilterAsync();
                break;

            // ========== 按标签筛选 ==========
            case "FilterByTag":
                await ShowTagFilterAsync();
                break;

            // ========== 取消筛选 ==========
            case "ClearFilter":
                ClearFilter();
                break;

            // ========== 仪表盘统计 ==========
            case "StatsConfig":
                OpenStatisticsConfig();
                break;

            case "RefreshStats":
                await RefreshStatisticsAsync();
                break;

            case "ExportChart":
                await ExportChartAsync();
                break;

            // ========== 视图配置 ==========
            case "ViewList":
                SwitchToListView();
                break;

            case "ViewCard":
                SwitchToCardView();
                break;

            case "ViewCustomColumns":
                ShowColumnCustomization();
                break;

            case "RefreshList":
                await RefreshTripListAsync();
                break;

            default:
                MessageBoxWindow.Show(Application.Current.MainWindow, $"行程管理菜单 - {action}");
                break;
        }
    }

    #region 标签筛选

    /// <summary>
    ///     显示标签筛选对话框
    /// </summary>
    private async Task ShowTagFilterAsync()
    {
        try
        {
            var tags = await _ticketTagRepository.GetAllTagsAsync();
            if (!tags.Any())
            {
                MessageBoxWindow.Show(Application.Current.MainWindow, "暂无标签，请先创建标签");
                return;
            }

            // 创建标签选择对话框
            var tagNames = string.Join("\n", tags.Select(t => $"• {t.Name}"));
            var dialog = new InputDialogWindow("按标签筛选", $"可用标签：\n{tagNames}\n\n请输入要筛选的标签名称：");
            dialog.Owner = Application.Current.MainWindow;

            if (dialog.ShowDialog() == true)
            {
                var tagName = dialog.InputText?.Trim();
                if (!string.IsNullOrWhiteSpace(tagName))
                {
                    var selectedTag =
                        tags.FirstOrDefault(t => t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase));
                    if (selectedTag != null)
                    {
                        var criteria = new AdvancedSearchCriteria
                        {
                            TagId = selectedTag.Id,
                            TagName = selectedTag.Name
                        };

                        WeakReferenceMessenger.Default.Send(new AdvancedSearchMessage(criteria));
                        WeakReferenceMessenger.Default.Send(new StatusMessageMessage($"已应用标签筛选：{selectedTag.Name}"));
                    }
                    else
                    {
                        MessageBoxWindow.Show(Application.Current.MainWindow, $"未找到标签：{tagName}", "提示",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MessageBoxWindow.Show(Application.Current.MainWindow, $"获取标签失败：{ex.Message}", "错误", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    #endregion

    [RelayCommand]
    public async Task ToolsMenuCommandAsync(string action)
    {
        var owner = Application.Current.MainWindow;

        switch (action)
        {
            case "DataVerify":
                // 直接调用数据完整性校验功能
                await VerifyDataIntegrityAsync();
                break;

            case "DbCompact":
                // 数据库碎片整理
                await CompactDatabaseAsync();
                break;

            case "DataClean":
                // 数据清理
                await ShowDataCleanDialogAsync();
                break;

            default:
                MessageBoxWindow.Show(owner, $"工具集菜单 - {action}\n\n该功能正在开发中，敬请期待！");
                break;
        }
    }

    #region 数据库碎片整理

    private async Task CompactDatabaseAsync()
    {
        var owner = Application.Current.MainWindow;
        MessageBoxWindow? progressWindow = null;

        try
        {
            // 显示进度对话框
            progressWindow = MessageBoxWindow.ShowProgress("正在整理数据库碎片，请稍候...", "数据库优化中");
            await Task.Delay(100);

            // 执行碎片整理
            var result = await _maintenanceService.DefragmentAsync();

            // 关闭进度对话框
            progressWindow?.Close();
            progressWindow = null;

            if (result.Success)
            {
                MessageBoxWindow.Show(owner, $"✅ 数据库碎片整理完成！\n\n{result.Message}", "优化完成");
                _logService?.Info("MenuViewModel", "数据库碎片整理完成");
            }
            else
            {
                MessageBoxWindow.Show(owner, $"❌ 数据库碎片整理失败\n\n{result.Message}", "优化失败", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                _logService?.Error("MenuViewModel", $"数据库碎片整理失败: {result.Message}");
            }
        }
        catch (Exception ex)
        {
            progressWindow?.Close();
            MessageBoxWindow.Show(owner, $"整理数据库碎片时发生错误:\n{ex.Message}", "错误", MessageBoxButton.OK,
                MessageBoxImage.Error);
            _logService?.Error("MenuViewModel", $"数据库碎片整理异常: {ex.Message}");
        }
    }

    #endregion

    #region 数据清理

    private async Task ShowDataCleanDialogAsync()
    {
        var owner = Application.Current.MainWindow;

        // 显示功能说明对话框
        var result = MessageBoxWindow.Show(
            owner,
            "数据清理功能可以帮助您：\n\n" +
            "• 清理已删除车票的残留数据\n" +
            "• 清理过期的临时文件\n" +
            "• 优化数据库性能\n\n" +
            "该功能即将推出，敬请期待！",
            "数据清理"
        );
    }

    #endregion

    [RelayCommand]
    public void ConfigMenuCommand(string action)
    {
        MessageBoxWindow.Show(Application.Current.MainWindow, $"配置菜单 - {action}");
    }

    [RelayCommand]
    public void HelpMenuCommand(string action)
    {
        var owner = Application.Current.MainWindow;

        switch (action)
        {
            case "HelpDoc":
                // 帮助文档
                MessageBoxWindow.Show(
                    owner,
                    "📖 帮助文档\n\n" +
                    "火车票管理系统使用指南：\n\n" +
                    "【基本操作】\n" +
                    "• 新增票务：点击「新增」按钮或使用快捷键 Ctrl+N\n" +
                    "• OCR识别：点击「OCR识别」或使用快捷键 Ctrl+O\n" +
                    "• 编辑票务：双击行程列表中的记录或选中后按 Ctrl+E\n" +
                    "• 删除票务：选中记录后按 Delete 键\n\n" +
                    "【数据管理】\n" +
                    "• 数据导入：支持从CSV文件导入票务数据\n" +
                    "• 数据备份：支持全量备份和增量备份\n" +
                    "• 数据恢复：可从备份文件恢复数据库\n\n" +
                    "【快捷键】\n" +
                    "• Ctrl+N - 新增票务\n" +
                    "• Ctrl+O - OCR识别\n" +
                    "• Ctrl+E - 编辑选中票务\n" +
                    "• Ctrl+P - 票面预览\n" +
                    "• Ctrl+M - 车票地图\n" +
                    "• Ctrl+L - 日志管理\n" +
                    "• Ctrl+, - 系统设置\n" +
                    "• F5 - 刷新数据\n" +
                    "• Delete - 删除选中票务\n\n" +
                    "详细文档正在编写中，敬请期待！",
                    "帮助文档");
                break;

            case "Manual":
                // 使用手册
                MessageBoxWindow.Show(
                    owner,
                    "📚 使用手册\n\n" +
                    "【快速入门】\n" +
                    "1. 添加车票：点击快捷功能区的「🆕」按钮\n" +
                    "2. 查看统计：在仪表盘区域查看行程统计图表\n" +
                    "3. 筛选行程：使用左侧高级检索区按条件筛选\n" +
                    "4. 导出数据：在行程列表底部点击「导出」按钮\n\n" +
                    "【高级功能】\n" +
                    "• 批量操作：在「票务」菜单中选择批量修改状态、标签或删除\n" +
                    "• 标签管理：为车票添加自定义标签便于分类管理\n" +
                    "• 地图视图：在「行程管理」菜单中打开车票地图\n" +
                    "• 数据校验：定期使用「数据完整性校验」确保数据安全\n\n" +
                    "完整手册正在编写中，敬请期待！",
                    "使用手册");
                break;

            case "About":
                // 关于系统
                ShowAboutDialog();
                break;

            case "CheckUpdate":
                // 检查更新
                CheckForUpdates();
                break;

            default:
                MessageBoxWindow.Show(owner, $"帮助与信息菜单 - {action}\n\n该功能正在开发中，敬请期待！");
                break;
        }
    }

    #region 关于对话框

    private void ShowAboutDialog()
    {
        var owner = Application.Current.MainWindow;
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        var versionString = version != null ? $"v{version.Major}.{version.Minor}.{version.Build}" : "v1.0.0";

        MessageBoxWindow.Show(
            owner,
            $"🚄 火车票管理系统 {versionString}\n\n" +
            "一款专业的火车票务管理软件\n\n" +
            "【主要功能】\n" +
            "• 火车票务记录管理\n" +
            "• OCR智能识别车票信息\n" +
            "• 数据统计与可视化\n" +
            "• 数据备份与恢复\n" +
            "• 标签分类管理\n\n" +
            "© 2024 GuiPiao Team\n" +
            "保留所有权利",
            "关于系统");
    }

    #endregion

    #region 检查更新

    private void CheckForUpdates()
    {
        var owner = Application.Current.MainWindow;

        // 这里可以实现实际的更新检查逻辑
        // 目前显示提示信息
        MessageBoxWindow.Show(
            owner,
            "🔄 检查更新\n\n" +
            "当前版本：v1.0.0\n" +
            "最新版本：v1.0.0\n\n" +
            "您当前使用的是最新版本！\n\n" +
            "自动更新功能即将推出，敬请期待！",
            "检查更新");
    }

    #endregion

    [RelayCommand]
    public void OpenLogManager()
    {
        var logWindow = new LogManagerWindow
        {
            // 不设置 Owner，避免最小化时影响主窗口
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };

        WindowStateManager.Instance.RegisterWindow(LastPageOption.LogManager, logWindow);
        logWindow.Show();
    }

    [RelayCommand]
    public void OpenLogSettings()
    {
        var settingsWindow = new SettingsWindow(SettingsPageType.Log)
        {
            Owner = Application.Current.MainWindow
        };
        settingsWindow.ShowDialog();
    }

    [RelayCommand]
    public void OpenSettings(string? pageName = null)
    {
        var settingsWindow = string.IsNullOrEmpty(pageName)
            ? new SettingsWindow()
            : new SettingsWindow(Enum.Parse<SettingsPageType>(pageName));
        settingsWindow.Owner = Application.Current.MainWindow;
        settingsWindow.ShowDialog();
    }

    #region 数据库备份与恢复

    private async Task FullBackupDatabaseAsync()
    {
        var owner = Application.Current.MainWindow;

        try
        {
            var backupPath = await Task.Run(() => _backupService.AutoBackup());

            if (!string.IsNullOrEmpty(backupPath))
            {
                MessageBoxWindow.Show(owner, $"全量备份成功！\n\n备份文件: {backupPath}", "备份成功");
                _logService?.Info("MenuViewModel", $"全量备份完成: {backupPath}");
            }
            else
            {
                MessageBoxWindow.Show(owner, "备份失败，请检查日志了解详情。", "备份失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBoxWindow.Show(owner, $"备份失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            _logService?.Error("MenuViewModel", $"全量备份失败: {ex.Message}");
        }
    }

    private async Task IncrementalBackupDatabaseAsync()
    {
        var owner = Application.Current.MainWindow;
        MessageBoxWindow? progressWindow = null;

        try
        {
            // 显示进度对话框
            progressWindow = MessageBoxWindow.ShowProgress("正在执行增量备份，请稍候...", "增量备份中");

            // 等待窗口渲染
            await Task.Delay(100);

            var result = await _incrementalBackupService.PerformIncrementalBackupAsync();

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
                    _logService?.Info("MenuViewModel", $"增量备份完成: {result.BackupPath}, 记录数: {result.RecordCount}");
                }
            }
            else
            {
                MessageBoxWindow.Show(owner, result.Message, "增量备份失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            // 确保关闭进度对话框
            progressWindow?.Close();
            progressWindow = null;

            MessageBoxWindow.Show(owner, $"增量备份时发生错误:\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            _logService?.Error("MenuViewModel", $"增量备份失败: {ex.Message}");
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

    private async Task RestoreDatabaseAsync()
    {
        var owner = Application.Current.MainWindow;

        try
        {
            var dialog = new OpenFileDialog
            {
                Title = "选择备份文件",
                Filter = "SQLite数据库文件|*.db|所有文件|*.*",
                InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "GuiPiao", "Backups")
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
                _logService?.Info("MenuViewModel", $"数据库恢复完成: {backupPath}");

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
            _logService?.Error("MenuViewModel", $"数据库恢复失败: {ex.Message}");
        }
    }

    private void OpenStorageDirectory()
    {
        try
        {
            var success = _databaseInfoService.OpenDatabaseDirectory();
            if (!success)
            {
                var owner = Application.Current.MainWindow;
                MessageBoxWindow.Show(owner, "数据库文件不存在，无法打开目录。", "文件不存在", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            var owner = Application.Current.MainWindow;
            MessageBoxWindow.Show(owner, $"打开目录失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    #region 时间筛选

    /// <summary>
    ///     应用时间范围筛选
    /// </summary>
    private void ApplyTimeFilter(string dateRange)
    {
        var criteria = new AdvancedSearchCriteria
        {
            DateRange = dateRange
        };

        WeakReferenceMessenger.Default.Send(new AdvancedSearchMessage(criteria));
        WeakReferenceMessenger.Default.Send(new StatusMessageMessage($"已应用筛选：{dateRange}"));
    }

    /// <summary>
    ///     显示自定义日期筛选对话框
    /// </summary>
    private async Task ShowCustomDateFilterAsync()
    {
        var dialog = new CustomTimeRangeWindow();
        dialog.Owner = Application.Current.MainWindow;

        if (dialog.ShowDialog() == true)
            if (dialog.SelectedStartDate.HasValue && dialog.SelectedEndDate.HasValue)
            {
                var criteria = new AdvancedSearchCriteria
                {
                    StartDate = dialog.SelectedStartDate.Value.ToString("yyyy-MM-dd"),
                    EndDate = dialog.SelectedEndDate.Value.ToString("yyyy-MM-dd")
                };

                WeakReferenceMessenger.Default.Send(new AdvancedSearchMessage(criteria));
                WeakReferenceMessenger.Default.Send(new StatusMessageMessage(
                    $"已应用自定义时间筛选：{dialog.SelectedStartDate.Value:yyyy-MM-dd} 至 {dialog.SelectedEndDate.Value:yyyy-MM-dd}"));
            }
    }

    /// <summary>
    ///     应用未出行筛选
    /// </summary>
    private void ApplyUpcomingFilter()
    {
        var criteria = new AdvancedSearchCriteria
        {
            Status = "未出行"
        };

        WeakReferenceMessenger.Default.Send(new AdvancedSearchMessage(criteria));
        WeakReferenceMessenger.Default.Send(new StatusMessageMessage("已应用筛选：未出行行程"));
    }

    #endregion

    #region 车次/站点筛选

    /// <summary>
    ///     显示车次/站点筛选对话框
    /// </summary>
    private async Task ShowTrainStationFilterAsync()
    {
        // 使用输入对话框获取车次或站点信息
        var trainNoDialog = new InputDialogWindow("按车次/站点筛选", "请输入车次号、出发站或到达站（支持模糊匹配）：");
        trainNoDialog.Owner = Application.Current.MainWindow;

        if (trainNoDialog.ShowDialog() == true)
        {
            var keyword = trainNoDialog.InputText?.Trim();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var criteria = new AdvancedSearchCriteria();

                // 判断输入类型：如果是纯数字或字母数字组合，认为是车次
                if (Regex.IsMatch(keyword, @"^[A-Za-z]?\d+$"))
                {
                    criteria.TrainNo = keyword;
                }
                else
                {
                    // 否则同时匹配出发站和到达站
                    criteria.DepartStation = keyword;
                    criteria.ArriveStation = keyword;
                }

                WeakReferenceMessenger.Default.Send(new AdvancedSearchMessage(criteria));
                WeakReferenceMessenger.Default.Send(new StatusMessageMessage($"已应用筛选：{keyword}"));
            }
        }
    }

    /// <summary>
    ///     取消筛选，恢复默认列表
    /// </summary>
    private void ClearFilter()
    {
        // 发送清空筛选的消息
        WeakReferenceMessenger.Default.Send(new AdvancedSearchMessage());
        WeakReferenceMessenger.Default.Send(new StatusMessageMessage("已取消筛选，恢复默认列表"));
    }

    #endregion

    #region 仪表盘统计

    private void OpenStatisticsConfig()
    {
        WeakReferenceMessenger.Default.Send(new StatusMessageMessage("正在打开统计配置..."));
        // 发送消息通知 DashboardViewModel 打开统计配置
        WeakReferenceMessenger.Default.Send(new OpenStatisticsConfigMessage());
    }

    private async Task RefreshStatisticsAsync()
    {
        WeakReferenceMessenger.Default.Send(new StatusMessageMessage("正在刷新统计数据..."));
        // 发送消息通知 DashboardViewModel 刷新统计
        WeakReferenceMessenger.Default.Send(new RefreshStatisticsMessage());
    }

    private async Task ExportChartAsync()
    {
        MessageBoxWindow.Show(Application.Current.MainWindow, "导出统计图表功能即将推出");
    }

    #endregion

    #region 视图配置

    private void SwitchToListView()
    {
        WeakReferenceMessenger.Default.Send(new StatusMessageMessage("已切换到专业列表视图"));
        // 发送消息通知切换视图
        WeakReferenceMessenger.Default.Send(new SwitchViewMessage(ViewType.List));
    }

    private void SwitchToCardView()
    {
        MessageBoxWindow.Show(Application.Current.MainWindow, "简洁卡片视图功能即将推出");
    }

    private void ShowColumnCustomization()
    {
        // 获取当前列配置
        var uiSettingsService = new UISettingsService();
        var columnConfigs = uiSettingsService.Config.DataGridColumns;

        if (columnConfigs == null || columnConfigs.Count == 0)
        {
            MessageBoxWindow.Show(Application.Current.MainWindow, "暂无列配置数据");
            return;
        }

        var dialog = new ColumnCustomizationDialog(columnConfigs);
        dialog.Owner = Application.Current.MainWindow;
        dialog.ShowDialog();
    }

    private async Task RefreshTripListAsync()
    {
        WeakReferenceMessenger.Default.Send(new StatusMessageMessage("正在刷新行程列表..."));
        // 发送刷新消息给 TripListViewModel
        WeakReferenceMessenger.Default.Send(new RefreshTripListMessage());
    }

    #endregion
}