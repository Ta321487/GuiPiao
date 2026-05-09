using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;

namespace GuiPiao.ViewModel;

public partial class MainViewModel
{
    #region 转发属性 - 布局相关

    public int LeftPanelWidth => Layout.LeftPanelWidth;
    public bool LeftPanelLocked => Layout.LeftPanelLocked;
    public int RightPanelWidth => Layout.RightPanelWidth;
    public bool RightPanelLocked => Layout.RightPanelLocked;
    public int BottomPanelHeight => Layout.BottomPanelHeight;
    public bool BottomPanelLocked => Layout.BottomPanelLocked;
    public GridLength LeftColumnWidth => Layout.LeftColumnWidth;
    public GridLength RightColumnWidth => Layout.RightColumnWidth;
    public GridLength BottomRowHeight => Layout.BottomRowHeight;
    public bool LeftSplitterVisible => Layout.LeftSplitterVisible;
    public bool RightSplitterVisible => Layout.RightSplitterVisible;
    public bool BottomSplitterVisible => Layout.BottomSplitterVisible;
    public GridLength LeftSplitterWidth => Layout.LeftSplitterWidth;
    public GridLength RightSplitterWidth => Layout.RightSplitterWidth;
    public GridLength BottomSplitterHeight => Layout.BottomSplitterHeight;
    public string ScrollbarStyle => Layout.ScrollbarStyle;
    public bool ShowActionButtonsOnHover => Layout.ShowActionButtonsOnHover;
    public bool ShowTimestamp => Layout.ShowTimestamp;
    public bool ShowModuleSource => Layout.ShowModuleSource;
    public string LogRowHeight => Layout.LogRowHeight;
    public double LogRowHeightValue => Layout.LogRowHeightValue;

    #endregion

    private void SubscribeToLayoutChanges()
    {
        Layout.PropertyChanged += (s, e) =>
        {
            OnPropertyChanged(e.PropertyName);
            switch (e.PropertyName)
            {
                case nameof(Layout.LeftPanelWidth):
                    OnPropertyChanged(nameof(LeftPanelWidth));
                    OnPropertyChanged(nameof(LeftColumnWidth));
                    break;
                case nameof(Layout.LeftPanelLocked):
                    OnPropertyChanged(nameof(LeftPanelLocked));
                    OnPropertyChanged(nameof(LeftSplitterVisible));
                    OnPropertyChanged(nameof(LeftColumnWidth));
                    OnPropertyChanged(nameof(LeftSplitterWidth));
                    break;
                case nameof(Layout.RightPanelWidth):
                    OnPropertyChanged(nameof(RightPanelWidth));
                    OnPropertyChanged(nameof(RightColumnWidth));
                    break;
                case nameof(Layout.RightPanelLocked):
                    OnPropertyChanged(nameof(RightPanelLocked));
                    OnPropertyChanged(nameof(RightSplitterVisible));
                    OnPropertyChanged(nameof(RightColumnWidth));
                    OnPropertyChanged(nameof(RightSplitterWidth));
                    break;
                case nameof(Layout.BottomPanelHeight):
                    OnPropertyChanged(nameof(BottomPanelHeight));
                    OnPropertyChanged(nameof(BottomRowHeight));
                    break;
                case nameof(Layout.BottomPanelLocked):
                    OnPropertyChanged(nameof(BottomPanelLocked));
                    OnPropertyChanged(nameof(BottomSplitterVisible));
                    OnPropertyChanged(nameof(BottomRowHeight));
                    OnPropertyChanged(nameof(BottomSplitterHeight));
                    break;
                case nameof(Layout.ScrollbarStyle):
                    OnPropertyChanged(nameof(ScrollbarStyle));
                    break;
                case nameof(Layout.ShowActionButtonsOnHover):
                    OnPropertyChanged(nameof(ShowActionButtonsOnHover));
                    break;
                case nameof(Layout.ShowTimestamp):
                    OnPropertyChanged(nameof(ShowTimestamp));
                    break;
                case nameof(Layout.ShowModuleSource):
                    OnPropertyChanged(nameof(ShowModuleSource));
                    break;
                case nameof(Layout.LogRowHeight):
                    OnPropertyChanged(nameof(LogRowHeight));
                    OnPropertyChanged(nameof(LogRowHeightValue));
                    break;
            }
        };
    }
}
