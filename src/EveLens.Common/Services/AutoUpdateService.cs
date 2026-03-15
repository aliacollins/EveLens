// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace EveLens.Common.Services
{
    /// <summary>
    /// Downloads and applies application updates. Platform-aware: Windows installer,
    /// Linux AppImage swap, macOS .app swap.
    /// </summary>
    public static class AutoUpdateService
    {
        /// <summary>
        /// Downloads the update file to a temporary location with progress reporting.
        /// </summary>
        /// <param name="url">Download URL for the update file.</param>
        /// <param name="progress">Optional progress callback (0.0 – 1.0).</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Full path to the downloaded file.</returns>
        public static async Task<string> DownloadAsync(Uri url, IProgress<double>? progress = null, CancellationToken ct = default)
        {
            string fileName = Path.GetFileName(url.LocalPath);
            string tempPath = Path.Combine(Path.GetTempPath(), "EveLens-Update", fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);

            using var client = new HttpClient();
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            long? totalBytes = response.Content.Headers.ContentLength;
            await using var contentStream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            await using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            byte[] buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct).ConfigureAwait(false);
                totalRead += bytesRead;

                if (totalBytes.HasValue && totalBytes.Value > 0)
                    progress?.Report((double)totalRead / totalBytes.Value);
            }

            progress?.Report(1.0);
            return tempPath;
        }

        /// <summary>
        /// Applies the downloaded update and exits the application.
        /// Platform-specific: Windows runs installer, Linux/macOS swap files via script.
        /// </summary>
        /// <param name="downloadedPath">Path to the downloaded update file.</param>
        /// <param name="installArgs">Installer arguments (Windows only, e.g., "/SILENT").</param>
        public static void ApplyAndRestart(string downloadedPath, string? installArgs)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                ApplyWindows(downloadedPath, installArgs);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                ApplyLinux(downloadedPath);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                ApplyMacOS(downloadedPath);
            else
                throw new PlatformNotSupportedException("Auto-update not supported on this platform.");
        }

        /// <summary>
        /// Windows: launch the installer and exit. The installer handles file replacement.
        /// </summary>
        private static void ApplyWindows(string installerPath, string? installArgs)
        {
            var psi = new ProcessStartInfo
            {
                FileName = installerPath,
                Arguments = installArgs ?? "/SILENT",
                UseShellExecute = true
            };

            Process.Start(psi);
            Environment.Exit(0);
        }

        /// <summary>
        /// Linux: backup current AppImage, write a swap script, launch it, exit.
        /// The script waits for the app to exit, swaps the files, and relaunches.
        /// </summary>
        private static void ApplyLinux(string downloadedAppImage)
        {
            // Current executable path — for AppImage this is the AppImage file itself
            string? currentExe = GetCurrentExecutablePath();
            if (currentExe == null)
                throw new InvalidOperationException("Cannot determine current executable path.");

            string backupPath = currentExe + ".backup";
            string scriptPath = Path.Combine(Path.GetTempPath(), "evelens-update.sh");

            // Write the swap script
            string script = $"""
                #!/bin/bash
                # EveLens auto-update swap script
                sleep 2
                cp "{currentExe}" "{backupPath}" 2>/dev/null
                cp "{downloadedAppImage}" "{currentExe}"
                chmod +x "{currentExe}"
                "{currentExe}" &
                rm -f "{downloadedAppImage}"
                rm -f "$0"
                """;

            File.WriteAllText(scriptPath, script);

            var psi = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = scriptPath,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process.Start(psi);
            Environment.Exit(0);
        }

        /// <summary>
        /// macOS: backup current .app, write a swap script, launch it, exit.
        /// Expects the downloaded file to be a .app.zip.
        /// </summary>
        private static void ApplyMacOS(string downloadedZip)
        {
            // For macOS, the executable is inside a .app bundle
            // AppContext.BaseDirectory gives us something like /Applications/EveLens.app/Contents/MacOS/
            string baseDir = AppContext.BaseDirectory;
            string? appBundlePath = FindAppBundle(baseDir);
            if (appBundlePath == null)
                throw new InvalidOperationException("Cannot determine .app bundle path.");

            string backupPath = appBundlePath + ".backup";
            string extractDir = Path.Combine(Path.GetTempPath(), "EveLens-Update-Extract");
            string scriptPath = Path.Combine(Path.GetTempPath(), "evelens-update.sh");

            string script = $"""
                #!/bin/bash
                # EveLens auto-update swap script (macOS)
                sleep 2
                rm -rf "{backupPath}"
                mv "{appBundlePath}" "{backupPath}"
                rm -rf "{extractDir}"
                mkdir -p "{extractDir}"
                unzip -q "{downloadedZip}" -d "{extractDir}"
                APP_NAME=$(ls "{extractDir}" | head -1)
                mv "{extractDir}/$APP_NAME" "{appBundlePath}"
                open "{appBundlePath}"
                rm -rf "{extractDir}"
                rm -f "{downloadedZip}"
                rm -f "$0"
                """;

            File.WriteAllText(scriptPath, script);

            var psi = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = scriptPath,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process.Start(psi);
            Environment.Exit(0);
        }

        /// <summary>
        /// Cleans up backup files from a previous update.
        /// Call on startup to remove .backup files.
        /// </summary>
        public static void CleanupPreviousUpdate()
        {
            try
            {
                string? currentExe = GetCurrentExecutablePath();
                if (currentExe == null) return;

                string backupPath = currentExe + ".backup";
                if (File.Exists(backupPath))
                    File.Delete(backupPath);

                // macOS: clean up .app.backup
                string baseDir = AppContext.BaseDirectory;
                string? appBundle = FindAppBundle(baseDir);
                if (appBundle != null)
                {
                    string appBackup = appBundle + ".backup";
                    if (Directory.Exists(appBackup))
                        Directory.Delete(appBackup, true);
                }
            }
            catch
            {
                // Cleanup is best-effort — don't crash on startup
            }
        }

        private static string? GetCurrentExecutablePath()
        {
            // APPIMAGE env var is set by AppImage runtime
            string? appImage = Environment.GetEnvironmentVariable("APPIMAGE");
            if (!string.IsNullOrEmpty(appImage))
                return appImage;

            // Fallback: the process executable
            return Environment.ProcessPath;
        }

        private static string? FindAppBundle(string baseDir)
        {
            // Walk up from Contents/MacOS/ to find the .app directory
            var dir = new DirectoryInfo(baseDir);
            while (dir != null)
            {
                if (dir.Name.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
                    return dir.FullName;
                dir = dir.Parent;
            }
            return null;
        }
    }
}
