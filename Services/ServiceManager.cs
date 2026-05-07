using System;

namespace GuiPiao.Services
{
    public class ServiceManager
    {
        private static readonly Lazy<ServiceManager> _instance = new Lazy<ServiceManager>(() => new ServiceManager());

        private readonly Lazy<TesseractService> _tesseractService;
        private readonly Lazy<MapService> _mapService;
        private readonly Lazy<ChartService> _chartService;
        private readonly Lazy<LogService> _logService;

        private ServiceManager()
        {
            _tesseractService = new Lazy<TesseractService>(() => new TesseractService());
            _mapService = new Lazy<MapService>(() => new MapService());
            _chartService = new Lazy<ChartService>(() => new ChartService());
            _logService = new Lazy<LogService>(() => new LogService());
        }

        public static ServiceManager Instance => _instance.Value;

        public TesseractService TesseractService => _tesseractService.Value;
        public LogService LogService => _logService.Value;
    }

    // Tesseract OCR服务
    public class TesseractService
    {
        public TesseractService()
        {
            // 初始化Tesseract
            // 这里只在第一次使用时才会执行
        }

        public void RecognizeTicket(string imagePath)
        {
            // 实现OCR识别逻辑
        }
    }

    // 地图服务
    public class MapService
    {
        public MapService() { }
    }

    // 图表服务
    public class ChartService
    {
        public ChartService() { }
    }
}
