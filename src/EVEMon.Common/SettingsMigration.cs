// Settings fork migration detection and handling (extracted from Settings.cs)
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using EVEMon.Common.Helpers;
using EVEMon.Common.Services;

namespace EVEMon.Common
{
    public static partial class Settings
    {
        /// <summary>
        /// Gets whether a migration from another EVEMon fork was detected on startup.
        /// If true, ESI tokens were cleared and users need to re-add their characters.
        /// </summary>
        public static bool MigrationFromOtherForkDetected { get; private set; }

        // Our fork identifier - used to detect settings from other EVEMon forks
        private const string OurForkId = "aliacollins";

        /// <summary>
        /// Result of fork migration detection.
        /// </summary>
        private class MigrationDetectionResult
        {
            public bool MigrationDetected { get; set; }
            public bool NeedsForkIdAdded { get; set; }
            public bool HasEsiKeys { get; set; }
            public string? DetectedForkId { get; set; }
            public int DetectedRevision { get; set; }
        }

        // Revision threshold for detecting peterhaneve's fork
        // peterhaneve uses auto-incrementing build numbers (e.g., 4986)
        // Our fork uses 0 for stable, 1-N for betas
        private const int PeterhaneveRevisionThreshold = 1000;

        /// <summary>
        /// Detects if the settings file is from another EVEMon fork.
        /// Uses forkId and revision number to distinguish between:
        /// - Our users (forkId matches OR forkId missing with low revision)
        /// - peterhaneve users (forkId missing with high revision > 1000)
        /// - Other fork users (forkId present but different)
        /// This method only detects - it does NOT show any UI or modify files.
        /// </summary>
        /// <param name="fileContent">The raw settings.xml content.</param>
        /// <returns>Detection result.</returns>
        private static MigrationDetectionResult DetectForkMigration(string fileContent)
        {
            var result = new MigrationDetectionResult();

            // Check for forkId attribute in the Settings root element
            var forkIdMatch = Regex.Match(fileContent, @"<Settings[^>]*\sforkId=""([^""]+)""",
                RegexOptions.IgnoreCase);
            string? forkId = forkIdMatch.Success ? forkIdMatch.Groups[1].Value : null;
            result.DetectedForkId = forkId;

            // Get revision number for distinguishing forks when forkId is missing
            // peterhaneve uses high revision numbers (e.g., 4986)
            // Our fork uses 0 for stable, 1-N for betas
            int revision = Util.GetRevisionNumber(fileContent);
            result.DetectedRevision = revision;

            // Check if there are any ESI keys with refresh tokens
            var hasEsiKeys = Regex.IsMatch(fileContent, @"<esikey[^>]+refreshToken=""[^""]+""",
                RegexOptions.IgnoreCase);
            result.HasEsiKeys = hasEsiKeys;

            AppServices.TraceService?.Trace($"DetectForkMigration: forkId='{forkId ?? "(none)"}', revision={revision}, hasEsiKeys={hasEsiKeys}");

            // Detection logic:
            // 1. forkId == "aliacollins" → Our user, no migration
            // 2. forkId present AND different → Migration from that fork
            // 3. forkId missing:
            //    - revision > 1000 → peterhaneve user (they use high build numbers) → Migration
            //    - revision <= 1000 → Our existing user (pre-forkId) → Just add forkId silently

            if (forkId == OurForkId)
            {
                // Case 1: Our fork with forkId - no migration needed
                AppServices.TraceService?.Trace("DetectForkMigration: forkId matches ours, no migration");
                result.MigrationDetected = false;
            }
            else if (forkId != null && forkId != OurForkId)
            {
                // Case 2: Different forkId explicitly set - definite migration from another fork
                AppServices.TraceService?.Trace($"DetectForkMigration: Different forkId '{forkId}' detected");
                if (hasEsiKeys)
                {
                    AppServices.TraceService?.Trace("DetectForkMigration: MIGRATION DETECTED - different forkId with ESI keys");
                    result.MigrationDetected = true;
                }
                else
                {
                    // Different fork but no ESI keys - just update forkId
                    AppServices.TraceService?.Trace("DetectForkMigration: Different forkId but no ESI keys, just need to update forkId");
                    result.MigrationDetected = false;
                    result.NeedsForkIdAdded = true;
                }
            }
            else if (forkId == null)
            {
                // Case 3: forkId missing - use revision to distinguish
                if (revision > PeterhaneveRevisionThreshold && hasEsiKeys)
                {
                    // High revision (peterhaneve uses ~4986) + ESI keys = peterhaneve user
                    AppServices.TraceService?.Trace($"DetectForkMigration: MIGRATION DETECTED - high revision ({revision}) indicates peterhaneve fork");
                    result.MigrationDetected = true;
                }
                else
                {
                    // Low revision (our fork uses 0-N) = our existing user pre-forkId
                    // Just need to add forkId silently, no migration message
                    AppServices.TraceService?.Trace($"DetectForkMigration: Low revision ({revision}), assuming our existing user");
                    result.MigrationDetected = false;
                    result.NeedsForkIdAdded = true;
                }
            }

            return result;
        }

        /// <summary>
        /// Updates the settings file: clears ESI keys and adds our forkId/forkVersion.
        /// Called after migration is detected.
        /// </summary>
        /// <param name="fileContent">The original file content.</param>
        /// <param name="filePath">The path to the settings file.</param>
        /// <returns>The modified content.</returns>
        private static string UpdateSettingsFileForMigration(string fileContent, string filePath)
        {
            string forkVersion = AppServices.DataStore.FileVersion;

            // Clear ESI keys
            string modifiedContent = Regex.Replace(fileContent,
                @"<esiKeys>.*?</esiKeys>",
                "<esiKeys></esiKeys>",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);

            // Add or update forkId attribute on the Settings element
            if (Regex.IsMatch(modifiedContent, @"<Settings[^>]*\sforkId=""[^""]*""", RegexOptions.IgnoreCase))
            {
                // Update existing forkId
                modifiedContent = Regex.Replace(modifiedContent,
                    @"(<Settings[^>]*\s)forkId=""[^""]*""",
                    $"$1forkId=\"{OurForkId}\"",
                    RegexOptions.IgnoreCase);
            }
            else
            {
                // Add forkId attribute
                modifiedContent = Regex.Replace(modifiedContent,
                    @"<Settings\s",
                    $"<Settings forkId=\"{OurForkId}\" ",
                    RegexOptions.IgnoreCase);
            }

            // Add or update forkVersion attribute
            if (Regex.IsMatch(modifiedContent, @"<Settings[^>]*\sforkVersion=""[^""]*""", RegexOptions.IgnoreCase))
            {
                // Update existing forkVersion
                modifiedContent = Regex.Replace(modifiedContent,
                    @"(<Settings[^>]*\s)forkVersion=""[^""]*""",
                    $"$1forkVersion=\"{forkVersion}\"",
                    RegexOptions.IgnoreCase);
            }
            else
            {
                // Add forkVersion attribute after forkId
                modifiedContent = Regex.Replace(modifiedContent,
                    @"(<Settings[^>]*forkId=""[^""]*"")",
                    $"$1 forkVersion=\"{forkVersion}\"",
                    RegexOptions.IgnoreCase);
            }

            try
            {
                File.WriteAllText(filePath, modifiedContent);
                AppServices.TraceService?.Trace($"UpdateSettingsFileForMigration: Cleared ESI keys and set forkId={OurForkId}, forkVersion={forkVersion}");
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace($"UpdateSettingsFileForMigration: Failed to update settings file: {ex.Message}");
            }

            return modifiedContent;
        }

        /// <summary>
        /// Adds our forkId and forkVersion to a settings file that doesn't have them.
        /// Called for fresh installs or when forkId is missing but no migration needed.
        /// </summary>
        /// <param name="fileContent">The original file content.</param>
        /// <param name="filePath">The path to the settings file.</param>
        /// <returns>The modified content.</returns>
        private static string AddForkIdToSettingsFile(string fileContent, string filePath)
        {
            string forkVersion = AppServices.DataStore.FileVersion;
            string modifiedContent = fileContent;
            bool modified = false;

            // Add forkId if missing
            if (!Regex.IsMatch(modifiedContent, @"<Settings[^>]*\sforkId=""[^""]*""", RegexOptions.IgnoreCase))
            {
                modifiedContent = Regex.Replace(modifiedContent,
                    @"<Settings\s",
                    $"<Settings forkId=\"{OurForkId}\" ",
                    RegexOptions.IgnoreCase);
                modified = true;
            }

            // Add forkVersion if missing
            if (!Regex.IsMatch(modifiedContent, @"<Settings[^>]*\sforkVersion=""[^""]*""", RegexOptions.IgnoreCase))
            {
                // Add forkVersion after forkId
                modifiedContent = Regex.Replace(modifiedContent,
                    @"(<Settings[^>]*forkId=""[^""]*"")",
                    $"$1 forkVersion=\"{forkVersion}\"",
                    RegexOptions.IgnoreCase);
                modified = true;
            }

            if (!modified)
            {
                return fileContent; // Nothing to add
            }

            try
            {
                File.WriteAllText(filePath, modifiedContent);
                AppServices.TraceService?.Trace($"AddForkIdToSettingsFile: Added forkId={OurForkId}, forkVersion={forkVersion}");
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace($"AddForkIdToSettingsFile: Failed to update settings file: {ex.Message}");
            }

            return modifiedContent;
        }

        /// <summary>
        /// Shows the appropriate migration message based on detection results and whether settings can be preserved.
        /// </summary>
        /// <param name="migration">The migration detection result.</param>
        /// <param name="settingsCanBePreserved">Whether the settings were successfully loaded and can be preserved.</param>
        private static void ShowMigrationMessage(MigrationDetectionResult migration, bool settingsCanBePreserved)
        {
            string message;
            string title = "Welcome to EVEMon 5.1";

            if (settingsCanBePreserved)
            {
                // Settings format is compatible - plans and settings will be preserved
                message = @"Welcome to EVEMon 5.1!

It looks like you're coming from a different version of EVEMon.

Due to how EVE's login system works, your characters need to be re-added. This is a one-time thing - ESI authentication tokens are tied to the specific EVEMon version that created them and cannot be transferred.

Your skill plans and other settings have been preserved.

To re-add your characters:
  1. Go to File > Add Character
  2. Log in with your EVE account
  3. Repeat for each character

Click OK to continue.";
            }
            else
            {
                // Settings format is incompatible - starting fresh
                message = @"Welcome to EVEMon 5.1!

It looks like you're coming from a different version of EVEMon.

Unfortunately, your settings file format is too old to migrate. EVEMon will start with fresh settings.

You'll need to add your characters:
  1. Go to File > Add Character
  2. Log in with your EVE account
  3. Repeat for each character

Click OK to continue.";
            }

            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
            MigrationFromOtherForkDetected = true;
        }
    }
}
