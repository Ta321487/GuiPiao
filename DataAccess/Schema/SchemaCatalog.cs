using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace GuiPiao.DataAccess.Schema;

/// <summary>
///     已注册的 schema 迁移清单。新增变更：追加 Migration_00N，勿改已发布步骤。
/// </summary>
public static class SchemaCatalog
{
    public static IReadOnlyList<SchemaMigration> Migrations { get; } = new[]
    {
        new SchemaMigration(1, "baseline_core", MigrateTo1Baseline),
        new SchemaMigration(2, "sync_foundation", MigrateTo2SyncFoundation)
    };

    /// <summary>
    ///     v1：同步功能之前的核心表结构（含 status / arrive_* / is_default）。
    ///     对历史库（user_version=0）使用 IF NOT EXISTS / AddColumnIfMissing，避免重复失败。
    /// </summary>
    private static void MigrateTo1Baseline(SqliteConnection connection, SqliteTransaction tx)
    {
        SchemaSql.Execute(connection, tx, @"
            CREATE TABLE IF NOT EXISTS station_info (
                id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                station_name TEXT,
                province TEXT,
                city TEXT,
                district TEXT,
                station_code TEXT,
                station_pinyin TEXT,
                station_level INTEGER,
                railway_bureau TEXT,
                longitude TEXT,
                latitude TEXT
            );
            CREATE INDEX IF NOT EXISTS idx_station_name ON station_info (station_name);
            CREATE INDEX IF NOT EXISTS idx_station_code ON station_info (station_code);
            CREATE INDEX IF NOT EXISTS idx_station_pinyin ON station_info (station_pinyin);
        ");

        SchemaSql.Execute(connection, tx, @"
            CREATE TABLE IF NOT EXISTS train_ride_info (
                id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
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
                money REAL,
                seat_type TEXT,
                additional_info TEXT,
                ticket_purpose TEXT,
                ticket_modification_type TEXT,
                ticket_type_flags INTEGER DEFAULT 0,
                payment_channel_flags INTEGER DEFAULT 0,
                hint TEXT,
                depart_station_code TEXT,
                arrive_station_code TEXT,
                status INTEGER DEFAULT 0,
                arrive_time TEXT,
                arrive_day_offset INTEGER DEFAULT 0,
                FOREIGN KEY (arrive_station_code) REFERENCES station_info (station_code) ON DELETE CASCADE ON UPDATE CASCADE,
                FOREIGN KEY (depart_station_code) REFERENCES station_info (station_code) ON DELETE CASCADE ON UPDATE CASCADE
            );
        ");

        // 表已存在时 CREATE 不会改结构：把 v1 列补齐后再建索引
        SchemaSql.AddColumnIfMissing(connection, tx, "train_ride_info", "ticket_number", "ticket_number TEXT");
        SchemaSql.AddColumnIfMissing(connection, tx, "train_ride_info", "check_in_location", "check_in_location TEXT");
        SchemaSql.AddColumnIfMissing(connection, tx, "train_ride_info", "depart_station", "depart_station TEXT");
        SchemaSql.AddColumnIfMissing(connection, tx, "train_ride_info", "train_no", "train_no TEXT");
        SchemaSql.AddColumnIfMissing(connection, tx, "train_ride_info", "arrive_station", "arrive_station TEXT");
        SchemaSql.AddColumnIfMissing(connection, tx, "train_ride_info", "depart_station_pinyin",
            "depart_station_pinyin TEXT");
        SchemaSql.AddColumnIfMissing(connection, tx, "train_ride_info", "arrive_station_pinyin",
            "arrive_station_pinyin TEXT");
        SchemaSql.AddColumnIfMissing(connection, tx, "train_ride_info", "depart_date", "depart_date TEXT");
        SchemaSql.AddColumnIfMissing(connection, tx, "train_ride_info", "depart_time", "depart_time TEXT");
        SchemaSql.AddColumnIfMissing(connection, tx, "train_ride_info", "coach_no", "coach_no TEXT");
        SchemaSql.AddColumnIfMissing(connection, tx, "train_ride_info", "seat_no", "seat_no TEXT");
        SchemaSql.AddColumnIfMissing(connection, tx, "train_ride_info", "money", "money REAL");
        SchemaSql.AddColumnIfMissing(connection, tx, "train_ride_info", "seat_type", "seat_type TEXT");
        SchemaSql.AddColumnIfMissing(connection, tx, "train_ride_info", "additional_info", "additional_info TEXT");
        SchemaSql.AddColumnIfMissing(connection, tx, "train_ride_info", "ticket_purpose", "ticket_purpose TEXT");
        SchemaSql.AddColumnIfMissing(connection, tx, "train_ride_info", "ticket_modification_type",
            "ticket_modification_type TEXT");
        SchemaSql.AddColumnIfMissing(connection, tx, "train_ride_info", "ticket_type_flags",
            "ticket_type_flags INTEGER DEFAULT 0");
        SchemaSql.AddColumnIfMissing(connection, tx, "train_ride_info", "payment_channel_flags",
            "payment_channel_flags INTEGER DEFAULT 0");
        SchemaSql.AddColumnIfMissing(connection, tx, "train_ride_info", "hint", "hint TEXT");
        SchemaSql.AddColumnIfMissing(connection, tx, "train_ride_info", "depart_station_code",
            "depart_station_code TEXT");
        SchemaSql.AddColumnIfMissing(connection, tx, "train_ride_info", "arrive_station_code",
            "arrive_station_code TEXT");
        SchemaSql.AddColumnIfMissing(connection, tx, "train_ride_info", "status",
            "status INTEGER DEFAULT 0");
        SchemaSql.AddColumnIfMissing(connection, tx, "train_ride_info", "arrive_time",
            "arrive_time TEXT");
        SchemaSql.AddColumnIfMissing(connection, tx, "train_ride_info", "arrive_day_offset",
            "arrive_day_offset INTEGER DEFAULT 0");

        SchemaSql.Execute(connection, tx, @"
            CREATE INDEX IF NOT EXISTS idx_depart_station_code ON train_ride_info (depart_station_code);
            CREATE INDEX IF NOT EXISTS idx_arrive_station_code ON train_ride_info (arrive_station_code);
            CREATE INDEX IF NOT EXISTS idx_train_no_date ON train_ride_info (train_no, depart_date);
            CREATE INDEX IF NOT EXISTS idx_depart_station_date ON train_ride_info (depart_station, depart_date);
            CREATE INDEX IF NOT EXISTS idx_status ON train_ride_info (status);
        ");

        SchemaSql.Execute(connection, tx, @"
            CREATE TABLE IF NOT EXISTS system_log (
                id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                time TEXT NOT NULL,
                level INTEGER NOT NULL DEFAULT 1,
                module TEXT,
                content TEXT NOT NULL,
                created_at TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_log_time ON system_log (time);
            CREATE INDEX IF NOT EXISTS idx_log_level ON system_log (level);
            CREATE INDEX IF NOT EXISTS idx_log_module ON system_log (module);
            CREATE INDEX IF NOT EXISTS idx_log_created_at ON system_log (created_at);
        ");

        SchemaSql.Execute(connection, tx, @"
            CREATE TABLE IF NOT EXISTS ticket_tag (
                id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                color TEXT,
                text_color TEXT,
                sort_order INTEGER DEFAULT 0,
                is_default INTEGER DEFAULT 0,
                created_at TEXT
            );
            CREATE INDEX IF NOT EXISTS idx_tag_sort_order ON ticket_tag (sort_order);
        ");
        SchemaSql.AddColumnIfMissing(connection, tx, "ticket_tag", "is_default",
            "is_default INTEGER DEFAULT 0");

        SchemaSql.Execute(connection, tx, @"
            CREATE TABLE IF NOT EXISTS train_ride_tag (
                id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                train_ride_id INTEGER NOT NULL,
                tag_id INTEGER NOT NULL,
                created_at TEXT,
                FOREIGN KEY (train_ride_id) REFERENCES train_ride_info (id) ON DELETE CASCADE,
                FOREIGN KEY (tag_id) REFERENCES ticket_tag (id) ON DELETE CASCADE,
                UNIQUE(train_ride_id, tag_id)
            );
            CREATE INDEX IF NOT EXISTS idx_ride_tag_ride_id ON train_ride_tag (train_ride_id);
            CREATE INDEX IF NOT EXISTS idx_ride_tag_tag_id ON train_ride_tag (tag_id);
        ");

        SchemaSql.NormalizeRideDateTimes(connection, tx);
    }

    /// <summary>v2：多端同步底座（身份列、变更日志、配对、冲突箱、WAL）。</summary>
    private static void MigrateTo2SyncFoundation(SqliteConnection connection, SqliteTransaction tx)
    {
        SchemaSql.AddColumnIfMissing(connection, tx, "train_ride_info", "sync_id", "sync_id TEXT");
        SchemaSql.AddColumnIfMissing(connection, tx, "train_ride_info", "updated_at", "updated_at TEXT");
        SchemaSql.AddColumnIfMissing(connection, tx, "train_ride_info", "deleted_at", "deleted_at TEXT");

        SchemaSql.AddColumnIfMissing(connection, tx, "ticket_tag", "sync_id", "sync_id TEXT");
        SchemaSql.AddColumnIfMissing(connection, tx, "ticket_tag", "updated_at", "updated_at TEXT");
        SchemaSql.AddColumnIfMissing(connection, tx, "ticket_tag", "deleted_at", "deleted_at TEXT");

        SchemaSql.Execute(connection, tx, @"
            CREATE UNIQUE INDEX IF NOT EXISTS idx_train_ride_sync_id
                ON train_ride_info(sync_id) WHERE sync_id IS NOT NULL AND sync_id != '';
            CREATE INDEX IF NOT EXISTS idx_train_ride_updated_at ON train_ride_info(updated_at);
            CREATE INDEX IF NOT EXISTS idx_train_ride_deleted_at ON train_ride_info(deleted_at);
            CREATE UNIQUE INDEX IF NOT EXISTS idx_ticket_tag_sync_id
                ON ticket_tag(sync_id) WHERE sync_id IS NOT NULL AND sync_id != '';
        ");

        SchemaSql.Execute(connection, tx, @"
            CREATE TABLE IF NOT EXISTS sync_change (
                change_id TEXT NOT NULL PRIMARY KEY,
                entity TEXT NOT NULL,
                sync_id TEXT NOT NULL,
                op TEXT NOT NULL,
                payload TEXT,
                updated_at TEXT NOT NULL,
                seq INTEGER NOT NULL UNIQUE,
                device_id TEXT
            );
            CREATE INDEX IF NOT EXISTS idx_sync_change_seq ON sync_change (seq);
            CREATE INDEX IF NOT EXISTS idx_sync_change_entity_sync ON sync_change (entity, sync_id);

            CREATE TABLE IF NOT EXISTS sync_pairing_code (
                id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                code_hash TEXT NOT NULL,
                created_at TEXT NOT NULL,
                expires_at TEXT NOT NULL,
                consumed_at TEXT
            );
            CREATE INDEX IF NOT EXISTS idx_sync_pairing_code_hash ON sync_pairing_code (code_hash);

            CREATE TABLE IF NOT EXISTS sync_paired_device (
                device_id TEXT NOT NULL PRIMARY KEY,
                device_name TEXT NOT NULL,
                token_hash TEXT NOT NULL,
                created_at TEXT NOT NULL,
                last_seen_at TEXT,
                revoked INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS sync_conflict (
                id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                entity TEXT NOT NULL,
                sync_id TEXT NOT NULL,
                field TEXT NOT NULL,
                local_value TEXT,
                remote_value TEXT,
                local_updated_at TEXT,
                remote_updated_at TEXT,
                created_at TEXT NOT NULL,
                resolved_at TEXT
            );
            CREATE INDEX IF NOT EXISTS idx_sync_conflict_open ON sync_conflict (resolved_at, entity, sync_id);
        ");

        SchemaSql.BackfillSyncIds(connection, tx, "train_ride_info");
        SchemaSql.BackfillSyncIds(connection, tx, "ticket_tag");

        // journal_mode 在部分环境下不能放在显式事务里；提交后由 Database 再设一次亦可。
        // 这里先尝试；失败不阻断（外层仍设 WAL）。
        try
        {
            SchemaSql.Execute(connection, tx, "PRAGMA journal_mode=WAL;");
        }
        catch
        {
            // ignore — Database.Initialize 会兜底
        }
    }
}
