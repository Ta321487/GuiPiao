namespace GuiPiao.Mobile;

/// <summary>将未处理异常写入 AppData，便于真机闪退后用 adb 拉取。</summary>
public static class CrashLog
{
    private static readonly object Gate = new();

    public static void Write(string where, Exception? ex)
    {
        try
        {
            var dir = FileSystem.AppDataDirectory;
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "crash.log");
            var line =
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {where}\n{ex}\n----\n";
            lock (Gate)
                File.AppendAllText(path, line);
            System.Diagnostics.Debug.WriteLine("[CrashLog] " + where + " " + ex);
        }
        catch
        {
            // ignore
        }
    }
}
