using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GuiPiao.Mobile.Model;

public partial class SelectableTagItem : ObservableObject
{
    public string SyncId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#0078D4";
    public string TextColor { get; set; } = "#FFFFFF";

    [ObservableProperty] private bool _isSelected;

    [RelayCommand]
    private void Toggle() => IsSelected = !IsSelected;
}
