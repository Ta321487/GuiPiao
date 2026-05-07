using GuiPiao.DataAccess;
using GuiPiao.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GuiPiao.ViewModel.TrainTicketForm
{
    /// <summary>
    /// 车站查询服务 - 提供车站信息查询功能
    /// </summary>
    public class StationQueryService
    {
        private readonly StationRepository _stationRepository;

        public StationQueryService(StationRepository stationRepository)
        {
            _stationRepository = stationRepository;
        }

        /// <summary>
        /// 根据车站名称查询车站信息
        /// </summary>
        /// <param name="stationName">车站名称（不含"站"字）</param>
        /// <returns>车站信息，未找到返回null</returns>
        public async Task<StationInfo?> QueryStationAsync(string stationName)
        {
            if (string.IsNullOrWhiteSpace(stationName))
                return null;

            try
            {
                // 添加"站"字进行查询（因为数据库中存储的是"大连站"这样的完整名称）
                var fullName = stationName.EndsWith("站") ? stationName : $"{stationName}站";
                var station = await _stationRepository.GetStationByNameAsync(fullName);
                
                if (station == null)
                {
                    // 尝试直接查询（如果用户输入了完整名称）
                    station = await _stationRepository.GetStationByNameAsync(stationName);
                }

                return station;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[StationQueryService] 查询车站失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 根据关键词搜索车站
        /// </summary>
        /// <param name="keyword">关键词</param>
        /// <returns>车站列表</returns>
        public async Task<List<StationInfo>> SearchStationsAsync(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return new List<StationInfo>();

            try
            {
                var stations = await _stationRepository.SearchStationsAsync(keyword);
                return new List<StationInfo>(stations);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[StationQueryService] 搜索车站失败: {ex.Message}");
                return new List<StationInfo>();
            }
        }

        /// <summary>
        /// 智能搜索车站（支持拼音首字母、拼音、名称匹配）
        /// </summary>
        /// <param name="keyword">关键词</param>
        /// <returns>去重后的车站名称列表（不含"站"字，最多10条）</returns>
        public async Task<List<string>> SmartSearchStationNamesAsync(string keyword)
        {
            System.Diagnostics.Debug.WriteLine($"[StationQueryService] [DEBUG] SmartSearchStationNamesAsync 开始，关键词: '{keyword}'");
            
            if (string.IsNullOrWhiteSpace(keyword) || keyword.Length < 1)
            {
                System.Diagnostics.Debug.WriteLine("[StationQueryService] [DEBUG] 关键词为空或长度小于1，返回空列表");
                return new List<string>();
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"[StationQueryService] [DEBUG] 调用 _stationRepository.SmartSearchStationsAsync");
                var stations = await _stationRepository.SmartSearchStationsAsync(keyword);
                System.Diagnostics.Debug.WriteLine($"[StationQueryService] [DEBUG] 数据库返回 {stations.Count()} 个车站");
                
                // 去重并去掉"站"字
                var names = stations
                    .Select(s => RemoveStationSuffix(s.StationName))
                    .Where(name => !string.IsNullOrEmpty(name))
                    .Distinct()
                    .Take(10)
                    .ToList();
                
                System.Diagnostics.Debug.WriteLine($"[StationQueryService] [DEBUG] 处理后返回 {names.Count} 个名称: {string.Join(", ", names)}");
                return names;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[StationQueryService] [DEBUG] 智能搜索车站失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[StationQueryService] [DEBUG] 异常详情: {ex.StackTrace}");
                return new List<string>();
            }
        }

        /// <summary>
        /// 获取所有车站名称列表（用于自动完成）
        /// </summary>
        /// <returns>车站名称列表（不含"站"字）</returns>
        public async Task<List<string>> GetStationNamesAsync()
        {
            try
            {
                var stations = await _stationRepository.GetAllStationsAsync();
                var names = new List<string>();
                foreach (var station in stations)
                {
                    // 去掉"站"字返回
                    var name = RemoveStationSuffix(station.StationName);
                    if (!string.IsNullOrEmpty(name))
                    {
                        names.Add(name);
                    }
                }
                return names;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[StationQueryService] 获取车站列表失败: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// 去掉车站名称末尾的"站"字
        /// </summary>
        public static string RemoveStationSuffix(string? stationName)
        {
            if (string.IsNullOrEmpty(stationName))
                return string.Empty;
            
            return stationName.EndsWith("站") 
                ? stationName.Substring(0, stationName.Length - 1) 
                : stationName;
        }

        /// <summary>
        /// 添加"站"字到车站名称
        /// </summary>
        public static string AddStationSuffix(string? stationName)
        {
            if (string.IsNullOrEmpty(stationName))
                return string.Empty;
            
            return stationName.EndsWith("站") 
                ? stationName 
                : $"{stationName}站";
        }
    }
}
