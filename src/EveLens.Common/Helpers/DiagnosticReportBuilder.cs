// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using EveLens.Common.Data;
using EveLens.Common.Extensions;
using EveLens.Common.Models;
using EveLens.Common.Services;

namespace EveLens.Common.Helpers
{
    /// <summary>
    /// Builds sanitized diagnostic reports for sharing. Strips character names, IDs,
    /// file paths containing usernames, tokens, and other PII from diagnostic output.
    /// </summary>
    public static class DiagnosticReportBuilder
    {
        // Compiled regex patterns for sanitization (matches StringExtensions.cs pattern)
        private static readonly Regex s_windowsUserPath = new Regex(
            @"[A-Za-z]:\\Users\\[^\\]+\\",
            RegexOptions.Compiled);

        private static readonly Regex s_uncPath = new Regex(
            @"\\\\[^\\]+\\",
            RegexOptions.Compiled);

        private static readonly Regex s_longToken = new Regex(
            @"[A-Za-z0-9+/=_-]{40,}",
            RegexOptions.Compiled);

        // Catches standalone EVE entity IDs (8-20 digits) that weren't already replaced
        // by the known-ID pass. Avoids matching version numbers (x.y.z.w), timestamps
        // in our report format (00:00:00), and small numbers used as counts/sizes.
        private static readonly Regex s_eveEntityId = new Regex(
            @"(?<![.\d:])\b\d{8,20}\b(?![.\d:])",
            RegexOptions.Compiled);

        /// <summary>
        /// Builds an on-demand diagnostic report (no exception).
        /// Includes version, runtime, OS, character/ESI summary, query monitor status,
        /// datafile report, and sanitized trace log.
        /// </summary>
        /// <returns>The sanitized diagnostic report text.</returns>
        public static string BuildDiagnosticReport()
        {
            StringBuilder report = new StringBuilder();

            try
            {
                AppendSystemInfo(report);
                report.AppendLine();
                AppendOperationalSummary(report);
                report.AppendLine();
                AppendESIKeyHealth(report);
                report.AppendLine();
                AppendSettingsState(report);
                report.AppendLine();
                AppendQueryMonitorStatus(report);
                report.AppendLine();
                AppendDatafileReport(report);
                report.AppendLine();
                AppendTraceLog(report);
            }
            catch (Exception ex)
            {
                report.AppendLine($"Error building diagnostic report: {ex.Message}");
            }

            return SanitizeText(report.ToString());
        }

        /// <summary>
        /// Builds a crash report including everything from BuildDiagnosticReport
        /// plus the sanitized recursive stack trace.
        /// </summary>
        /// <param name="exception">The unhandled exception.</param>
        /// <returns>The sanitized crash report text.</returns>
        public static string BuildCrashReport(Exception exception)
        {
            StringBuilder report = new StringBuilder();

            try
            {
                AppendSystemInfo(report);
                report.AppendLine();

                report.AppendLine(GetRecursiveStackTrace(exception));
                report.AppendLine();

                AppendOperationalSummary(report);
                report.AppendLine();
                AppendESIKeyHealth(report);
                report.AppendLine();
                AppendSettingsState(report);
                report.AppendLine();
                AppendQueryMonitorStatus(report);
                report.AppendLine();
                AppendDatafileReport(report);
                report.AppendLine();
                AppendTraceLog(report);
            }
            catch (Exception ex)
            {
                report.AppendLine($"Error building crash report: {ex.Message}");
            }

            return SanitizeText(report.ToString());
        }

        /// <summary>
        /// Gets the recursive stack trace from an exception and all inner exceptions.
        /// Removes project local paths from the output.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <returns>The formatted stack trace string.</returns>
        public static string GetRecursiveStackTrace(Exception exception)
        {
            StringBuilder stackTraceBuilder = new StringBuilder();
            Exception ex = exception;

            stackTraceBuilder.Append(ex).AppendLine();

            while (ex.InnerException != null)
            {
                ex = ex.InnerException;
                stackTraceBuilder.AppendLine().Append(ex).AppendLine();
            }

            return stackTraceBuilder.ToString().RemoveProjectLocalPath();
        }

        /// <summary>
        /// Applies all sanitization rules to strip PII and game data from text.
        /// Safe to call even before EveLensClient is fully initialized.
        ///
        /// Strategy: first collect ALL known names and IDs from the live object model,
        /// then do longest-first string replacement, then apply regex catch-alls for
        /// anything we missed (tokens, file paths, EVE entity IDs).
        /// </summary>
        /// <param name="text">The text to sanitize.</param>
        /// <returns>The sanitized text.</returns>
        public static string SanitizeText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // ── Phase 1: Collect every known name and ID from the live model ──
            // We build two collections:
            //   nameReplacements: string → replacement (longest first to avoid partial matches)
            //   idReplacements:   numeric-string → replacement
            var nameReplacements = new List<(string original, string replacement)>();
            var idReplacements = new HashSet<string>();

            try
            {
                var characters = AppServices.Characters;
                if (characters != null)
                {
                    int charIndex = 1;
                    foreach (Character character in characters.OrderByDescending(c =>
                        c.Name?.Length ?? 0))
                    {
                        string label = $"Character_{charIndex}";

                        // Character name
                        if (!string.IsNullOrEmpty(character.Name))
                            nameReplacements.Add((character.Name, label));

                        // Corporation name and ID
                        if (!string.IsNullOrEmpty(character.CorporationName))
                            nameReplacements.Add((character.CorporationName,
                                $"Corporation_{charIndex}"));
                        if (character.CorporationID > 0)
                            idReplacements.Add(character.CorporationID.ToString());

                        // Alliance name and ID
                        if (!string.IsNullOrEmpty(character.AllianceName))
                            nameReplacements.Add((character.AllianceName,
                                $"Alliance_{charIndex}"));
                        if (character.AllianceID > 0)
                            idReplacements.Add(character.AllianceID.ToString());

                        // Character ID
                        if (character.CharacterID > 0)
                            idReplacements.Add(character.CharacterID.ToString());

                        // Employment history — corp names and IDs from past corps
                        try
                        {
                            if (character.EmploymentHistory != null)
                            {
                                foreach (var record in character.EmploymentHistory)
                                {
                                    if (!string.IsNullOrEmpty(record.CorporationName))
                                        nameReplacements.Add((record.CorporationName,
                                            "[CORP_NAME]"));
                                }
                            }
                        }
                        catch
                        {
                            // EmploymentHistory may not be loaded
                        }

                        // Plan names — user-created, could contain personal info
                        try
                        {
                            if (character.Plans != null)
                            {
                                int planIndex = 1;
                                foreach (var plan in character.Plans)
                                {
                                    if (!string.IsNullOrEmpty(plan.Name))
                                    {
                                        nameReplacements.Add((plan.Name,
                                            $"{label}_Plan_{planIndex}"));
                                        planIndex++;
                                    }
                                }
                            }
                        }
                        catch
                        {
                            // Plans may not be loaded
                        }

                        charIndex++;
                    }
                }

                // ESI key IDs
                var esiKeys = AppServices.ESIKeys;
                if (esiKeys != null)
                {
                    foreach (var key in esiKeys)
                    {
                        if (key.ID > 0)
                            idReplacements.Add(key.ID.ToString());
                    }
                }

                // Character identities — may have IDs not yet in the Characters collection
                var identities = AppServices.CharacterIdentities;
                if (identities != null)
                {
                    foreach (var identity in identities)
                    {
                        if (identity.CharacterID > 0)
                            idReplacements.Add(identity.CharacterID.ToString());

                        if (!string.IsNullOrEmpty(identity.CharacterName))
                            nameReplacements.Add((identity.CharacterName, "[CHAR_NAME]"));
                    }
                }
            }
            catch
            {
                // Collections may not be initialized during early crash
            }

            // ── Phase 2: Apply name replacements (longest first) ──
            // Sort by length descending so "Alia Collins" is replaced before "Alia"
            nameReplacements.Sort((a, b) => b.original.Length.CompareTo(a.original.Length));
            foreach (var (original, replacement) in nameReplacements)
            {
                text = text.Replace(original, replacement);
            }

            // ── Phase 3: Apply known ID replacements ──
            foreach (string id in idReplacements)
            {
                text = text.Replace(id, "[EVE_ID]");
            }

            // ── Phase 4: SSO credentials ──
            try
            {
                string ssoAppId = Constants.NetworkConstants.SSODefaultAppID;
                if (!string.IsNullOrEmpty(ssoAppId))
                    text = text.Replace(ssoAppId, "[SSO_CLIENT_ID]");

                string ssoClientId = Settings.SSOClientID;
                if (!string.IsNullOrEmpty(ssoClientId))
                    text = text.Replace(ssoClientId, "[SSO_CLIENT_ID]");

                string ssoSecret = Settings.SSOClientSecret;
                if (!string.IsNullOrEmpty(ssoSecret))
                    text = text.Replace(ssoSecret, "[SSO_CLIENT_SECRET]");
            }
            catch
            {
                // Resource or Settings may not be available
            }

            // ── Phase 5: File paths and system identity ──
            // Windows user paths: C:\Users\username\ -> C:\Users\[REDACTED]\
            text = s_windowsUserPath.Replace(text, @"C:\Users\[REDACTED]\");

            // UNC paths: \\server\ -> \\[REDACTED]\
            text = s_uncPath.Replace(text, @"\\[REDACTED]\");

            // Machine name and Windows username
            try
            {
                string machineName = Environment.MachineName;
                if (!string.IsNullOrEmpty(machineName))
                    text = text.Replace(machineName, "[REDACTED]");

                string userName = Environment.UserName;
                if (!string.IsNullOrEmpty(userName))
                    text = text.Replace(userName, "[REDACTED]");
            }
            catch
            {
                // Environment may throw in sandboxed contexts
            }

            // ── Phase 6: Tokens and secrets ──
            // Long base64/token strings (40+ alphanumeric chars)
            text = s_longToken.Replace(text, "[TOKEN_REDACTED]");

            // ── Phase 7: Catch-all for EVE entity IDs we missed ──
            // Any standalone 8-20 digit number not adjacent to dots/colons (avoids
            // version numbers like 5.1.2.0 and timestamps like 02:39:51).
            // At this point, known IDs are already replaced, so remaining large
            // numbers are likely entity IDs from EveIDToName lookups, employment
            // history corp IDs, or other game data we don't have a handle on.
            text = s_eveEntityId.Replace(text, "[EVE_ID]");

            // ── Phase 8: Project local paths ──
            text = text.RemoveProjectLocalPath();

            return text;
        }

        /// <summary>
        /// Saves a report to a text file in the EveLens data directory.
        /// </summary>
        /// <param name="reportText">The report text to save.</param>
        /// <returns>The full file path on success, or null on failure.</returns>
        public static string SaveReportToFile(string reportText)
        {
            try
            {
                string filePath = Path.Combine(AppServices.ApplicationPaths.DataDirectory,
                    "DiagnosticReport.txt");
                File.WriteAllText(filePath, reportText);
                return filePath;
            }
            catch (IOException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
        }

        /// <summary>
        /// Builds a GitHub issue URL with a short pre-filled body that instructs the
        /// user to attach the saved report file. Keeps the URL well under GitHub's
        /// ~8KB limit.
        /// </summary>
        /// <param name="title">The issue title.</param>
        /// <param name="crashSummary">Optional crash summary (exception type + message)
        /// to include in the body. Null for diagnostic reports.</param>
        /// <returns>The fully-formed GitHub issue URL.</returns>
        public static string BuildGitHubIssueUrl(string title, string crashSummary = null)
        {
            string version;
            try
            {
                version = AppServices.FileVersionInfo?.FileVersion ?? "(unknown)";
            }
            catch
            {
                version = "(unknown)";
            }

            string os = Environment.OSVersion.VersionString;

            StringBuilder body = new StringBuilder();
            body.AppendLine("## Environment");
            body.AppendLine($"- **EveLens Version:** {version}");
            body.AppendLine($"- **OS:** {os}");

            if (!string.IsNullOrEmpty(crashSummary))
            {
                body.AppendLine();
                body.AppendLine("## Crash");
                body.AppendLine($"`{crashSummary}`");
            }

            body.AppendLine();
            body.AppendLine("## Report");
            body.AppendLine("Please attach the `DiagnosticReport.txt` file from your " +
                "EveLens data folder (`%APPDATA%\\EveLens\\`), or paste the report from " +
                "your clipboard into this issue.");

            string encodedTitle = Uri.EscapeDataString(title ?? "Bug Report");
            string encodedBody = Uri.EscapeDataString(body.ToString());
            return $"https://github.com/aliacollins/evelens/issues/new" +
                   $"?title={encodedTitle}&labels=bug&body={encodedBody}";
        }

        #region Private Helpers

        /// <summary>
        /// Appends system/version information to the report.
        /// </summary>
        private static void AppendSystemInfo(StringBuilder report)
        {
            try
            {
                report.Append("EveLens Version: ").AppendLine(
                    AppServices.FileVersionInfo.FileVersion);
            }
            catch
            {
                report.AppendLine("EveLens Version: (unavailable)");
            }

            report.Append(".NET Runtime Version: ").AppendLine(
                Environment.Version.ToString());
            report.Append("Operating System: ").AppendLine(
                Environment.OSVersion.VersionString);
            report.Append("64-bit OS: ").AppendLine(
                Environment.Is64BitOperatingSystem.ToString());
            report.Append("64-bit Process: ").AppendLine(
                Environment.Is64BitProcess.ToString());
            report.Append("Processor Count: ").AppendLine(
                Environment.ProcessorCount.ToString());

            try
            {
                TimeSpan uptime = EveLensClient.Uptime;
                report.AppendLine($"Uptime: {uptime.Days}d {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s");

                using (Process proc = Process.GetCurrentProcess())
                {
                    report.AppendLine($"Working Set: {proc.WorkingSet64 / (1024 * 1024)} MB");
                    report.AppendLine($"Thread Count: {proc.Threads.Count}");
                }

                report.AppendLine($"GC Heap: {GC.GetTotalMemory(false) / (1024 * 1024)} MB");
            }
            catch
            {
                // Process info may not be available in all contexts
            }
        }

        /// <summary>
        /// Appends operational summary with counts and booleans only (no names, IDs, or game data).
        /// </summary>
        private static void AppendOperationalSummary(StringBuilder report)
        {
            report.AppendLine("Operational Summary:");

            try
            {
                var characters = AppServices.Characters;
                var esiKeys = AppServices.ESIKeys;
                var monitored = AppServices.MonitoredCharacters;

                int charCount = characters?.Count() ?? 0;
                int ccpCount = characters?.OfType<CCPCharacter>().Count() ?? 0;
                int localCount = charCount - ccpCount;
                int esiCount = esiKeys?.Count() ?? 0;
                int monCount = monitored?.Count() ?? 0;

                report.AppendLine($"  Characters Loaded: {charCount} ({ccpCount} CCP, {localCount} local)");
                report.AppendLine($"  ESI Keys: {esiCount}");
                report.AppendLine($"  Monitored: {monCount}");
                report.AppendLine($"  Data Loaded: {AppServices.IsDataLoaded}");
                report.AppendLine($"  Settings Restoring: {Settings.IsRestoring}");
            }
            catch
            {
                report.AppendLine("  (unavailable - not initialized)");
            }
        }

        /// <summary>
        /// Appends per-character query monitor status using anonymized character names.
        /// </summary>
        private static void AppendQueryMonitorStatus(StringBuilder report)
        {
            report.AppendLine("Query Monitor Status:");

            try
            {
                var characters = AppServices.Characters;
                if (characters == null)
                {
                    report.AppendLine("  (unavailable)");
                    return;
                }

                int charIndex = 1;
                foreach (Character character in characters)
                {
                    CCPCharacter ccpChar = character as CCPCharacter;
                    if (ccpChar?.QueryMonitors == null)
                    {
                        charIndex++;
                        continue;
                    }

                    report.AppendLine($"  Character_{charIndex}:");

                    foreach (var monitor in ccpChar.QueryMonitors)
                    {
                        var line = $"    {monitor.Method}: " +
                            $"Status={monitor.Status}, " +
                            $"Enabled={monitor.Enabled}, " +
                            $"LastUpdate={monitor.LastUpdate:u}, " +
                            $"NextUpdate={monitor.NextUpdate:u}";

                        var result = monitor.LastResult;
                        if (result != null && result.HasError)
                        {
                            if (!string.IsNullOrEmpty(result.ErrorMessage))
                                line += $", Error=\"{result.ErrorMessage}\"";
                            if (result.ErrorCode != 0)
                                line += $", ErrorCode={result.ErrorCode}";
                            if (result.ErrorType != Enumerations.CCPAPI.APIErrorType.None)
                                line += $", ErrorType={result.ErrorType}";
                        }

                        report.AppendLine(line);
                    }

                    charIndex++;
                }
            }
            catch
            {
                report.AppendLine("  (error reading query monitors)");
            }
        }

        /// <summary>
        /// Appends per-key ESI auth status as booleans only (no tokens, IDs, or credentials).
        /// </summary>
        private static void AppendESIKeyHealth(StringBuilder report)
        {
            report.AppendLine("ESI Key Health:");

            try
            {
                var esiKeys = AppServices.ESIKeys;
                if (esiKeys == null || !esiKeys.Any())
                {
                    report.AppendLine("  (no keys)");
                    return;
                }

                int keyIndex = 1;
                foreach (var key in esiKeys)
                {
                    bool hasToken = !string.IsNullOrEmpty(key.RefreshToken);
                    report.AppendLine($"  Key {keyIndex}: " +
                        $"HasError={key.HasError}, " +
                        $"HasToken={hasToken}, " +
                        $"Processed={key.IsProcessed}");
                    keyIndex++;
                }
            }
            catch
            {
                report.AppendLine("  (unavailable)");
            }
        }

        /// <summary>
        /// Appends settings configuration context (format, migration state, update channel).
        /// </summary>
        private static void AppendSettingsState(StringBuilder report)
        {
            report.AppendLine("Settings:");

            try
            {
                report.AppendLine($"  Format: {(Settings.UsingJsonFormat ? "JSON" : "XML")}");
                report.AppendLine($"  ForkMigration: {(Settings.MigrationFromOtherForkDetected ? "Yes" : "No")}");

                string channel = "Stable";
                if (AppServices.IsAlphaVersion)
                    channel = "Alpha";
                else if (AppServices.IsBetaVersion)
                    channel = "Beta";

                report.AppendLine($"Update Channel: {channel}");
                report.AppendLine($"  AutoCheck: {Settings.Updates.CheckEveLensVersion}");
            }
            catch
            {
                report.AppendLine("  (unavailable)");
            }
        }

        /// <summary>
        /// Appends datafile information (filenames and MD5 sums).
        /// Reuses the pattern from UnhandledExceptionWindow.GetDatafileReport.
        /// </summary>
        private static void AppendDatafileReport(StringBuilder report)
        {
            report.AppendLine("Datafile report:");

            try
            {
                foreach (string datafile in Datafile.GetFilesFrom(AppServices.ApplicationPaths.DataDirectory,
                    Datafile.DatafilesExtension))
                {
                    FileInfo info = new FileInfo(datafile);
                    Datafile file = new Datafile(Path.GetFileName(datafile));

                    report.AppendLine($"  {info.Name} " +
                        $"({info.Length / 1024}KiB - {file.MD5Sum})");
                }
            }
            catch (UnauthorizedAccessException)
            {
                report.AppendLine("  (unable to access datafile directory)");
            }
            catch (Exception)
            {
                report.AppendLine("  (error reading datafiles)");
            }
        }

        /// <summary>
        /// Appends the filtered trace log, excluding lines that contain game-specific data
        /// (asset locations, structure names, character names, item IDs, etc.).
        /// Keeps operational lines: settings, datafiles, ESI auth, query monitor lifecycle,
        /// updates, errors, and startup/shutdown events.
        /// </summary>
        private static void AppendTraceLog(StringBuilder report)
        {
            report.AppendLine("Diagnostic Log:");

            FileStream traceStream = null;
            try
            {
                traceStream = Util.GetFileStream(AppServices.ApplicationPaths.TraceFilePath,
                    FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using (StreamReader traceReader = new StreamReader(traceStream))
                {
                    traceStream = null;
                    string line;
                    while ((line = traceReader.ReadLine()) != null)
                    {
                        if (IsSafeTraceLine(line))
                            report.AppendLine(line);
                    }
                }
            }
            catch (IOException)
            {
                report.AppendLine("  (trace file not available)");
            }
            catch (UnauthorizedAccessException)
            {
                report.AppendLine("  (unable to access trace file)");
            }
            finally
            {
                traceStream?.Dispose();
            }
        }

        /// <summary>
        /// Returns true if a trace line is safe to include in diagnostic reports.
        /// Uses a whitelist approach: only lines matching known-safe prefixes are included.
        /// Everything else (character names, ESI key IDs, skill names, etc.) is excluded.
        /// </summary>
        private static bool IsSafeTraceLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;

            // Strip timestamp prefix to get to the content
            // Format: "2026-02-08 12:47:00Z > MethodName - args"
            // or:     "0d 0h 00m 05s > MethodName - args"
            int markerIndex = line.IndexOf("> ", StringComparison.Ordinal);
            string content = markerIndex >= 0 ? line.Substring(markerIndex + 2) : line;

            return content.StartsWith("Program.Startup", StringComparison.Ordinal) ||
                   content.StartsWith("Main loop", StringComparison.Ordinal) ||
                   content.StartsWith("Main window", StringComparison.Ordinal) ||
                   content.StartsWith("MainWindow", StringComparison.Ordinal) ||
                   content.StartsWith("Closed", StringComparison.Ordinal) ||
                   content.StartsWith("EveLensClient.Initialize", StringComparison.Ordinal) ||
                   content.StartsWith("EveLensClient.Run", StringComparison.Ordinal) ||
                   content.StartsWith("EveLensClient.Shutdown", StringComparison.Ordinal) ||
                   content.StartsWith("Settings.", StringComparison.Ordinal) ||
                   content.StartsWith("Datafiles.Load", StringComparison.Ordinal) ||
                   content.StartsWith("SettingsFileManager", StringComparison.Ordinal) ||
                   content.StartsWith("QueryMonitor", StringComparison.Ordinal) ||
                   content.StartsWith("UpdateManager", StringComparison.Ordinal) ||
                   content.StartsWith("TimeCheck", StringComparison.Ordinal) ||
                   content.StartsWith("CredentialProtection", StringComparison.Ordinal) ||
                   content.StartsWith("SSOAuthentication", StringComparison.Ordinal) ||
                   content.StartsWith("Batched update for", StringComparison.Ordinal) ||
                   content.StartsWith("Batched skill queue update", StringComparison.Ordinal);
        }

        #endregion
    }
}
