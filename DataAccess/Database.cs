using System;
using System.IO;
using GuiPiao.DataAccess.Schema;
using GuiPiao.Services;
using GuiPiao.Utils;
using Microsoft.Data.Sqlite;

namespace GuiPiao.DataAccess;

/// <summary>
///     数据库入口：打开连接并执行版本化 schema 迁移。
///     表结构变更请追加 <see cref="SchemaCatalog"/> 中的迁移步骤，勿在此堆 ALTER。
/// </summary>
public static class Database
{
    private static readonly Lazy<LogService> _logService = new(() => new LogService());

    public static void Initialize()
    {
        try
        {
            var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "guipiao.db");
            var dbDir = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(dbDir) && !Directory.Exists(dbDir))
            {
                Directory.CreateDirectory(dbDir);
                _logService.Value.Info("Database", $"创建数据库目录: {dbDir}");
            }

            using var connection = new SqliteConnection(ConfigManager.Instance.DatabaseConnectionString);
            connection.Open();

            SchemaMigrator.Apply(connection);
            EnsureJournalWal(connection);

            _logService.Value.Info("Database",
                $"数据库就绪，schema v{SchemaMigrator.GetUserVersion(connection)}（目标 v{SchemaMigrator.CurrentVersion}）");
        }
        catch (Exception ex)
        {
            _logService.Value.Error("Database", $"数据库初始化失败: {ex.Message}");
            throw;
        }
    }

    private static void EnsureJournalWal(SqliteConnection connection)
    {
        using var cmd = new SqliteCommand("PRAGMA journal_mode=WAL;", connection);
        var mode = cmd.ExecuteScalar()?.ToString();
        _logService.Value.Info("Database", $"SQLite journal_mode={mode}");
    }

    public static SqliteConnection GetConnection()
    {
        return new SqliteConnection(ConfigManager.Instance.DatabaseConnectionString);
    }
}
