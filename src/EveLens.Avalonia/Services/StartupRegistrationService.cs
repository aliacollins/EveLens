// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using EveLens.Common.Services;

namespace EveLens.Avalonia.Services
{
    /// <summary>
    /// Registers/unregisters EveLens to launch at OS login, starting quietly in the tray
    /// (Issue #72). Windows uses the per-user registry Run key pointing at the Velopack
    /// root stub (which survives updates); Linux writes an XDG autostart .desktop entry.
    /// macOS login items require user consent dialogs, so it is not offered there yet.
    /// Idempotent: <see cref="Sync"/> is safe to call on every launch.
    /// </summary>
    internal static class StartupRegistrationService
    {
        private const string RunValueName = "EveLens";
        private const string StartMinimizedArg = "--start-minimized";

        /// <summary>True when the current OS supports startup registration.</summary>
        public static bool IsSupported =>
            OperatingSystem.IsWindows() || OperatingSystem.IsLinux();

        /// <summary>
        /// Brings the OS registration in line with the setting. Called at startup and when
        /// the setting changes — repairs a registration that points at a stale path.
        /// </summary>
        public static void Sync(bool runAtStartup)
        {
            try
            {
                // OperatingSystem.* guards (not RuntimeInformation) so the CA1416
                // platform-compatibility analyzer can verify the call flow
                if (OperatingSystem.IsWindows())
                    SyncWindows(runAtStartup);
                else if (OperatingSystem.IsLinux())
                    SyncLinux(runAtStartup);
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace(
                    $"Startup registration sync failed: {ex.Message}", printMethod: false);
            }
        }

        /// <summary>
        /// The executable to launch at login. Prefers the Velopack root stub
        /// (%LocalAppData%\EveLens\EveLens.exe) because it survives updates —
        /// the versioned current\ path changes on every release.
        /// </summary>
        private static string GetLaunchPath()
        {
            string processPath = Environment.ProcessPath ?? string.Empty;

            // Installed layout: <root>\current\EveLens.exe with a launcher stub at <root>\EveLens.exe
            var currentDir = Path.GetDirectoryName(processPath);
            if (currentDir != null &&
                string.Equals(Path.GetFileName(currentDir), "current", StringComparison.OrdinalIgnoreCase))
            {
                string stub = Path.Combine(Path.GetDirectoryName(currentDir)!,
                    Path.GetFileName(processPath));
                if (File.Exists(stub))
                    return stub;
            }

            return processPath;
        }

        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        private static void SyncWindows(bool runAtStartup)
        {
            using var runKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", writable: true);
            if (runKey == null)
                return;

            if (runAtStartup)
            {
                string launch = $"\"{GetLaunchPath()}\" {StartMinimizedArg}";
                if (!Equals(runKey.GetValue(RunValueName), launch))
                    runKey.SetValue(RunValueName, launch);
            }
            else if (runKey.GetValue(RunValueName) != null)
            {
                runKey.DeleteValue(RunValueName, throwOnMissingValue: false);
            }
        }

        private static void SyncLinux(bool runAtStartup)
        {
            string autostartDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".config", "autostart");
            string entryPath = Path.Combine(autostartDir, "evelens.desktop");

            if (!runAtStartup)
            {
                if (File.Exists(entryPath))
                    File.Delete(entryPath);
                return;
            }

            Directory.CreateDirectory(autostartDir);
            File.WriteAllText(entryPath,
                "[Desktop Entry]\n" +
                "Type=Application\n" +
                "Name=EveLens\n" +
                $"Exec=\"{GetLaunchPath()}\" {StartMinimizedArg}\n" +
                "X-GNOME-Autostart-enabled=true\n" +
                "Comment=Character Intelligence for EVE Online\n");
        }
    }
}
