using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using GuiPiao.Model;
using GuiPiao.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace GuiPiao.Services;

/// <summary>
///     设置导入导出服务
/// </summary>
public class SettingsImportExportService
{
    private static readonly Lazy<SettingsImportExportService> _instance = new(() => new SettingsImportExportService());
    private readonly Dictionary<string, Type> _configTypes;

    private readonly JsonSerializerSettings _serializerSettings;

    private SettingsImportExportService()
    {
        _serializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            TypeNameHandling = TypeNameHandling.Auto,
            Converters = new List<JsonConverter> { new StringEnumConverter() },
            NullValueHandling = NullValueHandling.Include,
            DefaultValueHandling = DefaultValueHandling.Include,
            MissingMemberHandling = MissingMemberHandling.Error
        };

        // 注册所有配置类型
        _configTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            { "GeneralConfig", typeof(GeneralConfig) },
            { "DatabaseConfig", typeof(DatabaseConfig) },
            { "UISettingsConfig", typeof(UISettingsConfig) },
            { "MapSettingsConfig", typeof(MapSettingsConfig) },
            { "DashboardConfig", typeof(DashboardConfig) },
            { "OcrConfig", typeof(OcrConfig) },
            { "ExportConfig", typeof(ExportConfig) },
            { "ShortcutConfig", typeof(ShortcutConfig) }
        };
    }

    public static SettingsImportExportService Instance => _instance.Value;

    /// <summary>
    ///     导出所有设置到文件
    /// </summary>
    /// <param name="filePath">导出文件路径</param>
    /// <returns>是否导出成功</returns>
    public async Task<bool> ExportSettingsAsync(string filePath)
    {
        try
        {
            var exportData = new SettingsExportData
            {
                General = LoadConfig<GeneralConfig>("generalsettings.json"),
                Database = LoadConfig<DatabaseConfig>("databasesettings.json"),
                UI = LoadConfig<UISettingsConfig>("uisettings.json"),
                Map = LoadConfig<MapSettingsConfig>("mapsettings.json"),
                Dashboard = LoadConfig<DashboardConfig>("dashboardsettings.json"),
                OCR = LoadConfig<OcrConfig>("ocrsettings.json"),
                Export = LoadConfig<ExportConfig>("exportsettings.json"),
                Shortcut = LoadConfig<ShortcutConfig>("shortcutsettings.json")
            };

            var json = JsonConvert.SerializeObject(exportData, _serializerSettings);
            await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SettingsImportExportService] 导出设置失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    ///     从文件导入设置
    /// </summary>
    /// <param name="filePath">导入文件路径</param>
    /// <returns>导入验证结果</returns>
    public async Task<ImportValidationResult> ImportSettingsAsync(string filePath)
    {
        var result = new ImportValidationResult();

        try
        {
            if (!File.Exists(filePath))
            {
                result.Errors.Add("导入文件不存在");
                return result;
            }

            var json = await File.ReadAllTextAsync(filePath, Encoding.UTF8);

            // 1. 基本JSON格式验证
            JObject? rootObject;
            try
            {
                rootObject = JObject.Parse(json);
            }
            catch (JsonException ex)
            {
                result.Errors.Add($"JSON格式错误: {ex.Message}");
                return result;
            }

            // 2. 验证必需字段
            if (!ValidateRequiredFields(rootObject, result.Errors)) return result;

            // 3. 验证版本兼容性
            var version = rootObject["Version"]?.Value<string>();
            if (!string.IsNullOrEmpty(version))
                if (!IsVersionCompatible(version))
                {
                    result.Errors.Add(
                        $"版本不兼容: 导出文件版本为 {version}，当前应用程序版本为 {Assembly.GetExecutingAssembly().GetName().Version}");
                    return result;
                }

            // 4. 验证各配置项
            var exportData = new SettingsExportData();
            var hasValidConfig = false;

            // 验证常规设置
            if (rootObject["General"] is JObject generalObj)
            {
                var validation = ValidateConfig<GeneralConfig>(generalObj, "常规设置");
                if (!validation.IsValid)
                {
                    result.Errors.AddRange(validation.Errors);
                }
                else
                {
                    exportData.General = validation.Config;
                    hasValidConfig = true;
                }
            }

            // 验证数据库设置
            if (rootObject["Database"] is JObject databaseObj)
            {
                var validation = ValidateConfig<DatabaseConfig>(databaseObj, "数据库设置");
                if (!validation.IsValid)
                {
                    result.Errors.AddRange(validation.Errors);
                }
                else
                {
                    exportData.Database = validation.Config;
                    hasValidConfig = true;
                }
            }

            // 验证界面设置
            if (rootObject["UI"] is JObject uiObj)
            {
                var validation = ValidateConfig<UISettingsConfig>(uiObj, "界面设置");
                if (!validation.IsValid)
                {
                    result.Errors.AddRange(validation.Errors);
                }
                else
                {
                    exportData.UI = validation.Config;
                    hasValidConfig = true;
                }
            }

            // 验证地图设置
            if (rootObject["Map"] is JObject mapObj)
            {
                var validation = ValidateConfig<MapSettingsConfig>(mapObj, "地图设置");
                if (!validation.IsValid)
                {
                    result.Errors.AddRange(validation.Errors);
                }
                else
                {
                    exportData.Map = validation.Config;
                    hasValidConfig = true;
                }
            }

            // 验证仪表盘设置
            if (rootObject["Dashboard"] is JObject dashboardObj)
            {
                var validation = ValidateConfig<DashboardConfig>(dashboardObj, "仪表盘设置");
                if (!validation.IsValid)
                {
                    result.Errors.AddRange(validation.Errors);
                }
                else
                {
                    exportData.Dashboard = validation.Config;
                    hasValidConfig = true;
                }
            }

            // 验证OCR设置
            if (rootObject["OCR"] is JObject ocrObj)
            {
                var validation = ValidateConfig<OcrConfig>(ocrObj, "OCR设置");
                if (!validation.IsValid)
                {
                    result.Errors.AddRange(validation.Errors);
                }
                else
                {
                    exportData.OCR = validation.Config;
                    hasValidConfig = true;
                }
            }

            // 验证导出设置
            if (rootObject["Export"] is JObject exportObj)
            {
                var validation = ValidateConfig<ExportConfig>(exportObj, "导出设置");
                if (!validation.IsValid)
                {
                    result.Errors.AddRange(validation.Errors);
                }
                else
                {
                    exportData.Export = validation.Config;
                    hasValidConfig = true;
                }
            }

            // 验证快捷键设置
            if (rootObject["Shortcut"] is JObject shortcutObj)
            {
                var validation = ValidateConfig<ShortcutConfig>(shortcutObj, "快捷键设置");
                if (!validation.IsValid)
                {
                    result.Errors.AddRange(validation.Errors);
                }
                else
                {
                    exportData.Shortcut = validation.Config;
                    hasValidConfig = true;
                }
            }

            // 5. 检查是否至少有一个有效配置
            if (!hasValidConfig)
            {
                result.Errors.Add("导入文件中未找到任何有效的配置项");
                return result;
            }

            // 验证通过
            result.IsValid = true;
            result.ValidatedData = exportData;
            return result;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"导入过程中发生错误: {ex.Message}");
            return result;
        }
    }

    /// <summary>
    ///     应用导入的设置
    /// </summary>
    /// <param name="data">验证通过的配置数据</param>
    public void ApplyImportedSettings(SettingsExportData data)
    {
        if (data.General != null) SaveConfig("generalsettings.json", data.General);

        if (data.Database != null) SaveConfig("databasesettings.json", data.Database);

        if (data.UI != null) SaveConfig("uisettings.json", data.UI);

        if (data.Map != null) SaveConfig("mapsettings.json", data.Map);

        if (data.Dashboard != null) SaveConfig("dashboardsettings.json", data.Dashboard);

        if (data.OCR != null) SaveConfig("ocrsettings.json", data.OCR);

        if (data.Export != null) SaveConfig("exportsettings.json", data.Export);

        if (data.Shortcut != null) SaveConfig("shortcutsettings.json", data.Shortcut);
    }

    /// <summary>
    ///     验证必需字段
    /// </summary>
    private bool ValidateRequiredFields(JObject rootObject, List<string> errors)
    {
        var isValid = true;

        // 检查版本号
        if (rootObject["Version"] == null)
        {
            errors.Add("缺少必需字段: Version");
            isValid = false;
        }

        // 检查导出时间
        if (rootObject["ExportTime"] == null)
        {
            errors.Add("缺少必需字段: ExportTime");
            isValid = false;
        }

        // 检查应用程序版本
        if (rootObject["AppVersion"] == null)
        {
            errors.Add("缺少必需字段: AppVersion");
            isValid = false;
        }

        return isValid;
    }

    /// <summary>
    ///     验证配置对象
    /// </summary>
    private (bool IsValid, List<string> Errors, T? Config) ValidateConfig<T>(JObject configObj, string configName)
        where T : class, new()
    {
        var errors = new List<string>();
        T? config = null;

        try
        {
            // 尝试反序列化
            config = configObj.ToObject<T>(JsonSerializer.Create(_serializerSettings));

            if (config == null)
            {
                errors.Add($"{configName}: 反序列化失败");
                return (false, errors, null);
            }

            // 验证属性值范围（如果有范围限制）
            var validationErrors = ValidatePropertyRanges(config, configName);
            if (validationErrors.Any())
            {
                errors.AddRange(validationErrors);
                return (false, errors, null);
            }

            return (true, errors, config);
        }
        catch (JsonException ex)
        {
            errors.Add($"{configName}: JSON格式错误 - {ex.Message}");
            return (false, errors, null);
        }
        catch (Exception ex)
        {
            errors.Add($"{configName}: 验证失败 - {ex.Message}");
            return (false, errors, null);
        }
    }

    /// <summary>
    ///     验证属性值范围
    /// </summary>
    private List<string> ValidatePropertyRanges<T>(T config, string configName) where T : class
    {
        var errors = new List<string>();
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            var value = prop.GetValue(config);

            // 检查枚举值是否有效
            if (prop.PropertyType.IsEnum && value != null)
                if (!Enum.IsDefined(prop.PropertyType, value))
                    errors.Add($"{configName}.{prop.Name}: 无效的枚举值 '{value}'");

            // 检查字符串长度
            if (prop.PropertyType == typeof(string) && value is string strValue)
            {
                // 检查颜色格式
                if (prop.Name.EndsWith("Color") && !string.IsNullOrEmpty(strValue))
                    if (!IsValidColorFormat(strValue))
                        errors.Add($"{configName}.{prop.Name}: 无效的颜色格式 '{strValue}'");

                // 检查路径格式
                if ((prop.Name.EndsWith("Path") || prop.Name.EndsWith("Directory")) && !string.IsNullOrEmpty(strValue))
                    if (strValue.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                        errors.Add($"{configName}.{prop.Name}: 包含非法路径字符");
            }

            // 检查数值范围
            if (value is int intValue)
            {
                // 检查百分比值 (0-100)
                if (prop.Name.Contains("Percent") || prop.Name.Contains("Progress") || prop.Name.Contains("Threshold"))
                    if (intValue < 0 || intValue > 100)
                        errors.Add($"{configName}.{prop.Name}: 百分比值必须在 0-100 之间，当前值: {intValue}");

                // 检查面板尺寸
                if (prop.Name.Contains("Width") || prop.Name.Contains("Height"))
                    if (intValue < 0 || intValue > 2000)
                        errors.Add($"{configName}.{prop.Name}: 尺寸值必须在 0-2000 之间，当前值: {intValue}");
            }
        }

        return errors;
    }

    /// <summary>
    ///     检查颜色格式是否有效
    /// </summary>
    private bool IsValidColorFormat(string color)
    {
        if (string.IsNullOrEmpty(color))
            return true; // 空值视为有效

        // 支持 #RGB, #RRGGBB, #AARRGGBB 格式
        if (color.StartsWith("#"))
        {
            var hex = color.Substring(1);
            return hex.Length == 3 || hex.Length == 6 || hex.Length == 8;
        }

        // 支持命名颜色
        var namedColors = new[] { "Red", "Green", "Blue", "Black", "White", "Yellow", "Orange", "Purple", "Gray" };
        return namedColors.Contains(color, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     检查版本兼容性
    /// </summary>
    private bool IsVersionCompatible(string exportVersion)
    {
        try
        {
            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0, 0);
            var exported = Version.Parse(exportVersion);

            // 主版本号必须相同
            if (exported.Major != currentVersion.Major) return false;

            // 导出版本不能高于当前版本
            if (exported > currentVersion) return false;

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    ///     从文件加载配置
    /// </summary>
    private T? LoadConfig<T>(string fileName) where T : class, new()
    {
        try
        {
            return JsonConfigManager.Instance.LoadConfig(fileName, new T());
        }
        catch
        {
            return new T();
        }
    }

    /// <summary>
    ///     保存配置到文件
    /// </summary>
    private void SaveConfig<T>(string fileName, T config) where T : class
    {
        JsonConfigManager.Instance.SaveConfig(fileName, config);
    }

    /// <summary>
    ///     获取默认导出文件名
    /// </summary>
    public string GetDefaultExportFileName()
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        return $"GuiPiao_Settings_{timestamp}.json";
    }

    /// <summary>
    ///     获取导入文件过滤器
    /// </summary>
    public string GetImportFileFilter()
    {
        return "JSON配置文件 (*.json)|*.json|所有文件 (*.*)|*.*";
    }

    /// <summary>
    ///     设置导出数据容器
    /// </summary>
    public class SettingsExportData
    {
        /// <summary>
        ///     导出版本号，用于兼容性检查
        /// </summary>
        public string Version { get; set; } = "1.0";

        /// <summary>
        ///     导出时间
        /// </summary>
        public DateTime ExportTime { get; set; } = DateTime.Now;

        /// <summary>
        ///     应用程序版本
        /// </summary>
        public string AppVersion { get; set; } =
            Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0";

        /// <summary>
        ///     常规设置
        /// </summary>
        public GeneralConfig? General { get; set; }

        /// <summary>
        ///     数据库设置
        /// </summary>
        public DatabaseConfig? Database { get; set; }

        /// <summary>
        ///     界面设置
        /// </summary>
        public UISettingsConfig? UI { get; set; }

        /// <summary>
        ///     地图设置
        /// </summary>
        public MapSettingsConfig? Map { get; set; }

        /// <summary>
        ///     仪表盘设置
        /// </summary>
        public DashboardConfig? Dashboard { get; set; }

        /// <summary>
        ///     OCR设置
        /// </summary>
        public OcrConfig? OCR { get; set; }

        /// <summary>
        ///     导出设置
        /// </summary>
        public ExportConfig? Export { get; set; }

        /// <summary>
        ///     快捷键设置
        /// </summary>
        public ShortcutConfig? Shortcut { get; set; }
    }

    /// <summary>
    ///     导入验证结果
    /// </summary>
    public class ImportValidationResult
    {
        /// <summary>
        ///     是否验证通过
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        ///     错误信息列表
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        ///     验证通过的配置数据
        /// </summary>
        public SettingsExportData? ValidatedData { get; set; }
    }
}