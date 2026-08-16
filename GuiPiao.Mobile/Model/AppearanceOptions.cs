using CommunityToolkit.Mvvm.ComponentModel;

namespace GuiPiao.Mobile.Model;

public sealed partial class ThemeModeOption : ObservableObject
{
    public ThemeMode Value { get; init; }
    public string Title { get; init; } = string.Empty;

    [ObservableProperty] private bool _isSelected;
}

public sealed partial class AccentColorOption : ObservableObject
{
    public AccentColor Value { get; init; }
    public string Title { get; init; } = string.Empty;

    [ObservableProperty] private string _hex = "#0078D4";
    [ObservableProperty] private bool _isSelected;

    public Color Swatch => Color.FromArgb(string.IsNullOrWhiteSpace(Hex) ? "#0078D4" : Hex);

    partial void OnHexChanged(string value) => OnPropertyChanged(nameof(Swatch));
}

public sealed class CustomColorSwatch
{
    public string Hex { get; init; } = "#0078D4";
    public Color Color => Color.FromArgb(Hex);
}
