using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GuiPiao.Messages;
using GuiPiao.Model;
using GuiPiao.Services;
using GuiPiao.View;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace GuiPiao.ViewModel
{
    public partial class QuickActionsViewModel : ObservableObject
    {
        [RelayCommand]
        public void NewTicketRecordCommand()
        {
            var addTicketWindow = new AddTrainTicketWindow();
            // 不设置 Owner，避免最小化时影响主窗口
            // 使用 Show() 而不是 ShowDialog()，允许用户切换窗口
            addTicketWindow.Show();
        }

        [RelayCommand]
        public void OcrRecognizeTicketCommand()
        {
            ServiceManager.Instance.TesseractService.RecognizeTicket("");
            MessageBoxWindow.Show(Application.Current.MainWindow, "OCR识别车票", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        public void OpenTicketMapCommand()
        {
            System.Diagnostics.Debug.WriteLine("OpenTicketMapCommand 被调用");
            try
            {
                var mapWindow = new MapWindow();

                // 不设置 Owner，避免最小化时影响主窗口
                WindowStateManager.Instance.RegisterWindow(LastPageOption.Map, mapWindow);

                System.Diagnostics.Debug.WriteLine("MapWindow 实例已创建");
                mapWindow.Show();
                System.Diagnostics.Debug.WriteLine("MapWindow Show() 已调用");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"打开车票地图失败：{ex}");
                MessageBoxWindow.Show(Application.Current.MainWindow, $"打开车票地图失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        public void TicketPreviewCommand()
        {
            MessageBoxWindow.Show(Application.Current.MainWindow, "票面预览", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        public async Task BackupRestoreDatabaseCommand()
        {
            var owner = Application.Current.MainWindow;

            // 显示选择对话框，让用户选择备份还是恢复
            var result = MessageBoxWindow.Show(
                owner,
                "请选择要执行的操作：\n\n" +
                "【备份数据库】创建当前数据库的备份文件\n" +
                "【恢复数据库】从备份文件恢复数据\n\n" +
                "注意：恢复操作将覆盖当前所有数据！",
                "备份/恢复数据库",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question,
                yesText: "备份数据库",
                noText: "恢复数据库",
                cancelText: "取消"
            );

            switch (result)
            {
                case MessageBoxResult.Yes:
                    // 执行备份
                    await BackupDatabaseAsync();
                    break;
                case MessageBoxResult.No:
                    // 执行恢复
                    await RestoreDatabaseAsync();
                    break;
                default:
                    // 取消操作
                    break;
            }
        }

        /// <summary>
        /// 备份数据库
        /// </summary>
        private async Task BackupDatabaseAsync()
        {
            var owner = Application.Current.MainWindow;
            var backupService = new DatabaseBackupService();
            var logService = ServiceManager.Instance.LogService;

            // 发送状态栏消息：开始备份
            WeakReferenceMessenger.Default.Send(new StatusMessageMessage("正在备份数据库...", false));

            try
            {
                string? backupPath = await Task.Run(() => backupService.AutoBackup());

                if (!string.IsNullOrEmpty(backupPath))
                {
                    // 发送状态栏消息：备份成功
                    WeakReferenceMessenger.Default.Send(new StatusMessageMessage($"✅ 备份成功: {System.IO.Path.GetFileName(backupPath)}"));
                    MessageBoxWindow.Show(owner, $"✅ 备份成功！\n\n备份文件: {backupPath}", "备份成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    logService?.Info("QuickActionsViewModel", $"数据库备份成功: {backupPath}");
                }
                else
                {
                    // 发送状态栏消息：备份失败
                    WeakReferenceMessenger.Default.Send(new StatusMessageMessage("❌ 备份失败"));
                    MessageBoxWindow.Show(owner, "❌ 备份失败，请检查日志了解详情。", "备份失败", MessageBoxButton.OK, MessageBoxImage.Error);
                    logService?.Error("QuickActionsViewModel", "数据库备份失败");
                }
            }
            catch (Exception ex)
            {
                // 发送状态栏消息：备份异常
                WeakReferenceMessenger.Default.Send(new StatusMessageMessage($"❌ 备份失败: {ex.Message}"));
                MessageBoxWindow.Show(owner, $"❌ 备份失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                logService?.Error("QuickActionsViewModel", $"数据库备份异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 恢复数据库
        /// </summary>
        private async Task RestoreDatabaseAsync()
        {
            var owner = Application.Current.MainWindow;
            var restoreService = new DatabaseRestoreService();
            var logService = ServiceManager.Instance.LogService;

            try
            {
                // 打开文件选择对话框
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "选择备份文件",
                    Filter = "SQLite数据库文件|*.db|所有文件|*.*",
                    InitialDirectory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GuiPiao", "Backups")
                };

                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                string backupPath = dialog.FileName;

                // 验证备份文件
                var validationResult = restoreService.ValidateBackupFile(backupPath);
                if (!validationResult.IsValid)
                {
                    MessageBoxWindow.Show(owner, validationResult.ErrorMessage, "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 显示确认对话框
                string confirmMessage = $"即将从以下备份文件恢复数据库:\n{backupPath}\n";
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

                if (confirmResult != MessageBoxResult.Yes)
                {
                    return;
                }

                // 发送状态栏消息：开始恢复
                WeakReferenceMessenger.Default.Send(new StatusMessageMessage("正在恢复数据库...", false));

                // 执行恢复
                var result = await Task.Run(() => restoreService.RestoreFromBackup(backupPath, backupCurrent: true));

                if (result.IsSuccess)
                {
                    string successMessage = "✅ 数据库恢复成功！\n\n";
                    if (result.HasCurrentBackup)
                    {
                        successMessage += $"恢复前已自动备份当前数据库:\n{result.CurrentBackupPath}\n\n";
                    }
                    successMessage += "点击确定后将自动重启程序以应用更改。";

                    MessageBoxWindow.Show(owner, successMessage, "恢复成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    logService?.Info("QuickActionsViewModel", $"数据库恢复完成: {backupPath}");

                    // 自动重启
                    RestartApplication();
                }
                else
                {
                    MessageBoxWindow.Show(owner, $"❌ {result.ErrorMessage}", "恢复失败", MessageBoxButton.OK, MessageBoxImage.Error);
                    logService?.Error("QuickActionsViewModel", $"数据库恢复失败: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                MessageBoxWindow.Show(owner, $"恢复过程中发生错误:\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                logService?.Error("QuickActionsViewModel", $"数据库恢复异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 重启应用程序
        /// </summary>
        private void RestartApplication()
        {
            try
            {
                string executablePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
                    ?? System.Reflection.Assembly.GetExecutingAssembly().Location;

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = executablePath,
                    UseShellExecute = true
                });

                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                var owner = Application.Current.MainWindow;
                MessageBoxWindow.Show(owner, $"自动重启失败: {ex.Message}\n请手动重启程序。", "重启失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        [RelayCommand]
        public void SystemConfigCommand()
        {
            var settingsWindow = new SettingsWindow();
            // 不设置 Owner，避免最小化时影响主窗口
            settingsWindow.Show();
        }
    }
}
