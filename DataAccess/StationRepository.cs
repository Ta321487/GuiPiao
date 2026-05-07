using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using GuiPiao.Model;
using GuiPiao.Utils;
using Microsoft.Data.Sqlite;

namespace GuiPiao.DataAccess;

public class StationRepository
{
    private readonly string _connectionString = ConfigManager.Instance.DatabaseConnectionString;

    public async Task<int> AddStationAsync(StationInfo station)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = @"
                    INSERT INTO station_info (station_name, province, city, district, station_code, station_pinyin, station_level, railway_bureau, longitude, latitude)
                    VALUES (@StationName, @Province, @City, @District, @StationCode, @StationPinyin, @StationLevel, @RailwayBureau, @Longitude, @Latitude);
                    SELECT last_insert_rowid();
                ";
            return await connection.QuerySingleAsync<int>(sql, station);
        }
    }

    public async Task<IEnumerable<StationInfo>> GetAllStationsAsync()
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = @"
                    SELECT 
                        id AS Id,
                        station_name AS StationName,
                        province AS Province,
                        city AS City,
                        district AS District,
                        station_code AS StationCode,
                        station_pinyin AS StationPinyin,
                        station_level AS StationLevel,
                        railway_bureau AS RailwayBureau,
                        longitude AS Longitude,
                        latitude AS Latitude
                    FROM station_info
                ";
            return await connection.QueryAsync<StationInfo>(sql);
        }
    }

    public async Task<StationInfo> GetStationByCodeAsync(string code)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = @"
                    SELECT 
                        id AS Id,
                        station_name AS StationName,
                        province AS Province,
                        city AS City,
                        district AS District,
                        station_code AS StationCode,
                        station_pinyin AS StationPinyin,
                        station_level AS StationLevel,
                        railway_bureau AS RailwayBureau,
                        longitude AS Longitude,
                        latitude AS Latitude
                    FROM station_info 
                    WHERE station_code = @Code
                ";
            return await connection.QuerySingleOrDefaultAsync<StationInfo>(sql, new { Code = code });
        }
    }

    public async Task<StationInfo> GetStationByNameAsync(string name)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = @"
                    SELECT 
                        id AS Id,
                        station_name AS StationName,
                        province AS Province,
                        city AS City,
                        district AS District,
                        station_code AS StationCode,
                        station_pinyin AS StationPinyin,
                        station_level AS StationLevel,
                        railway_bureau AS RailwayBureau,
                        longitude AS Longitude,
                        latitude AS Latitude
                    FROM station_info 
                    WHERE station_name = @Name
                ";
            return await connection.QuerySingleOrDefaultAsync<StationInfo>(sql, new { Name = name });
        }
    }

    public async Task<int> UpdateStationAsync(StationInfo station)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = @"
                    UPDATE station_info
                    SET station_name = @StationName, province = @Province, city = @City, district = @District,
                        station_pinyin = @StationPinyin, station_level = @StationLevel, railway_bureau = @RailwayBureau,
                        longitude = @Longitude, latitude = @Latitude
                    WHERE station_code = @StationCode;
                ";
            return await connection.ExecuteAsync(sql, station);
        }
    }

    public async Task<int> DeleteStationAsync(string code)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = "DELETE FROM station_info WHERE station_code = @Code";
            return await connection.ExecuteAsync(sql, new { Code = code });
        }
    }

    public async Task<IEnumerable<StationInfo>> SearchStationsAsync(string keyword)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = @"
                    SELECT 
                        id AS Id,
                        station_name AS StationName,
                        province AS Province,
                        city AS City,
                        district AS District,
                        station_code AS StationCode,
                        station_pinyin AS StationPinyin,
                        station_level AS StationLevel,
                        railway_bureau AS RailwayBureau,
                        longitude AS Longitude,
                        latitude AS Latitude
                    FROM station_info
                    WHERE station_name LIKE @Keyword OR station_code LIKE @Keyword OR station_pinyin LIKE @Keyword
                ";
            return await connection.QueryAsync<StationInfo>(sql, new { Keyword = $"%{keyword}%" });
        }
    }

    /// <summary>
    ///     智能搜索车站（支持拼音首字母、拼音、名称匹配）
    /// </summary>
    /// <param name="keyword">关键词</param>
    /// <returns>按匹配优先级排序的车站列表</returns>
    public async Task<IEnumerable<StationInfo>> SmartSearchStationsAsync(string keyword)
    {
        Debug.WriteLine($"[StationRepository] [DEBUG] SmartSearchStationsAsync 开始，关键词: '{keyword}'");
        Debug.WriteLine($"[StationRepository] [DEBUG] 连接字符串: {_connectionString}");

        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            Debug.WriteLine("[StationRepository] [DEBUG] 数据库连接已打开");

            // 优先匹配拼音首字母（如：dl 匹配 dalian）
            // 其次匹配拼音（如：dalian 匹配 dalian）
            // 最后匹配名称（如：大 匹配 大连）
            var sql = @"
                    SELECT 
                        id AS Id,
                        station_name AS StationName,
                        province AS Province,
                        city AS City,
                        district AS District,
                        station_code AS StationCode,
                        station_pinyin AS StationPinyin,
                        station_level AS StationLevel,
                        railway_bureau AS RailwayBureau,
                        longitude AS Longitude,
                        latitude AS Latitude
                    FROM station_info
                    WHERE station_pinyin LIKE @PinyinPrefix 
                       OR station_pinyin LIKE @PinyinLike
                       OR station_name LIKE @NameLike
                    ORDER BY 
                        CASE 
                            WHEN station_pinyin LIKE @PinyinPrefix THEN 1
                            WHEN station_pinyin LIKE @PinyinLike THEN 2
                            WHEN station_name LIKE @NameLike THEN 3
                            ELSE 4
                        END, 
                        station_name
                    LIMIT 10
                ";

            var parameters = new
            {
                PinyinPrefix = $"{keyword}%",
                PinyinLike = $"%{keyword}%",
                NameLike = $"%{keyword}%"
            };

            Debug.WriteLine(
                $"[StationRepository] [DEBUG] SQL参数: PinyinPrefix='{parameters.PinyinPrefix}', PinyinLike='{parameters.PinyinLike}', NameLike='{parameters.NameLike}'");

            var result = await connection.QueryAsync<StationInfo>(sql, parameters);
            var stationList = result.ToList();
            Debug.WriteLine($"[StationRepository] [DEBUG] 查询完成，返回 {stationList.Count} 个车站");

            foreach (var station in stationList)
                Debug.WriteLine($"[StationRepository] [DEBUG] - {station.StationName} ({station.StationPinyin})");

            return stationList;
        }
    }
}