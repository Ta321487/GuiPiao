using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GuiPiao.Utils;

/// <summary>
///     JSON配置管理器 - 通用配置持久化服务
/// </summary>
public class JsonConfigManager
{
    private static readonly Lazy<JsonConfigManager> _instance = new(() => new JsonConfigManager());

    private readonly string _configDirectory;

    private JsonConfigManager()
    {
        // 配置文件存放在程序目录下的 Config 文件夹
        _configDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");
        EnsureConfigDirectoryExists();
    }

    public static JsonConfigManager Instance => _instance.Value;

    /// <summary>
    ///     确保配置目录存在
    /// </summary>
    private void EnsureConfigDirectoryExists()
    {
        if (!Directory.Exists(_configDirectory)) Directory.CreateDirectory(_configDirectory);
    }

    /// <summary>
    ///     获取配置文件完整路径
    /// </summary>
    public string GetConfigFilePath(string fileName)
    {
        return Path.Combine(_configDirectory, fileName);
    }

    /// <summary>
    ///     获取JSON序列化设置（包含类型信息）
    /// </summary>
    private JsonSerializerSettings GetSerializerSettings()
    {
        return new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            TypeNameHandling = TypeNameHandling.Auto,
            Converters = new List<JsonConverter> { new StringEnumConverter() }
        };
    }

    /// <summary>
    ///     加载配置
    /// </summary>
    /// <typeparam name="T">配置类型</typeparam>
    /// <param name="fileName">配置文件名</param>
    /// <param name="defaultValue">默认值（当文件不存在时返回）</param>
    /// <returns>配置对象</returns>
    public T LoadConfig<T>(string fileName, T defaultValue) where T : class
    {
        try
        {
            var filePath = GetConfigFilePath(fileName);

            if (!File.Exists(filePath)) return defaultValue;

            var json = File.ReadAllText(filePath);
            var settings = GetSerializerSettings();
            var config = JsonConvert.DeserializeObject<T>(json, settings);
            return config ?? defaultValue;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"加载配置失败: {ex.Message}");
            return defaultValue;
        }
    }

    /// <summary>
    ///     保存配置
    /// </summary>
    /// <typeparam name="T">配置类型</typeparam>
    /// <param name="fileName">配置文件名</param>
    /// <param name="config">配置对象</param>
    public void SaveConfig<T>(string fileName, T config) where T : class
    {
        try
        {
            EnsureConfigDirectoryExists();
            var filePath = GetConfigFilePath(fileName);

            var settings = GetSerializerSettings();
            var json = JsonConvert.SerializeObject(config, settings);
            Debug.WriteLine($"[JsonConfigManager.SaveConfig] Saving to {filePath}, json length: {json.Length}");
            // 查找 CustomTimePeriods 字段
            var customPeriodsIndex = json.IndexOf("CustomTimePeriods");
            if (customPeriodsIndex >= 0)
            {
                var endIndex = json.IndexOf("]", customPeriodsIndex);
                if (endIndex > 0)
                {
                    var customPeriodsJson = json.Substring(customPeriodsIndex, endIndex - customPeriodsIndex + 1);
                    Debug.WriteLine($"[JsonConfigManager.SaveConfig] CustomTimePeriods: {customPeriodsJson}");
                }
            }
            else
            {
                Debug.WriteLine("[JsonConfigManager.SaveConfig] CustomTimePeriods not found in JSON!");
            }

            File.WriteAllText(filePath, json);
            Debug.WriteLine("[JsonConfigManager.SaveConfig] File saved successfully");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"保存配置失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    ///     检查配置文件是否存在
    /// </summary>
    public bool ConfigExists(string fileName)
    {
        var filePath = GetConfigFilePath(fileName);
        return File.Exists(filePath);
    }

    /// <summary>
    ///     更新配置中的单个字段（线程安全）
    /// </summary>
    /// <typeparam name="T">配置类型</typeparam>
    /// <param name="fileName">配置文件名</param>
    /// <param name="defaultValue">默认值（当文件不存在时使用）</param>
    /// <param name="updateAction">更新配置的委托</param>
    public void UpdateConfig<T>(string fileName, T defaultValue, Action<T> updateAction) where T : class
    {
        lock (this)
        {
            // 1. 重新从文件加载最新配置
            var config = LoadConfig(fileName, defaultValue);

            // 2. 执行更新操作
            updateAction(config);

            // 3. 保存回文件
            SaveConfig(fileName, config);
        }
    }
}