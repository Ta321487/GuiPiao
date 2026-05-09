using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using GuiPiao.Utils;
using Microsoft.Data.Sqlite;

namespace GuiPiao.DataAccess;

/// <summary>
///     数据库高危操作Repository - 包含危险的数据库操作
/// </summary>
public class DatabaseDangerousRepository
{
    private readonly string _connectionString = ConfigManager.Instance.DatabaseConnectionString;

    #region 清空数据操作

    /// <summary>
    ///     清空全部票务记录
    /// </summary>
    public async Task<int> ClearAllTrainRidesAsync()
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = "DELETE FROM train_ride_info;";
            return await connection.ExecuteAsync(sql);
        }
    }

    /// <summary>
    ///     清空全部日志记录
    /// </summary>
    public async Task<int> ClearAllLogsAsync()
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = "DELETE FROM system_log;";
            return await connection.ExecuteAsync(sql);
        }
    }

    /// <summary>
    ///     清空全部车站信息
    /// </summary>
    public async Task<int> ClearAllStationsAsync()
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = "DELETE FROM station_info;";
            return await connection.ExecuteAsync(sql);
        }
    }

    /// <summary>
    ///     获取票务记录数量
    /// </summary>
    public async Task<int> GetTrainRideCountAsync()
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = "SELECT COUNT(*) FROM train_ride_info;";
            return await connection.ExecuteScalarAsync<int>(sql);
        }
    }

    #endregion

    #region 表结构操作

    /// <summary>
    ///     获取所有表名
    /// </summary>
    public async Task<IEnumerable<string>> GetAllTableNamesAsync()
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';";
            return await connection.QueryAsync<string>(sql);
        }
    }

    /// <summary>
    ///     获取表的创建SQL
    /// </summary>
    public async Task<string> GetTableCreateSqlAsync(string tableName)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = "SELECT sql FROM sqlite_master WHERE type='table' AND name = @TableName;";
            return await connection.ExecuteScalarAsync<string>(sql, new { TableName = tableName });
        }
    }

    /// <summary>
    ///     获取表的索引创建SQL
    /// </summary>
    public async Task<IEnumerable<string>> GetTableIndexSqlAsync(string tableName)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = "SELECT sql FROM sqlite_master WHERE type='index' AND tbl_name = @TableName;";
            return await connection.QueryAsync<string>(sql, new { TableName = tableName });
        }
    }

    /// <summary>
    ///     获取表的所有列信息
    /// </summary>
    public async Task<IEnumerable<ColumnInfo>> GetTableColumnsAsync(string tableName)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = $"PRAGMA table_info({tableName});";
            return await connection.QueryAsync<ColumnInfo>(sql);
        }
    }

    #endregion

    #region 数据库重置操作

    /// <summary>
    ///     删除所有表（危险操作）
    /// </summary>
    public async Task DropAllTablesAsync()
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();

            var tables = await GetAllTableNamesAsync();

            await connection.ExecuteAsync("PRAGMA foreign_keys = OFF;");

            try
            {
                foreach (var table in tables)
                {
                    var dropSql = $"DROP TABLE IF EXISTS {table};";
                    await connection.ExecuteAsync(dropSql);
                }
            }
            finally
            {
                await connection.ExecuteAsync("PRAGMA foreign_keys = ON;");
            }
        }
    }

    /// <summary>
    ///     创建初始表结构
    /// </summary>
    public async Task CreateInitialSchemaAsync()
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();

            // 创建票务信息表
            var createTrainRideTable = @"
                    CREATE TABLE IF NOT EXISTS train_ride_info (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        ticket_number TEXT,
                        check_in_location TEXT,
                        depart_station TEXT,
                        train_no TEXT,
                        arrive_station TEXT,
                        depart_station_pinyin TEXT,
                        arrive_station_pinyin TEXT,
                        depart_date TEXT,
                        depart_time TEXT,
                        coach_no TEXT,
                        seat_no TEXT,
                        money TEXT,
                        seat_type TEXT,
                        additional_info TEXT,
                        ticket_purpose TEXT,
                        ticket_modification_type TEXT,
                        ticket_type_flags TEXT,
                        payment_channel_flags TEXT,
                        hint TEXT,
                        depart_station_code TEXT,
                        arrive_station_code TEXT
                    );
                ";
            await connection.ExecuteAsync(createTrainRideTable);

            // 创建车站信息表
            var createStationTable = @"
                    CREATE TABLE IF NOT EXISTS station_info (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        station_name TEXT NOT NULL,
                        province TEXT,
                        city TEXT,
                        district TEXT,
                        station_code TEXT UNIQUE,
                        station_pinyin TEXT,
                        station_level TEXT,
                        railway_bureau TEXT,
                        longitude REAL,
                        latitude REAL
                    );
                ";
            await connection.ExecuteAsync(createStationTable);

            // 创建日志表
            var createLogTable = @"
                    CREATE TABLE IF NOT EXISTS system_log (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        time TEXT,
                        level INTEGER,
                        module TEXT,
                        content TEXT,
                        created_at TEXT
                    );
                ";
            await connection.ExecuteAsync(createLogTable);
        }
    }

    #endregion

    #region 导出SQL功能

    /// <summary>
    ///     生成完整的数据库结构SQL
    /// </summary>
    public async Task<string> GenerateSchemaSqlAsync()
    {
        var sqlBuilder = new StringBuilder();

        sqlBuilder.AppendLine("-- GuiPiao Database Schema");
        sqlBuilder.AppendLine($"-- Generated at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sqlBuilder.AppendLine("-- ============================================");
        sqlBuilder.AppendLine();

        // 获取所有表
        var tables = await GetAllTableNamesAsync();

        foreach (var table in tables)
        {
            // 表创建SQL
            var createSql = await GetTableCreateSqlAsync(table);
            if (!string.IsNullOrEmpty(createSql))
            {
                sqlBuilder.AppendLine($"-- Table: {table}");
                sqlBuilder.AppendLine(createSql + ";");
                sqlBuilder.AppendLine();

                // 索引SQL
                var indexSqls = await GetTableIndexSqlAsync(table);
                foreach (var indexSql in indexSqls)
                    if (!string.IsNullOrEmpty(indexSql))
                        sqlBuilder.AppendLine(indexSql + ";");

                if (indexSqls.Any()) sqlBuilder.AppendLine();
            }
        }

        return sqlBuilder.ToString();
    }

    /// <summary>
    ///     生成表数据导出SQL（可选）
    /// </summary>
    public async Task<string> GenerateTableDataSqlAsync(string tableName, int limit = 1000)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();

            // 获取列名
            var columns = await GetTableColumnsAsync(tableName);
            var columnNames = string.Join(", ", columns.Select(c => c.Name));

            // 查询数据
            var selectSql = $"SELECT * FROM {tableName} LIMIT {limit};";
            var data = await connection.QueryAsync(selectSql);

            var sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine($"-- Data for table: {tableName}");

            foreach (var row in data)
            {
                var values = new List<string>();
                foreach (var col in columns)
                {
                    var value = ((IDictionary<string, object>)row)[col.Name];
                    if (value == null)
                        values.Add("NULL");
                    else
                        values.Add($"'{value.ToString().Replace("'", "''")}'");
                }

                sqlBuilder.AppendLine($"INSERT INTO {tableName} ({columnNames}) VALUES ({string.Join(", ", values)});");
            }

            return sqlBuilder.ToString();
        }
    }

    #endregion
}

/// <summary>
///     列信息
/// </summary>
public class ColumnInfo
{
    public int Cid { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int NotNull { get; set; }
    public string DefaultValue { get; set; } = string.Empty;
    public int Pk { get; set; }
}