using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using GuiPiao.Model;
using GuiPiao.View;
using GuiPiao.ViewModel;
using TripItem = GuiPiao.Model.TripItem;

namespace GuiPiao.Services;

/// <summary>
///     将行程渲染为车票预览票面 PNG（红 / 蓝 / 红蓝一起）。
/// </summary>
public class TicketFaceImageExportService
{
    private readonly LogService _logService = new();

    public async Task<ExportResult> ExportAsync(
        string targetPath,
        IReadOnlyList<TripItem> trips,
        ImageTicketColorMode colorMode)
    {
        if (trips == null || trips.Count == 0)
            return new ExportResult { Success = false, Message = "没有可导出的行程" };

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null)
            return new ExportResult { Success = false, Message = "无法访问 UI 线程，票面图片导出失败" };

        if (dispatcher.CheckAccess())
            return ExportCore(targetPath, trips, colorMode);

        return await dispatcher.InvokeAsync(() => ExportCore(targetPath, trips, colorMode));
    }

    private ExportResult ExportCore(
        string targetPath,
        IReadOnlyList<TripItem> trips,
        ImageTicketColorMode colorMode)
    {
        TicketPreviewWindow? window = null;
        try
        {
            var colorFlags = ResolveColorFlags(colorMode);
            var totalFiles = trips.Count * colorFlags.Count;
            var (outputDir, singleFilePath) = ResolveOutputPaths(targetPath, totalFiles);

            // 克隆行程，避免预览窗内编辑影响列表数据
            var clones = trips.Select(CloneTrip).ToList();

            window = new TicketPreviewWindow(clones, TicketPreviewSessionMode.UserTripPreview)
            {
                ShowInTaskbar = false,
                ShowActivated = false,
                WindowStyle = WindowStyle.ToolWindow,
                Opacity = 0,
                AllowsTransparency = false,
                Width = 960,
                Height = 700,
                Left = -32000,
                Top = -32000
            };
            window.Show();
            window.UpdateLayout();

            if (window.DataContext is not TicketPreviewViewModel vm)
                return new ExportResult { Success = false, Message = "票面预览初始化失败" };

            var surface = window.TicketPreviewSurface;
            if (surface == null)
                return new ExportResult { Success = false, Message = "找不到票面画布" };

            var saved = 0;
            var firstPath = string.Empty;

            for (var i = 0; i < vm.PreviewDrafts.Count; i++)
            {
                vm.SelectedDraft = vm.PreviewDrafts[i];
                vm.EncodingText = vm.SelectedDraft?.Source.TicketNumber ?? string.Empty;
                window.UpdateLayout();

                foreach (var isRed in colorFlags)
                {
                    if (vm.IsRedTicket != isRed)
                        vm.IsRedTicket = isRed;
                    window.UpdateLayout();

                    var sx = vm.ScaleX;
                    var sy = vm.ScaleY;
                    try
                    {
                        vm.ScaleX = 1.0;
                        vm.ScaleY = 1.0;
                        window.UpdateLayout();

                        string filePath;
                        if (singleFilePath != null)
                        {
                            filePath = singleFilePath;
                        }
                        else
                        {
                            var baseName = BuildTicketFileBaseName(vm.SelectedDraft!.Source, i);
                            var suffix = isRed ? "红" : "蓝";
                            filePath = Path.Combine(outputDir, $"{baseName}_{suffix}.png");
                        }

                        TicketFacePngRenderer.SavePng(surface, filePath);
                        if (saved == 0) firstPath = filePath;
                        saved++;
                    }
                    finally
                    {
                        vm.ScaleX = sx;
                        vm.ScaleY = sy;
                    }
                }
            }

            var resultPath = totalFiles == 1 ? firstPath : outputDir;
            _logService.Info("TicketFaceImageExportService",
                $"票面图片导出成功: {resultPath}, 文件数: {saved}");

            return new ExportResult
            {
                Success = true,
                FilePath = resultPath,
                RecordCount = trips.Count,
                Message = $"已导出 {saved} 张票面图片"
            };
        }
        catch (Exception ex)
        {
            _logService.Error("TicketFaceImageExportService", $"票面图片导出异常: {ex.Message}");
            return new ExportResult { Success = false, Message = $"图片导出失败: {ex.Message}" };
        }
        finally
        {
            try
            {
                window?.Close();
            }
            catch
            {
                // ignore close errors
            }
        }
    }

    private static List<bool> ResolveColorFlags(ImageTicketColorMode mode) =>
        mode switch
        {
            ImageTicketColorMode.Blue => new List<bool> { false },
            ImageTicketColorMode.Both => new List<bool> { true, false },
            _ => new List<bool> { true }
        };

    /// <summary>
    ///     单文件时返回具体 png 路径；多文件时返回输出目录，singleFilePath 为 null。
    /// </summary>
    private static (string outputDir, string? singleFilePath) ResolveOutputPaths(string targetPath, int totalFiles)
    {
        if (string.IsNullOrWhiteSpace(targetPath))
            throw new ArgumentException("导出路径不能为空", nameof(targetPath));

        if (totalFiles <= 1)
        {
            var file = targetPath;
            if (!file.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                file += ".png";
            var dir = Path.GetDirectoryName(file);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            return (dir ?? ".", file);
        }

        string folder;
        if (Directory.Exists(targetPath) || string.IsNullOrEmpty(Path.GetExtension(targetPath)))
        {
            folder = targetPath;
        }
        else
        {
            var parent = Path.GetDirectoryName(targetPath) ?? ".";
            var name = Path.GetFileNameWithoutExtension(targetPath);
            folder = Path.Combine(parent, name);
        }

        Directory.CreateDirectory(folder);
        return (folder, null);
    }

    private static string BuildTicketFileBaseName(TripItem trip, int index)
    {
        var depart = TicketPreviewDraft.TrimTrailingStation(trip.DepartStation);
        var arrive = TicketPreviewDraft.TrimTrailingStation(trip.ArriveStation);
        var train = string.IsNullOrWhiteSpace(trip.TrainNo) ? $"票{index + 1}" : trip.TrainNo.Trim();
        var name = $"{depart}-{train}-{arrive}";
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        if (string.IsNullOrWhiteSpace(name) || name == "--")
            name = $"票面_{index + 1}";
        return name;
    }

    public static TripItem ToTripItem(TrainRideInfo ride)
    {
        return new TripItem
        {
            Id = ride.Id,
            DatabaseId = ride.Id,
            TicketNumber = ride.TicketNumber ?? "",
            TrainNo = ride.TrainNo ?? "",
            DepartStation = ride.DepartStation ?? "",
            ArriveStation = ride.ArriveStation ?? "",
            DepartStationPinyin = ride.DepartStationPinyin ?? "",
            ArriveStationPinyin = ride.ArriveStationPinyin ?? "",
            DepartDate = ride.DepartDate ?? "",
            DepartTime = ride.DepartTime ?? "",
            ArriveTime = ride.ArriveTime ?? "",
            ArriveDayOffset = ride.ArriveDayOffset,
            CoachNo = ride.CoachNo ?? "",
            SeatNo = ride.SeatNo ?? "",
            SeatType = ride.SeatType ?? "",
            Money = ride.Money.ToString("0.##"),
            CheckInLocation = ride.CheckInLocation ?? "",
            AdditionalInfo = ride.AdditionalInfo ?? "",
            TicketPurpose = ride.TicketPurpose ?? "",
            TicketModificationType = ride.TicketModificationType ?? "",
            Hint = ride.Hint ?? "",
            Status = ride.Status,
            Tags = ride.Tags ?? new List<TicketTag>(),
            TicketType = FormatTicketTypeFlags(ride.TicketTypeFlags),
            PaymentChannel = FormatPaymentChannelFlags(ride.PaymentChannelFlags)
        };
    }

    private static TripItem CloneTrip(TripItem t) =>
        new()
        {
            Id = t.Id,
            DatabaseId = t.DatabaseId,
            TicketNumber = t.TicketNumber ?? "",
            TrainNo = t.TrainNo ?? "",
            DepartStation = t.DepartStation ?? "",
            ArriveStation = t.ArriveStation ?? "",
            DepartStationPinyin = t.DepartStationPinyin ?? "",
            ArriveStationPinyin = t.ArriveStationPinyin ?? "",
            DepartDate = t.DepartDate ?? "",
            DepartTime = t.DepartTime ?? "",
            ArriveTime = t.ArriveTime ?? "",
            ArriveDayOffset = t.ArriveDayOffset,
            CoachNo = t.CoachNo ?? "",
            SeatNo = t.SeatNo ?? "",
            SeatType = t.SeatType ?? "",
            Money = t.Money ?? "",
            CheckInLocation = t.CheckInLocation ?? "",
            AdditionalInfo = t.AdditionalInfo ?? "",
            TicketPurpose = t.TicketPurpose ?? "",
            TicketModificationType = t.TicketModificationType ?? "",
            Hint = t.Hint ?? "",
            Status = t.Status,
            Tags = t.Tags ?? new List<TicketTag>(),
            TicketType = t.TicketType ?? "",
            PaymentChannel = t.PaymentChannel ?? ""
        };

    private static string FormatTicketTypeFlags(int flags)
    {
        if (flags == 0) return string.Empty;
        var types = new List<string>();
        if ((flags & 1) != 0) types.Add("学生票");
        if ((flags & 2) != 0) types.Add("优惠票");
        if ((flags & 4) != 0) types.Add("网络售票");
        if ((flags & 8) != 0) types.Add("儿童票");
        return string.Join(", ", types);
    }

    private static string FormatPaymentChannelFlags(int flags)
    {
        if (flags == 0) return string.Empty;
        var channels = new List<string>();
        if ((flags & 1) != 0) channels.Add("支付宝");
        if ((flags & 2) != 0) channels.Add("微信");
        if ((flags & 4) != 0) channels.Add("农业银行");
        if ((flags & 8) != 0) channels.Add("建设银行");
        if ((flags & 16) != 0) channels.Add("工商银行");
        if ((flags & 32) != 0) channels.Add("交通银行");
        if ((flags & 64) != 0) channels.Add("招商银行");
        if ((flags & 128) != 0) channels.Add("邮储银行");
        if ((flags & 256) != 0) channels.Add("中国银行");
        return string.Join(", ", channels);
    }
}
