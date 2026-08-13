using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GuiPiao.Messages;
using GuiPiao.Model;
using GuiPiao.Services;
using GuiPiao.Utils;
using GuiPiao.View;
using Microsoft.Win32;

namespace GuiPiao.ViewModel;

public partial class QuickActionsViewModel : ObservableObject
{
    /// <summary>可选：当前行程列表选中的数据库 ID，用于 OCR 覆盖编辑。</summary>
    public Func<int?>? GetSelectedTicketDatabaseId { get; set; }

    [RelayCommand]
    public void NewTicketRecordCommand()
    {
        WindowManager.ShowWindow(() => new AddTrainTicketWindow());
    }

    [RelayCommand]
    public async Task OcrRecognizeTicketCommand()
    {
        var owner = Application.Current.MainWindow;
        var ocrWindow = new OcrRecognizeTicketWindow
        {
            Owner = owner
        };

        var confirmed = ocrWindow.ShowDialog() == true;
        if (!confirmed)
            return;

        var drafts = ocrWindow.ResultDrafts;
        if (drafts == null || drafts.Count == 0)
        {
            if (ocrWindow.ResultDraft == null)
                return;
            drafts = new[] { ocrWindow.ResultDraft };
        }

        var preferDirectSave = !new GeneralSettingsService().Config.OcrEditConfirm;
        var selectedId = GetSelectedTicketDatabaseId?.Invoke();

        // 单张 + 有选中行程：可覆盖编辑
        if (drafts.Count == 1 && selectedId is > 0 && !preferDirectSave)
        {
            var choice = MessageBoxWindow.Show(owner,
                "当前已选中一条行程。\n\n【是】覆盖至该行程编辑窗口\n【否】作为新行程打开新增窗口\n【取消】放弃本次导入",
                "OCR 导入",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (choice == MessageBoxResult.Cancel)
                return;

            if (choice == MessageBoxResult.Yes)
            {
                var editWindow = EditTrainTicketWindow.CreateFromImportDraft(selectedId.Value, drafts[0]);
                editWindow.Owner = owner;
                if (!editWindow.IsVisible)
                    editWindow.Show();
                editWindow.Activate();
                return;
            }
        }

        if (preferDirectSave)
        {
            var failed = new List<TicketImportDraft>();
            var saved = 0;
            foreach (var draft in drafts)
            {
                var vm = new AddTrainTicketViewModel();
                vm.InitializeDefaults();
                await vm.ApplyImportDraftAsync(draft);
                var (ok, error) = await vm.TrySaveImportDirectlyAsync();
                if (ok)
                {
                    saved++;
                    continue;
                }

                failed.Add(draft);
                MessageBoxWindow.Show(owner,
                    drafts.Count > 1
                        ? $"第 {saved + failed.Count} 张无法直接保存（{error}）。\n将打开编辑窗口进行核对。"
                        : $"无法直接保存（{error}）。\n将打开编辑窗口进行核对。",
                    "提示",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            if (failed.Count == 0)
            {
                MessageBoxWindow.Show(owner,
                    saved == 1 ? "已直接保存 OCR 识别结果。" : $"已直接保存 {saved} 条行程。",
                    "提示",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var reviewWindow = failed.Count == 1
                ? AddTrainTicketWindow.CreateFromImportDraft(failed[0])
                : AddTrainTicketWindow.CreateFromImportDrafts(failed);
            reviewWindow.Owner = owner;
            reviewWindow.ShowDialog();

            if (saved > 0)
            {
                MessageBoxWindow.Show(owner,
                    $"已直接保存 {saved} 条；另有 {failed.Count} 条已在编辑窗口核对。",
                    "提示",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }

            return;
        }

        var addWindow = drafts.Count == 1
            ? AddTrainTicketWindow.CreateFromImportDraft(drafts[0])
            : AddTrainTicketWindow.CreateFromImportDrafts(drafts.ToList());
        addWindow.Owner = owner;
        addWindow.ShowDialog();
    }

    [RelayCommand]
    public void OpenTicketMapCommand()
    {
        Debug.WriteLine("OpenTicketMapCommand 被调用");
        try
        {
            var mapWindow = WindowManager.ShowWindow(() => new MapWindow());
            WindowStateManager.Instance.RegisterWindow(LastPageOption.Map, mapWindow);
            Debug.WriteLine("MapWindow Show() 已调用");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"打开车票地图失败：{ex}");
            MessageBoxWindow.Show(Application.Current.MainWindow, $"打开车票地图失败：{ex.Message}", "错误", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    public void TicketPreviewCommand()
    {
        MessageBoxWindow.Show(Application.Current.MainWindow, "票面预览");
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
            "备份数据库",
            "恢复数据库",
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
        }
    }

    /// <summary>
    ///     备份数据库
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
            var backupPath = await Task.Run(() => backupService.AutoBackup());

            if (!string.IsNullOrEmpty(backupPath))
            {
                // 发送状态栏消息：备份成功
                WeakReferenceMessenger.Default.Send(
                    new StatusMessageMessage($"备份成功: {Path.GetFileName(backupPath)}"));
                MessageBoxWindow.Show(owner, $"备份成功！\n\n备份文件: {backupPath}", "备份成功");
                logService?.Info("QuickActionsViewModel", $"数据库备份成功: {backupPath}");
            }
            else
            {
                // 发送状态栏消息：备份失败
                WeakReferenceMessenger.Default.Send(new StatusMessageMessage("备份失败"));
                MessageBoxWindow.Show(owner, "备份失败，请检查日志了解详情。", "备份失败", MessageBoxButton.OK, MessageBoxImage.Error);
                logService?.Error("QuickActionsViewModel", "数据库备份失败");
            }
        }
        catch (Exception ex)
        {
            // 发送状态栏消息：备份异常
            WeakReferenceMessenger.Default.Send(new StatusMessageMessage($"备份失败: {ex.Message}"));
            MessageBoxWindow.Show(owner, $"备份失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            logService?.Error("QuickActionsViewModel", $"数据库备份异常: {ex.Message}");
        }
    }

    /// <summary>
    ///     恢复数据库
    /// </summary>
    private async Task RestoreDatabaseAsync()
    {
        var owner = Application.Current.MainWindow;
        var restoreService = new DatabaseRestoreService();
        var logService = ServiceManager.Instance.LogService;

        try
        {
            // 打开文件选择对话框
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
            var validationResult = restoreService.ValidateBackupFile(backupPath);
            if (!validationResult.IsValid)
            {
                MessageBoxWindow.Show(owner, validationResult.ErrorMessage, "验证失败", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // 确认（受「恢复数据库备份时弹出确认」控制）
            var confirmMessage = $"即将从以下备份文件恢复数据库:\n{backupPath}\n";
            confirmMessage += $"文件大小: {validationResult.FormattedFileSize}\n\n";
            confirmMessage += "恢复操作将完全覆盖当前所有数据，且无法撤销！\n\n";
            confirmMessage += "恢复前会自动备份当前数据库。\n\n是否继续？";

            if (!new ConfirmationService().ConfirmRestore(confirmMessage)) return;

            // 发送状态栏消息：开始恢复
            WeakReferenceMessenger.Default.Send(new StatusMessageMessage("正在恢复数据库...", false));

            // 执行恢复
            var result = await Task.Run(() => restoreService.RestoreFromBackup(backupPath));

            if (result.IsSuccess)
            {
                var successMessage = "数据库恢复成功！\n\n";
                if (result.HasCurrentBackup) successMessage += $"恢复前已自动备份当前数据库:\n{result.CurrentBackupPath}\n\n";
                successMessage += "点击确定后将自动重启程序以应用更改。";

                MessageBoxWindow.Show(owner, successMessage, "恢复成功");
                logService?.Info("QuickActionsViewModel", $"数据库恢复完成: {backupPath}");

                // 自动重启
                RestartApplication();
            }
            else
            {
                MessageBoxWindow.Show(owner, $"{result.ErrorMessage}", "恢复失败", MessageBoxButton.OK,
                    MessageBoxImage.Error);
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
    ///     重启应用程序
    /// </summary>
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

    [RelayCommand]
    public void SystemConfigCommand()
    {
        WindowManager.ShowWindow(() => new SettingsWindow());
    }
}