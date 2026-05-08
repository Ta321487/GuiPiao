using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using GuiPiao.Services;

namespace GuiPiao.Utils;

public static class WindowManager
{
    private static readonly Dictionary<Type, List<Window>> _trackedWindows = new();

    private static readonly HashSet<Type> _formWindowTypes = new();

    public static void RegisterFormWindowType<T>() where T : Window
    {
        _formWindowTypes.Add(typeof(T));
    }

    public static bool IsFormWindowType(Type windowType)
    {
        return _formWindowTypes.Contains(windowType);
    }

    public static bool IsSingleInstanceMode()
    {
        try
        {
            return new GeneralSettingsService().Config.SingleInstance;
        }
        catch
        {
            return true;
        }
    }

    public static T ShowWindow<T>(Func<T> createWindow, bool activateExisting = true) where T : Window
    {
        var windowType = typeof(T);
        CleanupClosedWindows();

        if (IsSingleInstanceMode())
        {
            var existingList = GetWindowList(windowType);
            if (existingList.Count > 0)
            {
                var existing = existingList[0];
                if (activateExisting)
                {
                    existing.Activate();
                    existing.WindowState = WindowState.Normal;
                }
                return (T)existing;
            }
        }

        var window = createWindow();
        AddWindow(windowType, window);
        window.Show();
        return window;
    }

    public static T ShowDialog<T>(Func<T> createWindow) where T : Window
    {
        var window = createWindow();
        AddWindow(typeof(T), window);
        window.ShowDialog();
        return window;
    }

    public static bool TryGetExistingWindow<T>(out T? existingWindow) where T : Window
    {
        CleanupClosedWindows();
        var windowType = typeof(T);
        var list = GetWindowList(windowType);

        if (list.Count > 0)
        {
            existingWindow = (T)list[0];
            return true;
        }

        existingWindow = null;
        return false;
    }

    public static void EnforceSingleInstance()
    {
        CleanupClosedWindows();

        foreach (var kvp in _trackedWindows.ToList())
        {
            var windowList = kvp.Value;
            if (windowList.Count <= 1) continue;

            if (IsFormWindowType(kvp.Key))
            {
                var first = windowList[0];
                first.Activate();
                first.WindowState = WindowState.Normal;
                continue;
            }

            var keepWindow = windowList[0];
            keepWindow.Activate();
            keepWindow.WindowState = WindowState.Normal;

            for (var i = windowList.Count - 1; i >= 1; i--)
            {
                var extraWindow = windowList[i];
                extraWindow.Close();
            }
        }
    }

    private static List<Window> GetWindowList(Type windowType)
    {
        if (!_trackedWindows.TryGetValue(windowType, out var list))
        {
            list = new List<Window>();
            _trackedWindows[windowType] = list;
        }
        return list;
    }

    private static void AddWindow(Type windowType, Window window)
    {
        var list = GetWindowList(windowType);
        list.Add(window);
        window.Closed += (_, _) => CleanupClosedWindows();
    }

    private static void CleanupClosedWindows()
    {
        foreach (var kvp in _trackedWindows.ToList())
        {
            kvp.Value.RemoveAll(w => w == null || !w.IsVisible);
            if (kvp.Value.Count == 0)
                _trackedWindows.Remove(kvp.Key);
        }
    }
}
