using System.Collections.ObjectModel;
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

    [ObservableProperty] private ThemeMode _themeMode = ThemeMode.Light;
    [ObservableProperty] private AccentColor _accentColor = AccentColor.MicrosoftBlue;
    [ObservableProperty] private string _customColor = "#0078D4";
    [ObservableProperty] private string _baseUrl = "http://127.0.0.1:17880";
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private string _statsText = string.Empty;
    [ObservableProperty] private ObservableCollection<string> _yearStats = new();
    [ObservableProperty] private ObservableCollection<MobileTag> _tagsList = new();
    [ObservableProperty] private string _newTagName = string.Empty;

    public IReadOnlyList<ThemeMode> ThemeModes { get; } =
        Enum.GetValues<ThemeMode>();

    public IReadOnlyList<AccentColor> AccentColors { get; } =
        Enum.GetValues<AccentColor>();

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
        ThemeMode = appearance.ThemeMode;
        AccentColor = appearance.AccentColor;
        CustomColor = appearance.CustomColor;
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
    private void ApplyAppearance()
    {
        var appearance = new AppearanceConfig
        {
            ThemeMode = ThemeMode,
            AccentColor = AccentColor,
            CustomColor = CustomColor
        };
        _settings.SaveAppearance(appearance);
        _theme.Apply(appearance);
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
        _theme.ApplyThemeMode(value);
        _theme.ApplyAccentColor(AccentColor, CustomColor);
    }

    partial void OnAccentColorChanged(AccentColor value)
    {
        _theme.ApplyAccentColor(value, CustomColor);
    }

    partial void OnCustomColorChanged(string value)
    {
        if (AccentColor == AccentColor.Custom)
            _theme.ApplyAccentColor(AccentColor, value);
    }
}
