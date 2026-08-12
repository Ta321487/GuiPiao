using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GuiPiao.Services;

namespace GuiPiao.View;

public partial class WorkbenchSystemFontPickWindow : Window
{
    private readonly IReadOnlyList<string> _all;

    public string? SelectedSource { get; private set; }

    public WorkbenchSystemFontPickWindow(string? currentSource = null)
    {
        InitializeComponent();
        _all = FontFamilyPickerSupport.SystemFontFamilySources;
        ApplyFilter(string.Empty);

        if (!string.IsNullOrWhiteSpace(currentSource))
        {
            var match = _all.FirstOrDefault(s =>
                string.Equals(s, currentSource.Trim(), StringComparison.OrdinalIgnoreCase));
            if (match != null)
                FontList.SelectedItem = match;
        }

        Loaded += (_, _) => FilterBox.Focus();
    }

    private void FilterBox_OnTextChanged(object sender, TextChangedEventArgs e) =>
        ApplyFilter(FilterBox.Text);

    private void ApplyFilter(string? filter)
    {
        var q = (filter ?? string.Empty).Trim();
        IEnumerable<string> src = _all;
        if (!string.IsNullOrEmpty(q))
            src = _all.Where(s => s.Contains(q, StringComparison.CurrentCultureIgnoreCase));

        var selected = FontList.SelectedItem as string;
        FontList.ItemsSource = src.ToList();
        if (selected != null &&
            FontList.Items.Cast<string>().Any(s => string.Equals(s, selected, StringComparison.OrdinalIgnoreCase)))
            FontList.SelectedItem = selected;
    }

    private void FontList_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (FontList.SelectedItem is string) Accept();
    }

    private void OkButton_OnClick(object sender, RoutedEventArgs e) => Accept();

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Accept()
    {
        if (FontList.SelectedItem is not string s || string.IsNullOrWhiteSpace(s))
        {
            MessageBoxWindow.Show(this, "请选择字体。", "字体",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        SelectedSource = s;
        DialogResult = true;
        Close();
    }
}
