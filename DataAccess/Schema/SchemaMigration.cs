using System;
using Microsoft.Data.Sqlite;

namespace GuiPiao.DataAccess.Schema;

/// <summary>单步 schema 迁移：从 Version-1 升到 Version。</summary>
public sealed class SchemaMigration
{
    public SchemaMigration(int version, string name, Action<SqliteConnection, SqliteTransaction> apply)
    {
        if (version < 1) throw new ArgumentOutOfRangeException(nameof(version));
        Version = version;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Apply = apply ?? throw new ArgumentNullException(nameof(apply));
    }

    public int Version { get; }
    public string Name { get; }
    public Action<SqliteConnection, SqliteTransaction> Apply { get; }
}
