using System.Windows;
using GuiPiao.Model;
using GuiPiao.Services;

namespace GuiPiao.View;

/// <summary>
///     行程导出：选择格式（及票面颜色）。
/// </summary>
public partial class ExportTripFormatWindow : Window
{
    public ExportTripFormatWindow(ExportFormatOption defaultFormat, ImageTicketColorMode defaultColor)
    {
        InitializeComponent();
        ThemeManager.ApplyThemeToWindow(this);

        SelectedFormat = defaultFormat;
        SelectedTicketColor = defaultColor;

        FormatExcel.IsChecked = defaultFormat == ExportFormatOption.Excel;
        FormatCsv.IsChecked = defaultFormat == ExportFormatOption.Csv;
        FormatPdf.IsChecked = defaultFormat == ExportFormatOption.Pdf;
        FormatImage.IsChecked = defaultFormat == ExportFormatOption.Image;

        ColorRed.IsChecked = defaultColor == ImageTicketColorMode.Red;
        ColorBlue.IsChecked = defaultColor == ImageTicketColorMode.Blue;
        ColorBoth.IsChecked = defaultColor == ImageTicketColorMode.Both;

        UpdateTicketColorVisibility();
    }

    public ExportFormatOption SelectedFormat { get; private set; } = ExportFormatOption.Excel;
    public ImageTicketColorMode SelectedTicketColor { get; private set; } = ImageTicketColorMode.Red;
    public bool Confirmed { get; private set; }

    private void Format_Changed(object sender, RoutedEventArgs e) => UpdateTicketColorVisibility();

    private void UpdateTicketColorVisibility()
    {
        TicketColorPanel.Visibility = FormatImage.IsChecked == true
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (FormatCsv.IsChecked == true)
            SelectedFormat = ExportFormatOption.Csv;
        else if (FormatPdf.IsChecked == true)
            SelectedFormat = ExportFormatOption.Pdf;
        else if (FormatImage.IsChecked == true)
            SelectedFormat = ExportFormatOption.Image;
        else
            SelectedFormat = ExportFormatOption.Excel;

        if (ColorBlue.IsChecked == true)
            SelectedTicketColor = ImageTicketColorMode.Blue;
        else if (ColorBoth.IsChecked == true)
            SelectedTicketColor = ImageTicketColorMode.Both;
        else
            SelectedTicketColor = ImageTicketColorMode.Red;

        Confirmed = true;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Confirmed = false;
        DialogResult = false;
        Close();
    }
}
