// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using EveLens.Common.Serialization.Settings;
using EveLens.Common.Services;

namespace EveLens.Common.Helpers
{
    public static partial class SettingsFileManager
    {
        #region Migration from XML

        /// <summary>
        /// Migrates settings from the legacy XML format to JSON.
        /// Zero translation — the same SerializableSettings object is serialized directly to JSON.
        /// </summary>
        /// <param name="xmlSettings">The deserialized XML settings.</param>
        public static async Task MigrateFromXmlAsync(SerializableSettings xmlSettings)
        {
            AppServices.TraceService?.Trace("begin - migrating from XML to JSON (direct format)");

            if (xmlSettings == null)
            {
                AppServices.TraceService?.Trace("No XML settings to migrate");
                return;
            }

            try
            {
                // Save directly as JSON — zero translation
                await SaveFromSerializableSettingsAsync(xmlSettings);

                // Rename XML to .migrated (don't delete — user can restore)
                string migratedPath = LegacySettingsFilePath + ".migrated";
                if (File.Exists(LegacySettingsFilePath))
                {
                    if (File.Exists(migratedPath))
                        File.Delete(migratedPath);
                    File.Move(LegacySettingsFilePath, migratedPath);
                    AppServices.TraceService?.Trace("Renamed settings.xml to settings.xml.migrated");
                }

                AppServices.TraceService?.Trace(
                    $"done - migrated {xmlSettings.Characters.Count} characters to settings.json");
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace($"Error during migration: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Save from SerializableSettings

        /// <summary>
        /// Saves settings to JSON format by serializing SerializableSettings directly.
        /// Zero translation — the same object used in-memory is written to disk.
        /// </summary>
        /// <param name="settings">The serializable settings to save.</param>
        public static async Task SaveFromSerializableSettingsAsync(SerializableSettings settings)
        {
            if (settings == null)
            {
                AppServices.TraceService?.Trace("SaveFromSerializableSettingsAsync: No settings to save");
                return;
            }

            try
            {
                await SaveMultiFileAsync(settings);
                AppServices.TraceService?.Trace(
                    $"SaveFromSerializableSettingsAsync: Saved {settings.Characters.Count} chars, {settings.Plans.Count} plans (multi-file)");
            }
            catch (Exception ex)
            {
                var inner = ex;
                while (inner.InnerException != null) inner = inner.InnerException;
                AppServices.TraceService?.Trace(
                    $"SaveFromSerializableSettingsAsync: Error: {inner.GetType().Name}: {inner.Message}");
                throw;
            }
        }

        #endregion

        #region Multi-File Write

        /// <summary>
        /// Writes a file synchronously using atomic temp-file + rename pattern.
        /// Synchronous companion to WriteFileAtomicAsync for hot paths.
        /// </summary>
        private static void WriteFileSyncAtomic(string filePath, string content)
        {
            string directory = Path.GetDirectoryName(filePath) ?? Path.GetTempPath();
            string tempPath = Path.Combine(directory, $".{Path.GetFileName(filePath)}.tmp");

            try
            {
                File.WriteAllText(tempPath, content);

                if (File.Exists(filePath))
                {
                    string backupPath = filePath + ".bak";
                    File.Replace(tempPath, filePath, backupPath);
                }
                else
                {
                    File.Move(tempPath, filePath);
                }
            }
            catch (IOException)
            {
                // File.Replace can fail on some filesystems; fall back to direct write
                try { File.WriteAllText(filePath, content); }
                catch { throw; }
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); }
                    catch { /* Ignore cleanup errors */ }
                }
            }
        }

        /// <summary>
        /// Saves settings as multiple atomic files. Synchronous — suitable for
        /// Settings.Save(), SmartSettingsManager, and shutdown paths.
        /// </summary>
        public static void SaveMultiFileSync(SerializableSettings settings)
        {
            if (settings == null)
                return;

            var (config, credentials, index, characters) = DecomposeSettings(settings);

            EnsureDirectoriesExist();

            // Write config.json
            string configJson = JsonSerializer.Serialize(config, s_jsonOptions);
            WriteFileSyncAtomic(ConfigFilePath, configJson);

            // Write credentials.json
            string credsJson = JsonSerializer.Serialize(credentials, s_jsonOptions);
            WriteFileSyncAtomic(CredentialsFilePath, credsJson);

            // Write each character as component files
            foreach (var charData in characters)
                SaveCharacterComponentsSync(charData);

            // Write index.json last — all character files are consistent before the index points to them
            string indexJson = JsonSerializer.Serialize(index, s_jsonOptions);
            WriteFileSyncAtomic(CharacterIndexFilePath, indexJson);

            // Remove orphaned character files/directories
            var activeIds = new HashSet<long>(characters.Select(c => c.CharacterId));
            RemoveOrphanedCharacterFiles(activeIds);

            AppServices.TraceService?.Trace(
                $"SaveMultiFileSync: {characters.Count} chars, config={configJson.Length}b, creds={credsJson.Length}b");
        }

        /// <summary>
        /// Saves settings as multiple atomic files, async variant.
        /// Used by migration and import paths.
        /// </summary>
        public static async Task SaveMultiFileAsync(SerializableSettings settings)
        {
            if (settings == null)
                return;

            var (config, credentials, index, characters) = DecomposeSettings(settings);

            EnsureDirectoriesExist();

            await SaveConfigAsync(config);
            await SaveCredentialsAsync(credentials);

            // Write each character as component files
            foreach (var charData in characters)
                await SaveCharacterComponentsAsync(charData);

            await SaveCharacterIndexAsync(index);

            // Remove orphaned character files/directories
            var activeIds = new HashSet<long>(characters.Select(c => c.CharacterId));
            RemoveOrphanedCharacterFiles(activeIds);

            AppServices.TraceService?.Trace(
                $"SaveMultiFileAsync: {characters.Count} chars written");
        }

        /// <summary>
        /// Removes character data (directories and/or legacy flat files) that no longer correspond to any active character.
        /// </summary>
        private static void RemoveOrphanedCharacterFiles(HashSet<long> activeIds)
        {
            try
            {
                foreach (long savedId in GetSavedCharacterIds())
                {
                    if (!activeIds.Contains(savedId))
                    {
                        // Remove component directory
                        string charDir = GetCharacterDirectory(savedId);
                        if (Directory.Exists(charDir))
                        {
                            Directory.Delete(charDir, recursive: true);
                            AppServices.TraceService?.Trace($"Removed orphaned character directory: {savedId}");
                        }

                        // Remove legacy flat file
                        string legacyPath = GetLegacyCharacterFilePath(savedId);
                        if (File.Exists(legacyPath))
                        {
                            File.Delete(legacyPath);
                            AppServices.TraceService?.Trace($"Removed orphaned legacy character file: {savedId}.json");
                        }
                        string bakPath = legacyPath + ".bak";
                        if (File.Exists(bakPath))
                            File.Delete(bakPath);
                    }
                }
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace($"Error cleaning orphaned character files: {ex.Message}");
            }
        }

        #endregion
    }
}
