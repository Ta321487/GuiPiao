using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GuiPiao.Mobile.Data;
using GuiPiao.Mobile.Model;
using GuiPiao.Mobile.Services;

namespace GuiPiao.Mobile.ViewModels;

public partial class MeViewModel : ObservableObject
{
    private readonly MobileSettingsStore _settings;
    private readonly ThemeService _theme;
    private readonly RideRepository _rides;
    private readonly TagRepository _tags;
    private readonly TagWriteService _tagWrite;
    private readonly SyncPushQueue _pushQueue;
    private bool _loadingAppearance;
    private bool _syncingCustomRgb;
    private CancellationTokenSource? _customAccentDebounce;

    [ObservableProperty] private ThemeMode _themeMode = ThemeMode.Light;
    [ObservableProperty] private AccentColor _accentColor = AccentColor.MicrosoftBlue;
    [ObservableProperty] private string _customColor = "#0078D4";
    [ObservableProperty] private Color _customPreview = Color.FromArgb("#0078D4");
    [ObservableProperty] private double _customRed = 0;
    [ObservableProperty] private double _customGreen = 120;
    [ObservableProperty] private double _customBlue = 212;
    [ObservableProperty] private bool _isCustomAccent;
    [ObservableProperty] private string _baseUrl = "http://127.0.0.1:17880";
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private string _statsText = string.Empty;
    [ObservableProperty] private ObservableCollection<string> _yearStats = new();
    [ObservableProperty] private ObservableCollection<MobileTag> _tagsList = new();
    [ObservableProperty] private string _newTagName = string.Empty;

    public IReadOnlyList<ThemeModeOption> ThemeModeOptions { get; } =
    [
        new() { Value = ThemeMode.Light, Title = "浅色模式" },
        new() { Value = ThemeMode.Dark, Title = "深色模式" },
        new() { Value = ThemeMode.System, Title = "跟随系统" }
    ];

    public IReadOnlyList<AccentColorOption> AccentColorOptions { get; } =
    [
        new() { Value = AccentColor.MicrosoftBlue, Title = "微软蓝", Hex = "#0078D4" },
        new() { Value = AccentColor.FreshGreen, Title = "清新绿", Hex = "#28A745" },
        new() { Value = AccentColor.VitalityOrange, Title = "活力橙", Hex = "#FD7E14" },
        new() { Value = AccentColor.DarkPurple, Title = "暗夜紫", Hex = "#6F42C1" },
        new() { Value = AccentColor.MinimalGray, Title = "极简灰", Hex = "#6C757D" },
        new() { Value = AccentColor.Custom, Title = "自定义", Hex = "#0078D4" }
    ];

    /// <summary>自定义色板（对齐 PC ColorPickerDialog 常用色）。</summary>
    public IReadOnlyList<CustomColorSwatch> CustomColorPresets { get; } =
    [
        new() { Hex = "#E81123" }, new() { Hex = "#F7630C" }, new() { Hex = "#FFF100" }, new() { Hex = "#16C60C" },
        new() { Hex = "#00B7C3" }, new() { Hex = "#0078D4" }, new() { Hex = "#8764B8" }, new() { Hex = "#E3008C" },
        new() { Hex = "#FFFFFF" }, new() { Hex = "#D2D2D2" }, new() { Hex = "#888888" }, new() { Hex = "#505050" },
        new() { Hex = "#000000" }, new() { Hex = "#F4A6A6" }, new() { Hex = "#FA8072" }, new() { Hex = "#8B0000" },
        new() { Hex = "#FF8C00" }, new() { Hex = "#DAA520" }, new() { Hex = "#9ACD32" }, new() { Hex = "#228B22" },
        new() { Hex = "#87CEEB" }, new() { Hex = "#4682B4" }, new() { Hex = "#000080" }, new() { Hex = "#9400D3" }
    ];

    public MeViewModel(
        MobileSettingsStore settings,
        ThemeService theme,
        RideRepository rides,
        TagRepository tags,
        TagWriteService tagWrite,
        SyncPushQueue pushQueue)
    {
        _settings = settings;
        _theme = theme;
        _rides = rides;
        _tags = tags;
        _tagWrite = tagWrite;
        _pushQueue = pushQueue;
        Reload();
    }

    public void Reload()
    {
        var appearance = _settings.LoadAppearance();
        _loadingAppearance = true;
        try
        {
            ThemeMode = appearance.ThemeMode;
            AccentColor = appearance.AccentColor;
            CustomColor = NormalizeHex(appearance.CustomColor);
            SyncCustomRgbFromHex(CustomColor);
            RefreshSelectionFlags();
        }
        finally
        {
            _loadingAppearance = false;
        }

        BaseUrl = _settings.LoadSync().BaseUrl;

        var total = _rides.CountActive();
        var pending = _pushQueue.Count;
        StatsText = $"行程 {total} 条 · 待推送 {pending} 条";
        YearStats = new ObservableCollection<string>(
            _rides.StatsByDepartYear()
                .Select(x => $"{x.Year} 年 · {x.Count} 趟 · ¥{x.Money:0.##}"));
        TagsList = new ObservableCollection<MobileTag>(_tags.ListActive());
    }

    [RelayCommand]
    private async Task OpenTagsAsync() =>
        await Shell.Current.GoToAsync("tags");

    [RelayCommand]
    private async Task OpenConnectionAsync() =>
        await Shell.Current.GoToAsync("syncconnection");

    [RelayCommand]
    private void SelectThemeMode(ThemeModeOption? option)
    {
        if (option == null) return;
        ThemeMode = option.Value;
        PersistAppearance(showStatus: true);
    }

    [RelayCommand]
    private void SelectAccentColor(AccentColorOption? option)
    {
        if (option == null) return;
        AccentColor = option.Value;
        if (option.Value == AccentColor.Custom)
            SyncCustomRgbFromHex(CustomColor);
        PersistAppearance(showStatus: true);
    }

    [RelayCommand]
    private void PickCustomPreset(CustomColorSwatch? swatch)
    {
        if (swatch == null) return;
        AccentColor = AccentColor.Custom;
        CustomColor = swatch.Hex;
        SyncCustomRgbFromHex(CustomColor);
        PersistAppearance(showStatus: true);
    }

    [RelayCommand]
    private void ApplyAppearance() => PersistAppearance(showStatus: true);

    private void PersistAppearance(bool showStatus)
    {
        if (_loadingAppearance) return;

        var appearance = new AppearanceConfig
        {
            ThemeMode = ThemeMode,
            AccentColor = AccentColor,
            CustomColor = NormalizeHex(CustomColor)
        };
        _settings.SaveAppearance(appearance);
        _theme.Apply(appearance);
        RefreshSelectionFlags();
        if (showStatus)
            StatusMessage = $"已应用 · {ThemeService.ResolveAccentHex(AccentColor, CustomColor)}";
    }

    [RelayCommand]
    private void SaveServerAddress()
    {
        var sync = _settings.LoadSync();
        sync.BaseUrl = BaseUrl.Trim();
        _settings.SaveSync(sync);
        StatusMessage = "服务地址已保存";
    }

    [RelayCommand]
    private async Task PasteServerUrlAsync()
    {
        try
        {
            if (!Clipboard.Default.HasText)
            {
                StatusMessage = "剪贴板为空";
                return;
            }

            var text = await Clipboard.Default.GetTextAsync() ?? "";
            var url = ServerUrlQrHelper.ExtractHttpUrl(text);
            if (string.IsNullOrWhiteSpace(url))
            {
                StatusMessage = "未识别到 http 地址";
                return;
            }

            BaseUrl = url;
            SaveServerAddress();
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task ScanQrFromPhotoAsync()
    {
        try
        {
            var photo = await MediaPicker.Default.PickPhotoAsync();
            if (photo == null) return;
            await using var stream = await photo.OpenReadAsync();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            var decoded = ServerUrlQrHelper.TryDecodeQr(ms.ToArray());
            var url = ServerUrlQrHelper.ExtractHttpUrl(decoded);
            if (string.IsNullOrWhiteSpace(url))
            {
                StatusMessage = "未识别到二维码地址";
                return;
            }

            BaseUrl = url;
            SaveServerAddress();
            StatusMessage = "扫码已填入服务地址";
        }
        catch (Exception ex)
        {
            StatusMessage = "扫码失败：" + ex.Message;
        }
    }

    [RelayCommand]
    private void AddTag()
    {
        var name = (NewTagName ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            StatusMessage = "请输入标签名";
            return;
        }

        _tagWrite.SaveUpsert(new MobileTag
        {
            Name = name,
            Color = "#0078D4",
            TextColor = "#FFFFFF",
            SortOrder = TagsList.Count
        });
        NewTagName = string.Empty;
        Reload();
        StatusMessage = "标签已保存，对齐后推送到 PC";
    }

    [RelayCommand]
    private async Task RenameTagAsync(MobileTag? tag)
    {
        if (tag == null) return;
        var name = await Shell.Current.DisplayPromptAsync(
            "重命名标签",
            "新名称",
            accept: "保存",
            cancel: "取消",
            placeholder: tag.Name,
            maxLength: 32,
            keyboard: Keyboard.Default,
            initialValue: tag.Name);
        if (string.IsNullOrWhiteSpace(name)) return;
        tag.Name = name.Trim();
        _tagWrite.SaveUpsert(tag);
        Reload();
        StatusMessage = "标签已更新，对齐后推送";
    }

    [RelayCommand]
    private async Task DeleteTagAsync(MobileTag? tag)
    {
        if (tag == null) return;
        var ok = await Shell.Current.DisplayAlertAsync(
            "删除标签",
            $"删除「{tag.Name}」？对齐后会推送到 PC。",
            "删除",
            "取消");
        if (!ok) return;
        _tagWrite.SoftDelete(tag.SyncId);
        Reload();
        StatusMessage = "标签已删除（待推送）";
    }

    partial void OnThemeModeChanged(ThemeMode value)
    {
        if (_loadingAppearance) return;
        RefreshSelectionFlags();
        // 选色命令会 PersistAppearance；此处仅即时预览，避免重复 Apply + 写盘
        _theme.ApplyThemeMode(value);
        _theme.ApplyAccentColor(AccentColor, CustomColor);
    }

    partial void OnAccentColorChanged(AccentColor value)
    {
        if (_loadingAppearance) return;
        IsCustomAccent = value == AccentColor.Custom;
        RefreshSelectionFlags();
        _theme.ApplyAccentColor(value, CustomColor);
    }

    partial void OnCustomColorChanged(string value)
    {
        if (_loadingAppearance) return;
        if (!_syncingCustomRgb)
            SyncCustomRgbFromHex(value);
        UpdateCustomPreview(value);
        RefreshCustomAccentSwatch();
        if (AccentColor == AccentColor.Custom)
            ScheduleCustomAccentPersist(value);
    }

    /// <summary>滑条拖动时只预览强调色，落盘与完整 Apply 防抖，避免整页卡顿。</summary>
    private void ScheduleCustomAccentPersist(string value)
    {
        _customAccentDebounce?.Cancel();
        _customAccentDebounce = new CancellationTokenSource();
        var token = _customAccentDebounce.Token;
        var hex = NormalizeHex(value);
        _theme.ApplyAccentHex(hex);

        _ = DebouncePersistCustomAsync(hex, token);
    }

    private async Task DebouncePersistCustomAsync(string hex, CancellationToken token)
    {
        try
        {
            await Task.Delay(350, token);
            if (token.IsCancellationRequested || _loadingAppearance) return;
            var appearance = new AppearanceConfig
            {
                ThemeMode = ThemeMode,
                AccentColor = AccentColor.Custom,
                CustomColor = hex
            };
            _settings.SaveAppearance(appearance);
        }
        catch (TaskCanceledException)
        {
            // ignore
        }
    }

    partial void OnCustomRedChanged(double value) => SyncHexFromRgb();
    partial void OnCustomGreenChanged(double value) => SyncHexFromRgb();
    partial void OnCustomBlueChanged(double value) => SyncHexFromRgb();

    private void SyncHexFromRgb()
    {
        if (_loadingAppearance || _syncingCustomRgb) return;
        _syncingCustomRgb = true;
        try
        {
            var hex = $"#{(int)Math.Clamp(CustomRed, 0, 255):X2}{(int)Math.Clamp(CustomGreen, 0, 255):X2}{(int)Math.Clamp(CustomBlue, 0, 255):X2}";
            if (!string.Equals(CustomColor, hex, StringComparison.OrdinalIgnoreCase))
            {
                AccentColor = AccentColor.Custom;
                CustomColor = hex;
            }
        }
        finally
        {
            _syncingCustomRgb = false;
        }
    }

    private void SyncCustomRgbFromHex(string? hex)
    {
        if (!TryParseHex(hex, out var r, out var g, out var b))
            return;

        _syncingCustomRgb = true;
        try
        {
            CustomRed = r;
            CustomGreen = g;
            CustomBlue = b;
            UpdateCustomPreview(NormalizeHex(hex!));
        }
        finally
        {
            _syncingCustomRgb = false;
        }
    }

    private void UpdateCustomPreview(string? hex)
    {
        if (TryParseHex(hex, out var r, out var g, out var b))
            CustomPreview = Color.FromRgb(r, g, b);
    }

    private void RefreshSelectionFlags()
    {
        foreach (var option in ThemeModeOptions)
            option.IsSelected = option.Value == ThemeMode;

        foreach (var option in AccentColorOptions)
        {
            option.IsSelected = option.Value == AccentColor;
            if (option.Value == AccentColor.Custom)
                RefreshCustomAccentSwatch();
        }

        IsCustomAccent = AccentColor == AccentColor.Custom;
    }

    private void RefreshCustomAccentSwatch()
    {
        var custom = AccentColorOptions.FirstOrDefault(x => x.Value == AccentColor.Custom);
        if (custom == null) return;
        custom.Hex = NormalizeHex(CustomColor);
    }

    private static string NormalizeHex(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return "#0078D4";
        hex = hex.Trim();
        if (!hex.StartsWith('#')) hex = "#" + hex;
        return hex.Length is 7 or 9 ? hex.ToUpperInvariant() : "#0078D4";
    }

    private static bool TryParseHex(string? hex, out int r, out int g, out int b)
    {
        r = g = b = 0;
        if (string.IsNullOrWhiteSpace(hex)) return false;
        hex = hex.Trim();
        if (hex.StartsWith('#')) hex = hex[1..];
        if (hex.Length == 8) hex = hex[..6];
        if (hex.Length != 6) return false;
        return int.TryParse(hex.AsSpan(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out r)
               && int.TryParse(hex.AsSpan(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out g)
               && int.TryParse(hex.AsSpan(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out b);
    }
}
