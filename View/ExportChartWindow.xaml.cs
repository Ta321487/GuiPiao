using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using CommunityToolkit.Mvvm.ComponentModel;
using GuiPiao.ViewModel;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace GuiPiao.View;

public partial class ExportChartWindow : Window
{
    private readonly List<ChartSelectionItem> _chartItems;
    private readonly Action<ChartSelectionItem, bool> _deselectAllCallback;
    private readonly Action<ChartSelectionItem, bool> _selectAllCallback;

    public ExportChartWindow(List<DashboardChartViewModel> charts,
        Action<ChartSelectionItem, bool> selectAllCallback,
        Action<ChartSelectionItem, bool> deselectAllCallback)
    {
        InitializeComponent();

        _chartItems = charts.Select(c => new ChartSelectionItem(c.Title ?? c.Card?.Name ?? "未命名图表", c)).ToList();
        ChartSelectionItemsControl.ItemsSource = _chartItems;
        _selectAllCallback = selectAllCallback;
        _deselectAllCallback = deselectAllCallback;

        // 设置默认值
        UseDefaultChartNames = true;
        FolderName = "统计图表";
        BaseFileName = "图表";

        // 确保事件处理程序在所有控件加载后附加
        Loaded += (s, e) =>
        {
            FormatComboBox.SelectionChanged += FormatComboBox_SelectionChanged;
            // 初始化时设置正确的状态
            FormatComboBox_SelectionChanged(null, null);
            UseDefaultNamesCheckBox_CheckedChanged(null, null);
        };
    }

    public string SaveFilePath { get; private set; }
    public ExportChartFormat ExportFormat { get; private set; }
    public bool IncludeRawData { get; private set; }
    public bool IncludeChartImage { get; private set; }
    public List<DashboardChartViewModel> SelectedCharts { get; private set; }
    public bool UseDefaultChartNames { get; private set; }
    public string FolderName { get; private set; }
    public string BaseFileName { get; private set; }

    private void FormatComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FormatComboBox == null || IncludeChartImageCheckBox == null || ImageOptionsPanel == null)
            return;

        var selectedItem = FormatComboBox.SelectedItem as ComboBoxItem;
        if (selectedItem != null)
        {
            var format = selectedItem.Tag?.ToString() ?? "Excel";
            // 根据格式动态启用/禁用"包含图表图片"选项
            IncludeChartImageCheckBox.IsEnabled = format is "Pdf" or "Image";
            if (format == "Excel") IncludeChartImageCheckBox.IsChecked = false;
            // 根据格式显示/隐藏图片选项
            ImageOptionsPanel.Visibility = format == "Image"
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }

    private void UseDefaultNamesCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
    {
        if (UseDefaultNamesCheckBox == null || FolderNameTextBox == null || BaseFileNamePanel == null)
            return;

        // 文件夹名称始终可编辑
        FolderNameTextBox.IsEnabled = true;
        // 基础文件名面板：勾选时隐藏，取消勾选时显示
        BaseFileNamePanel.Visibility = UseDefaultNamesCheckBox.IsChecked == true
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private void SelectAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var item in _chartItems)
        {
            _selectAllCallback?.Invoke(item, true);
            item.IsSelected = true;
        }
    }

    private void DeselectAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var item in _chartItems)
        {
            _deselectAllCallback?.Invoke(item, false);
            item.IsSelected = false;
        }
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        var selectedCharts = _chartItems.Where(c => c.IsSelected).ToList();
        if (selectedCharts.Count == 0)
        {
            MessageBoxWindow.Show(this, "请至少选择一个图表进行导出。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (FormatComboBox?.SelectedItem is ComboBoxItem selectedItem)
        {
            var format = selectedItem.Tag?.ToString() ?? "Excel";

            // 确保所有控件可用
            var includeRawData = IncludeRawDataCheckBox?.IsChecked == true;
            var includeChartImage = IncludeChartImageCheckBox?.IsChecked == true;

            if (format == "Image")
            {
                // 图片格式：选择文件夹
                var useDefaultNames = UseDefaultNamesCheckBox?.IsChecked == true;
                var folderName = FolderNameTextBox?.Text.Trim() ?? "统计图表";
                var baseFileName = BaseFileNameTextBox?.Text.Trim() ?? "图表";

                using var dialog = new FolderBrowserDialog
                {
                    Description = "选择保存图表的文件夹",
                    ShowNewFolderButton = true,
                    SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                };

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    SaveFilePath = dialog.SelectedPath;
                    ExportFormat = ExportChartFormat.Image;
                    IncludeRawData = includeRawData;
                    IncludeChartImage = includeChartImage;
                    UseDefaultChartNames = useDefaultNames;
                    FolderName = folderName;
                    BaseFileName = baseFileName;
                    SelectedCharts = selectedCharts.Select(c => c.ChartViewModel).ToList();

                    DialogResult = true;
                    Close();
                }
            }
            else
            {
                // Excel/PDF格式：选择文件
                var filter = format switch
                {
                    "Excel" => "Excel 工作簿|*.xlsx",
                    "Pdf" => "PDF 文档|*.pdf",
                    _ => "所有文件|*.*"
                };

                var dialog = new SaveFileDialog
                {
                    Title = "保存导出文件",
                    Filter = filter,
                    FileName = $"统计报告_{DateTime.Now:yyyyMMdd_HHmmss}{format switch {
                        "Excel" => ".xlsx",
                        "Pdf" => ".pdf",
                        _ => ""
                    }}",
                    DefaultExt = format switch
                    {
                        "Excel" => ".xlsx",
                        "Pdf" => ".pdf",
                        _ => ""
                    },
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                };

                if (dialog.ShowDialog() == true)
                {
                    SaveFilePath = dialog.FileName;
                    ExportFormat = format switch
                    {
                        "Excel" => ExportChartFormat.Excel,
                        "Pdf" => ExportChartFormat.Pdf,
                        _ => ExportChartFormat.Excel
                    };
                    IncludeRawData = includeRawData;
                    IncludeChartImage = includeChartImage;
                    SelectedCharts = selectedCharts.Select(c => c.ChartViewModel).ToList();

                    DialogResult = true;
                    Close();
                }
            }
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    public partial class ChartSelectionItem : ObservableObject
    {
        [ObservableProperty] private bool _isSelected = true;

        public ChartSelectionItem(string chartName, DashboardChartViewModel chartViewModel)
        {
            ChartName = chartName;
            ChartViewModel = chartViewModel;
            IsSelected = true;
        }

        public string ChartName { get; set; }
        public DashboardChartViewModel ChartViewModel { get; set; }
    }
}