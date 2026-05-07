using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GuiPiao.Model;
using GuiPiao.Services;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GuiPiao.ViewModel
{
    /// <summary>
    /// 票面预览视图模型
    /// </summary>
    public partial class TicketPreviewViewModel : ObservableObject
    {
        private readonly UISettingsService _uiSettingsService;
        private TripItem? _tripItem;
        private double _baseScale = 1.0;
        private string _currentImageType = "blue";

        #region 显示属性

        /// <summary>
        /// 当前缩放值（字符串形式，如"FitWindow"、"100"等）
        /// </summary>
        [ObservableProperty]
        private string _currentZoom = "FitWindow";

        /// <summary>
        /// 亮度值 (50-150)
        /// </summary>
        [ObservableProperty]
        private int _brightness = 100;

        /// <summary>
        /// X轴缩放比例
        /// </summary>
        [ObservableProperty]
        private double _scaleX = 1.0;

        /// <summary>
        /// Y轴缩放比例
        /// </summary>
        [ObservableProperty]
        private double _scaleY = 1.0;

        /// <summary>
        /// 票面图片源
        /// </summary>
        [ObservableProperty]
        private ImageSource _ticketImageSource;

        /// <summary>
        /// 票面信息文本
        /// </summary>
        [ObservableProperty]
        private string _ticketInfo = "未选择行程";

        /// <summary>
        /// 缩放百分比文本
        /// </summary>
        public string ZoomPercentText => $"{Math.Round(ScaleX * 100)}%";

        /// <summary>
        /// 亮度因子 (0.5 - 1.5)
        /// </summary>
        public double BrightnessFactor => Brightness / 100.0;

        /// <summary>
        /// 亮度遮罩画刷（用于模拟亮度调整）
        /// </summary>
        public Brush BrightnessOverlayBrush
        {
            get
            {
                if (Brightness > 100)
                {
                    // 亮度 > 100%：使用白色半透明遮罩
                    double opacity = (Brightness - 100) / 100.0 * 0.3;
                    return new SolidColorBrush(Color.FromArgb((byte)(opacity * 255), 255, 255, 255));
                }
                else if (Brightness < 100)
                {
                    // 亮度 < 100%：使用黑色半透明遮罩
                    double opacity = (100 - Brightness) / 100.0 * 0.5;
                    return new SolidColorBrush(Color.FromArgb((byte)(opacity * 255), 0, 0, 0));
                }
                return Brushes.Transparent;
            }
        }

        /// <summary>
        /// 图片水平对齐方式
        /// </summary>
        public HorizontalAlignment ImageHorizontalAlignment =>
            _uiSettingsService?.Config?.TicketCentered ?? true ? HorizontalAlignment.Center : HorizontalAlignment.Left;

        /// <summary>
        /// 图片垂直对齐方式
        /// </summary>
        public VerticalAlignment ImageVerticalAlignment =>
            _uiSettingsService?.Config?.TicketCentered ?? true ? VerticalAlignment.Center : VerticalAlignment.Top;

        /// <summary>
        /// 是否允许鼠标滚轮缩放
        /// </summary>
        public bool AllowMouseWheelZoom =>
            _uiSettingsService?.Config?.AllowMouseWheelZoom ?? true;

        /// <summary>
        /// 获取当前缩放设置值
        /// </summary>
        public string GetCurrentZoomSetting() => CurrentZoom;

        /// <summary>
        /// 获取当前亮度设置值
        /// </summary>
        public int GetCurrentBrightnessSetting() => Brightness;

        #endregion

        public TicketPreviewViewModel()
        {
            _uiSettingsService = new UISettingsService();
            LoadSettings();
            // 默认加载蓝色票样
            LoadSampleImage("blue");
        }

        /// <summary>
        /// 加载设置
        /// </summary>
        private void LoadSettings()
        {
            var config = _uiSettingsService.Config;
            CurrentZoom = config.DefaultZoom;
            Brightness = config.DisplayBrightness;

            // 应用初始缩放
            ApplyZoom(CurrentZoom);
        }

        /// <summary>
        /// 设置行程数据
        /// </summary>
        public void SetTripItem(TripItem tripItem)
        {
            _tripItem = tripItem;
            if (tripItem != null)
            {
                TicketInfo = $"{tripItem.TrainNo} {tripItem.DepartStation} → {tripItem.ArriveStation} | {tripItem.DepartDate}";

                // 加载票面图片（如果有）
                LoadTicketImage(tripItem);
            }
        }

        /// <summary>
        /// 加载票面图片
        /// </summary>
        private void LoadTicketImage(TripItem tripItem)
        {
            // TODO: 从实际数据源加载票面图片
            // 这里使用一个占位图片或默认图片
            // 如果有TicketImagePath属性，可以加载：
            // if (!string.IsNullOrEmpty(tripItem.TicketImagePath) && File.Exists(tripItem.TicketImagePath))
            // {
            //     TicketImageSource = new BitmapImage(new Uri(tripItem.TicketImagePath));
            // }

            // 暂时不设置图片，等待实际实现
            TicketImageSource = null;
        }

        /// <summary>
        /// 应用缩放
        /// </summary>
        private void ApplyZoom(string zoom)
        {
            if (zoom == "FitWindow")
            {
                // 适应窗口：将在窗口大小变化时计算
                ScaleX = 1.0;
                ScaleY = 1.0;
            }
            else if (double.TryParse(zoom, out double percent))
            {
                double scale = percent / 100.0;
                ScaleX = scale;
                ScaleY = scale;
            }
            OnPropertyChanged(nameof(ZoomPercentText));
        }

        /// <summary>
        /// 当前缩放值变化
        /// </summary>
        partial void OnCurrentZoomChanged(string value)
        {
            ApplyZoom(value);
        }

        /// <summary>
        /// 亮度变化
        /// </summary>
        partial void OnBrightnessChanged(int value)
        {
            OnPropertyChanged(nameof(BrightnessFactor));
            OnPropertyChanged(nameof(BrightnessOverlayBrush));
        }

        #region 命令

        /// <summary>
        /// 放大命令
        /// </summary>
        [RelayCommand]
        private void ZoomIn()
        {
            double currentPercent = ScaleX * 100;
            double newPercent = currentPercent switch
            {
                < 50 => 50,
                < 75 => 75,
                < 100 => 100,
                < 125 => 125,
                < 150 => 150,
                < 200 => 200,
                < 300 => 300,
                < 400 => 400,
                _ => 400
            };

            if (newPercent > currentPercent)
            {
                CurrentZoom = newPercent.ToString();
            }
        }

        /// <summary>
        /// 缩小命令
        /// </summary>
        [RelayCommand]
        private void ZoomOut()
        {
            double currentPercent = ScaleX * 100;
            double newPercent = currentPercent switch
            {
                > 300 => 300,
                > 200 => 200,
                > 150 => 150,
                > 125 => 125,
                > 100 => 100,
                > 75 => 75,
                > 50 => 50,
                _ => 50
            };

            if (newPercent < currentPercent)
            {
                CurrentZoom = newPercent.ToString();
            }
        }

        /// <summary>
        /// 鼠标滚轮缩放
        /// </summary>
        public void HandleMouseWheel(double delta)
        {
            if (!AllowMouseWheelZoom) return;

            if (delta > 0)
            {
                ZoomIn();
            }
            else
            {
                ZoomOut();
            }
        }

        /// <summary>
        /// 切换图片命令
        /// </summary>
        [RelayCommand]
        private void SwitchImage(string imageType)
        {
            _currentImageType = imageType;
            LoadSampleImage(imageType);
        }

        /// <summary>
        /// 加载示例图片
        /// </summary>
        private void LoadSampleImage(string imageType)
        {
            try
            {
                string fileName = imageType.ToLower() == "red" ? "redTicket.png" : "blueTicket.png";
                string imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Images", fileName);

                if (File.Exists(imagePath))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(imagePath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    if (bitmap.CanFreeze)
                        bitmap.Freeze();
                    TicketImageSource = bitmap;
                }
                else
                {
                    // 如果文件不存在，尝试从项目目录加载
                    imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Resources", "Images", fileName);
                    if (File.Exists(imagePath))
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(imagePath);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        if (bitmap.CanFreeze)
                            bitmap.Freeze();
                        TicketImageSource = bitmap;
                    }
                }
            }
            catch (Exception)
            {
                // 加载失败时不显示图片
                TicketImageSource = null;
            }
        }

        #endregion
    }
}
