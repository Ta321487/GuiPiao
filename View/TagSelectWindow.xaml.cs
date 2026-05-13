using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using GuiPiao.Model;

namespace GuiPiao.View;

public partial class TagSelectWindow : Window
{
    private readonly List<SelectableTag> _selectableTags;

    public TagSelectWindow(List<TicketTag> tags)
    {
        InitializeComponent();
        _selectableTags = tags.Select(t => new SelectableTag(t)).ToList();
        TagsItemsControl.ItemsSource = _selectableTags;
        SelectedTags = new List<TicketTag>();
    }

    public List<TicketTag> SelectedTags { get; private set; }

    private void TagBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.DataContext is SelectableTag tag) tag.IsSelected = !tag.IsSelected;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        SelectedTags = _selectableTags
            .Where(t => t.IsSelected)
            .Select(t => new TicketTag
            {
                Id = t.Id,
                Name = t.Name,
                Color = t.Color,
                TextColor = t.TextColor
            })
            .ToList();
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    public partial class SelectableTag : ObservableObject
    {
        [ObservableProperty] private bool _isSelected;

        public SelectableTag(TicketTag tag)
        {
            Id = tag.Id;
            Name = tag.Name;
            Color = tag.Color;
            TextColor = tag.TextColor;
            IsSelected = false;
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Color { get; set; }
        public string TextColor { get; set; }
    }
}