using System.Collections.Generic;
using System.Linq;
using System.Windows;
using GuiPiao.ViewModel.TrainTicketForm;
using TripItemModel = GuiPiao.Model.TripItem;

namespace GuiPiao.View;

/// <summary>
///     批量更新席别窗口
/// </summary>
public partial class BatchUpdateSeatWindow : Window
{
    public BatchUpdateSeatWindow(List<TripItemModel> selectedTickets)
    {
        InitializeComponent();

        EditableTickets = selectedTickets.Where(t => t.Status == 0 || t.Status == 1).ToList();
        SkippedTickets = selectedTickets.Where(t => t.Status == 2 || t.Status == 3).ToList();

        TicketsDataGrid.ItemsSource = EditableTickets;

        if (SkippedTickets.Count > 0)
        {
            SkippedBorder.Visibility = Visibility.Visible;
            var skippedInfo = string.Join(", ",
                SkippedTickets.Select(t => $"{t.TrainNo}({GetStatusDisplay(t.Status)})"));
            SkippedTextBlock.Text = $"以下 {SkippedTickets.Count} 张车票已改签或已退票，将被跳过：{skippedInfo}";
        }

        var options = new OptionsProvider();
        SeatTypeCombo.ItemsSource = options.SeatTypeOptions;
        if (options.SeatTypeOptions.Count > 0)
            SeatTypeCombo.SelectedIndex = 0;
    }

    public List<TripItemModel> EditableTickets { get; }

    public List<TripItemModel> SkippedTickets { get; }

    public string SelectedSeatType { get; private set; } = string.Empty;

    public bool IsConfirmed { get; private set; }

    private static string GetStatusDisplay(int status)
    {
        return status switch
        {
            0 => "未出行",
            1 => "已完成",
            2 => "已改签",
            3 => "已退票",
            _ => "未知"
        };
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (EditableTickets.Count == 0)
        {
            MessageBoxWindow.Show("没有可以更新席别的车票", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            DialogResult = false;
            Close();
            return;
        }

        if (SeatTypeCombo.SelectedItem is not string seatType || string.IsNullOrWhiteSpace(seatType))
        {
            MessageBoxWindow.Show("请选择目标席别", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        SelectedSeatType = seatType;
        IsConfirmed = true;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        IsConfirmed = false;
        DialogResult = false;
        Close();
    }
}
