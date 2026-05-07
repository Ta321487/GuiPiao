using System;
using System.ComponentModel;

namespace GuiPiao.Model;

/// <summary>
///     操作历史项
/// </summary>
public class OperationHistoryItem : INotifyPropertyChanged
{
    private bool _isUndone;
    public int Index { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string OldValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }

    public bool IsUndone
    {
        get => _isUndone;
        set
        {
            if (_isUndone != value)
            {
                _isUndone = value;
                OnPropertyChanged(nameof(IsUndone));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}