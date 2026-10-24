using System;
using System.IO;
using System.Windows;
using ControlzEx.Theming;

namespace LabScape.App.Services;

public static class ThemeService
{
    private static bool _isDarkTheme;

    public static bool IsDarkTheme => _isDarkTheme;

    public static event Action<bool>? ThemeChanged;

    public static void Initialize()
    {
        bool isDark = false;
        try
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LabScape");
            string file = Path.Combine(dir, "theme.txt");
            if (File.Exists(file))
            {
                string text = File.ReadAllText(file).Trim();
                if (string.Equals(text, "Dark", StringComparison.OrdinalIgnoreCase))
                {
                    isDark = true;
                }
            }
        }
        catch
        {
            // Default to Light on error
        }

        ApplyTheme(isDark, savePreference: false);
    }

    public static void ToggleTheme()
    {
        ApplyTheme(!_isDarkTheme, savePreference: true);
    }

    public static void ApplyTheme(bool isDark, bool savePreference = true)
    {
        _isDarkTheme = isDark;

        var app = Application.Current;
        if (app != null)
        {
            // 1. Fluent.Ribbon theme
            try
            {
                ThemeManager.Current.ChangeTheme(app, isDark ? "Dark.Cobalt" : "Light.Cobalt");
            }
            catch
            {
                // Fallback
            }

            // 2. Custom App Dynamic Brush Dictionary
            try
            {
                string targetUri = isDark ? "Resources/Themes/DarkTheme.xaml" : "Resources/Themes/LightTheme.xaml";
                var newDict = new ResourceDictionary { Source = new Uri(targetUri, UriKind.RelativeOrAbsolute) };
                var merged = app.Resources.MergedDictionaries;
                ResourceDictionary? found = null;
                foreach (var dict in merged)
                {
                    if (dict.Source != null && (dict.Source.OriginalString.Contains("DarkTheme.xaml") || dict.Source.OriginalString.Contains("LightTheme.xaml")))
                    {
                        found = dict;
                        break;
                    }
                }

                if (found != null)
                {
                    int idx = merged.IndexOf(found);
                    merged[idx] = newDict;
                }
                else
                {
                    merged.Add(newDict);
                }
            }
            catch
            {
                // Resource fallback
            }
        }

        // 3. Persist user preference
        if (savePreference)
        {
            try
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LabScape");
                Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, "theme.txt"), isDark ? "Dark" : "Light");
            }
            catch
            {
                // Non-fatal
            }
        }

        ThemeChanged?.Invoke(_isDarkTheme);
    }
}
