using System;
using System.Collections.Generic;
using System.IO;

namespace EVEMon.Avalonia.Services
{
    /// <summary>
    /// Manages theme palette preferences. Theme changes require an app restart
    /// because palettes are loaded as StaticResource during initialization.
    /// </summary>
    internal static class ThemeManager
    {
        /// <summary>
        /// Available themes as (internalName, displayName) pairs.
        /// The internal name must match the palette AXAML filename (without extension).
        /// </summary>
        public static IReadOnlyList<(string Name, string DisplayName)> AvailableThemes { get; } = new[]
        {
            ("DarkSpace", "Dark Space"),
            ("CaldariBlue", "Caldari Blue"),
            ("AmarrGold", "Amarr Gold"),
            ("MinmatarRust", "Minmatar Rust"),
            ("GallenteGreen", "Gallente Green"),
            ("Midnight", "Midnight"),
        };

        /// <summary>
        /// Reads the current theme name from theme.txt. Returns "DarkSpace" if missing or unreadable.
        /// </summary>
        public static string CurrentTheme
        {
            get
            {
                try
                {
                    string path = GetThemeFilePath();
                    if (File.Exists(path))
                    {
                        string name = File.ReadAllText(path).Trim();
                        if (!string.IsNullOrEmpty(name))
                            return name;
                    }
                }
                catch
                {
                    // Fall through to default
                }

                return "DarkSpace";
            }
        }

        /// <summary>
        /// Writes the theme preference to theme.txt. The change takes effect on next app launch.
        /// </summary>
        public static void WriteThemePreference(string themeName)
        {
            try
            {
                string path = GetThemeFilePath();
                string? dir = Path.GetDirectoryName(path);
                if (dir != null)
                    Directory.CreateDirectory(dir);
                File.WriteAllText(path, themeName);
            }
            catch
            {
                // Best-effort — if we can't write, the default theme will be used next launch
            }
        }

        private static string GetThemeFilePath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EVEMon", "theme.txt");
        }
    }
}
