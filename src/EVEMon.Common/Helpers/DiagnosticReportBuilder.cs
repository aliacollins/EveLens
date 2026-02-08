using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using EVEMon.Common.Data;
using EVEMon.Common.Extensions;
using EVEMon.Common.Models;

namespace EVEMon.Common.Helpers
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
        /// Applies all sanitization rules to strip PII from text.
        /// Safe to call even before EveMonClient is fully initialized.
        /// </summary>
        /// <param name="text">The text to sanitize.</param>
        /// <returns>The sanitized text.</returns>
        public static string SanitizeText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // 1-4: Replace character names, corp names, alliance names, character IDs, ESI key IDs
            // Sort by longest name first to avoid partial matches
            try
            {
                var characters = EveMonClient.Characters;
                if (characters != null)
                {
                    int charIndex = 1;
                    // Sort by name length descending to replace longest names first
                    foreach (Character character in characters.OrderByDescending(c =>
                        c.Name?.Length ?? 0))
                    {
                        string label = $"Character_{charIndex}";

                        if (!string.IsNullOrEmpty(character.Name))
                            text = text.Replace(character.Name, label);

                        if (!string.IsNullOrEmpty(character.CorporationName))
                            text = text.Replace(character.CorporationName,
                                $"Corporation_{charIndex}");

                        if (!string.IsNullOrEmpty(character.AllianceName))
                            text = text.Replace(character.AllianceName,
                                $"Alliance_{charIndex}");

                        if (character.CharacterID > 0)
                            text = text.Replace(character.CharacterID.ToString(),
                                "[CHAR_ID]");

                        charIndex++;
                    }
                }

                var esiKeys = EveMonClient.ESIKeys;
                if (esiKeys != null)
                {
                    foreach (var key in esiKeys)
                    {
                        if (key.ID > 0)
                            text = text.Replace(key.ID.ToString(), "[ESI_KEY_ID]");
                    }
                }
            }
            catch
            {
                // Characters/ESIKeys may not be initialized during early crash
            }

            // 5: Replace known SSO client ID/secret
            try
            {
                string ssoAppId = Constants.NetworkConstants.SSODefaultAppID;
                if (!string.IsNullOrEmpty(ssoAppId))
                    text = text.Replace(ssoAppId, "[SSO_CLIENT_ID]");

                // Also sanitize user-customized SSO credentials
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

            // 6: Windows user paths: C:\Users\username\ -> C:\Users\[REDACTED]\
            text = s_windowsUserPath.Replace(text, @"C:\Users\[REDACTED]\");

            // 7: UNC paths: \\server\ -> \\[REDACTED]\
            text = s_uncPath.Replace(text, @"\\[REDACTED]\");

            // 8: Machine name and username
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

            // 9: Long base64/token strings (40+ chars)
            text = s_longToken.Replace(text, "[TOKEN_REDACTED]");

            // 10: Remove project local path (existing extension method)
            text = text.RemoveProjectLocalPath();

            return text;
        }

        /// <summary>
        /// Saves a report to a text file in the EVEMon data directory.
        /// </summary>
        /// <param name="reportText">The report text to save.</param>
        /// <returns>The full file path on success, or null on failure.</returns>
        public static string SaveReportToFile(string reportText)
        {
            try
            {
                string filePath = Path.Combine(EveMonClient.EVEMonDataDir,
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
                version = EveMonClient.FileVersionInfo?.FileVersion ?? "(unknown)";
            }
            catch
            {
                version = "(unknown)";
            }

            string os = Environment.OSVersion.VersionString;

            StringBuilder body = new StringBuilder();
            body.AppendLine("## Environment");
            body.AppendLine($"- **EVEMon Version:** {version}");
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
                "EVEMon data folder (`%APPDATA%\\EVEMon\\`), or paste the report from " +
                "your clipboard into this issue.");

            string encodedTitle = Uri.EscapeDataString(title ?? "Bug Report");
            string encodedBody = Uri.EscapeDataString(body.ToString());
            return $"https://github.com/aliacollins/evemon/issues/new" +
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
                report.Append("EVEMon Version: ").AppendLine(
                    EveMonClient.FileVersionInfo.FileVersion);
            }
            catch
            {
                report.AppendLine("EVEMon Version: (unavailable)");
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
                TimeSpan uptime = EveMonClient.Uptime;
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
                var characters = EveMonClient.Characters;
                var esiKeys = EveMonClient.ESIKeys;
                var monitored = EveMonClient.MonitoredCharacters;

                int charCount = characters?.Count() ?? 0;
                int ccpCount = characters?.OfType<CCPCharacter>().Count() ?? 0;
                int localCount = charCount - ccpCount;
                int esiCount = esiKeys?.Count() ?? 0;
                int monCount = monitored?.Count() ?? 0;

                report.AppendLine($"  Characters Loaded: {charCount} ({ccpCount} CCP, {localCount} local)");
                report.AppendLine($"  ESI Keys: {esiCount}");
                report.AppendLine($"  Monitored: {monCount}");
                report.AppendLine($"  Data Loaded: {EveMonClient.IsDataLoaded}");
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
                var characters = EveMonClient.Characters;
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
                var esiKeys = EveMonClient.ESIKeys;
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
                if (EveMonClient.IsAlphaVersion)
                    channel = "Alpha";
                else if (EveMonClient.IsBetaVersion)
                    channel = "Beta";

                report.AppendLine($"Update Channel: {channel}");
                report.AppendLine($"  AutoCheck: {Settings.Updates.CheckEVEMonVersion}");
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
                foreach (string datafile in Datafile.GetFilesFrom(EveMonClient.EVEMonDataDir,
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
                traceStream = Util.GetFileStream(EveMonClient.TraceFileNameFullPath,
                    FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using (StreamReader traceReader = new StreamReader(traceStream))
                {
                    traceStream = null;
                    string line;
                    while ((line = traceReader.ReadLine()) != null)
                    {
                        if (!IsGameDataTraceLine(line))
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
        /// Returns true if a trace line contains game-specific data that should be
        /// excluded from diagnostic reports (asset locations, structure lookups,
        /// character details, market data, etc.).
        /// </summary>
        private static bool IsGameDataTraceLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;

            return line.Contains("Asset.UpdateLocation") ||
                   line.Contains("structure", StringComparison.OrdinalIgnoreCase) ||
                   line.Contains("EveIDToName") ||
                   line.Contains("CharacterDataQuerying") ||
                   line.Contains("CharacterSheet updated") ||
                   line.Contains("Booster detected") ||
                   line.Contains("Booster re-detected") ||
                   line.Contains("EveNotificationType") ||
                   line.Contains("EveNotificationTextParser") ||
                   line.Contains("Remaining ids:") ||
                   line.Contains("ECItemPricer") ||
                   line.Contains("FuzzworksItemPricer") ||
                   line.Contains("EMItemPricer") ||
                   line.Contains("ImageService") ||
                   line.Contains("IgbTcpListener") ||
                   line.Contains("GAnalyticsTracker") ||
                   line.Contains("Emailer") ||
                   line.Contains("CodeCompiler");
        }

        #endregion
    }
}
