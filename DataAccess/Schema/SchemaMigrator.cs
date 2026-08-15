using System;
using System.Collections.Generic;
using System.Linq;
using GuiPiao.Services;
using Microsoft.Data.Sqlite;

namespace GuiPiao.DataAccess.Schema;

/// <summary>
///     版本化 schema 迁移。
///     使用 SQLite <c>PRAGMA user_version</c> 记录当前版本；启动时只跑未执行步骤。
/// </summary>
public static class SchemaMigrator
{
    private static readonly Lazy<LogService> Log = new(() => new LogService());

    /// <summary>应用内期望的最新 schema 版本（= 已注册迁移的最大 Version）。</summary>
    public static int CurrentVersion => SchemaCatalog.Migrations.Max(m => m.Version);

    public static void Apply(SqliteConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        var migrations = SchemaCatalog.Migrations.OrderBy(m => m.Version).ToList();
        ValidateMigrationChain(migrations);

        var from = GetUserVersion(connection);
        if (from > CurrentVersion)
        {
            throw new InvalidOperationException(
                $"数据库 schema 版本 {from} 高于程序支持的 {CurrentVersion}，请升级客户端。");
        }

        if (from == CurrentVersion)
        {
            Log.Value.Info("SchemaMigrator", $"schema 已是最新版本 v{CurrentVersion}");
            return;
        }

        Log.Value.Info("SchemaMigrator", $"开始迁移 schema：v{from} → v{CurrentVersion}");

        foreach (var migration in migrations.Where(m => m.Version > from))
        {
            using var transaction = connection.BeginTransaction();
            try
            {
                Log.Value.Info("SchemaMigrator", $"执行 v{migration.Version}: {migration.Name}");
                migration.Apply(connection, transaction);
                SetUserVersion(connection, migration.Version, transaction);
                transaction.Commit();
                Log.Value.Info("SchemaMigrator", $"完成 v{migration.Version}");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Log.Value.Error("SchemaMigrator", $"迁移 v{migration.Version} 失败: {ex.Message}");
                throw;
            }
        }
    }

    public static int GetUserVersion(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA user_version;";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static void SetUserVersion(SqliteConnection connection, int version, SqliteTransaction transaction)
    {
        // PRAGMA user_version 不能可靠绑定参数，版本号来自内部常量
        using var cmd = connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = $"PRAGMA user_version = {version};";
        cmd.ExecuteNonQuery();
    }

    private static void ValidateMigrationChain(IReadOnlyList<SchemaMigration> migrations)
    {
        if (migrations.Count == 0)
            throw new InvalidOperationException("未注册任何 schema 迁移。");

        for (var i = 0; i < migrations.Count; i++)
        {
            var expected = i + 1;
            if (migrations[i].Version != expected)
                throw new InvalidOperationException(
                    $"迁移版本必须从 1 连续递增，期望 v{expected}，实际 v{migrations[i].Version}（{migrations[i].Name}）。");
        }
    }
}
