using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Microsoft.Data.Sqlite;

namespace GuiPiao.Utils;

/// <summary>
///     启动时数据库损坏的恢复：从自动备份还原，或重建空库。
/// </summary>
public static class DatabaseRecovery
{
    public static string GetDatabaseFilePath(string connectionString)
    {
        const string prefix = "Data Source=";
        if (string.IsNullOrWhiteSpace(connectionString))
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "guipiao.db");

        foreach (var part in connectionString.Split(';'))
        {
            var trimmed = part.Trim();
            if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return trimmed[prefix.Length..].Trim().Trim('"');
        }

        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "guipiao.db");
    }

    public static bool IsSqliteDatabaseFile(string path)
    {
        try
        {
            if (!File.Exists(path) || new FileInfo(path).Length < 16)
                return false;

            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var header = new byte[16];
            if (fs.Read(header, 0, 16) < 16)
                return false;
            return Encoding.ASCII.GetString(header).StartsWith("SQLite format 3", StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    public static string? FindLatestValidBackup(string backupDir)
    {
        if (!Directory.Exists(backupDir))
            return null;

        var candidates = Directory.EnumerateFiles(backupDir, "database_backup_*.*")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .ToList();

        var extractRoot = Path.Combine(Path.GetTempPath(), "GuiPiaoDbRestore_" + Guid.NewGuid().ToString("N"));
        try
        {
            foreach (var candidate in candidates)
            {
                try
                {
                    if (candidate.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
                    {
                        if (IsSqliteDatabaseFile(candidate))
                            return candidate;
                        continue;
                    }

                    if (!candidate.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (Directory.Exists(extractRoot))
                        Directory.Delete(extractRoot, true);
                    Directory.CreateDirectory(extractRoot);
                    ZipFile.ExtractToDirectory(candidate, extractRoot);

                    var db = Directory.EnumerateFiles(extractRoot, "*.db", SearchOption.AllDirectories)
                        .FirstOrDefault(IsSqliteDatabaseFile);
                    if (db == null)
                        continue;

                    // 把解压结果落到可复用的临时文件，调用方负责 Copy
                    var sticky = Path.Combine(Path.GetTempPath(),
                        "GuiPiao_restore_" + Path.GetFileNameWithoutExtension(candidate) + ".db");
                    File.Copy(db, sticky, true);
                    return sticky;
                }
                catch
                {
                    // 尝试下一个备份
                }
            }
        }
        finally
        {
            try
            {
                if (Directory.Exists(extractRoot))
                    Directory.Delete(extractRoot, true);
            }
            catch
            {
                // ignore
            }
        }

        return null;
    }

    public static void QuarantineBrokenFile(string dbPath)
    {
        if (!File.Exists(dbPath))
            return;

        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var broken = dbPath + $".broken_{stamp}";
        try
        {
            if (File.Exists(broken))
                File.Delete(broken);
            File.Move(dbPath, broken);
        }
        catch
        {
            try
            {
                File.Copy(dbPath, broken, true);
                File.Delete(dbPath);
            }
            catch
            {
                // ignore
            }
        }
    }

    public static bool TryRestoreFromBackup(string dbPath, string backupDir, out string? usedBackup)
    {
        usedBackup = FindLatestValidBackup(backupDir);
        if (usedBackup == null)
            return false;

        QuarantineBrokenFile(dbPath);
        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.Copy(usedBackup, dbPath, true);
        return IsSqliteDatabaseFile(dbPath);
    }

    public static void CreateEmptyDatabaseFile(string dbPath)
    {
        QuarantineBrokenFile(dbPath);
        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        // 建一个可打开的空库；表结构由 Database.Initialize/CreateTables 补齐
        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();
    }
}
