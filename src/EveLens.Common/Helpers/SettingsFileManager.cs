// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using EveLens.Common.CloudStorageServices;
using EveLens.Common.Enumerations;
using EveLens.Common.Enumerations.UISettings;
using EveLens.Common.Scheduling;
using EveLens.Common.Serialization.Eve;
using EveLens.Common.Serialization.Settings;
using EveLens.Common.Services;
using EveLens.Common.SettingsObjects;

namespace EveLens.Common.Helpers
{
    /// <summary>
    /// Manages the new split settings file structure:
    /// - config.json: UI settings and preferences
    /// - credentials.json: ESI tokens (portable)
    /// - characters/{id}.json: Per-character data
    /// </summary>
    public static class SettingsFileManager
    {
        #region Constants

        private const string SettingsJsonFileName = "settings.json";
        private const string ConfigFileName = "config.json";
        private const string CredentialsFileName = "credentials.json";
        private const string CharactersFolderName = "characters";
        private const string CharacterIndexFileName = "index.json";
        private const string LegacySettingsFileName = "settings.xml";

        #endregion

        #region JSON Options

        /// <summary>
        /// Canonical JSON options for direct SerializableSettings serialization.
        /// No camelCase — property names match C# names exactly.
        /// Populate mode handles getter-only Collection&lt;T&gt; properties.
        /// </summary>
        internal static readonly JsonSerializerOptions DirectJsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate,
            Converters = { new JsonStringEnumConverter() }
        };

        // Legacy options for old Json* class format (backward compat during load)
        private static readonly JsonSerializerOptions s_jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

        private static readonly JsonSerializerOptions s_jsonReadOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        #endregion

        #region Paths

        /// <summary>
        /// Gets the base EveLens data directory.
        /// </summary>
        public static string DataDirectory => AppServices.ApplicationPaths.DataDirectory;

        /// <summary>
        /// Gets the full path to settings.json (new direct format).
        /// </summary>
        public static string SettingsJsonFilePath => Path.Combine(DataDirectory, SettingsJsonFileName);

        /// <summary>
        /// Gets the full path to config.json (legacy format).
        /// </summary>
        public static string ConfigFilePath => Path.Combine(DataDirectory, ConfigFileName);

        /// <summary>
        /// Gets the full path to credentials.json.
        /// </summary>
        public static string CredentialsFilePath => Path.Combine(DataDirectory, CredentialsFileName);

        /// <summary>
        /// Gets the characters folder path.
        /// </summary>
        public static string CharactersDirectory => Path.Combine(DataDirectory, CharactersFolderName);

        /// <summary>
        /// Gets the character index file path.
        /// </summary>
        public static string CharacterIndexFilePath => Path.Combine(CharactersDirectory, CharacterIndexFileName);

        /// <summary>
        /// Gets the legacy settings.xml path.
        /// </summary>
        public static string LegacySettingsFilePath => Path.Combine(DataDirectory, LegacySettingsFileName);

        /// <summary>
        /// Gets the file path for a specific character.
        /// </summary>
        public static string GetCharacterFilePath(long characterId)
            => Path.Combine(CharactersDirectory, $"{characterId}.json");

        #endregion

        #region Directory Management

        /// <summary>
        /// Ensures all required directories exist.
        /// </summary>
        public static void EnsureDirectoriesExist()
        {
            AppServices.TraceService?.Trace("begin");

            try
            {
                if (!Directory.Exists(DataDirectory))
                    Directory.CreateDirectory(DataDirectory);

                if (!Directory.Exists(CharactersDirectory))
                    Directory.CreateDirectory(CharactersDirectory);

                AppServices.TraceService?.Trace("done");
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace($"Error: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Detection

        /// <summary>
        /// Checks if JSON settings exist (new direct format or legacy multi-file format).
        /// </summary>
        public static bool JsonSettingsExist()
            => File.Exists(SettingsJsonFilePath) || File.Exists(ConfigFilePath);

        /// <summary>
        /// Checks if legacy XML settings exist.
        /// </summary>
        public static bool LegacySettingsExist()
            => File.Exists(LegacySettingsFilePath);

        /// <summary>
        /// Determines if migration from XML to JSON is needed.
        /// </summary>
        public static bool NeedsMigration()
            => LegacySettingsExist() && !JsonSettingsExist();

        /// <summary>
        /// Clears all JSON settings files.
        /// Used when resetting settings to factory defaults.
        /// </summary>
        public static void ClearAllJsonFiles(SemaphoreSlim? writeLock = null)
        {
            AppServices.TraceService?.Trace("begin");

            // Acquire write lock if provided to prevent racing with SmartSettingsManager writes
            writeLock?.Wait();
            try
            {
                // Delete settings.json (new direct format)
                if (File.Exists(SettingsJsonFilePath))
                    File.Delete(SettingsJsonFilePath);

                // Delete config.json (legacy format)
                if (File.Exists(ConfigFilePath))
                    File.Delete(ConfigFilePath);

                // Delete credentials.json (legacy format)
                if (File.Exists(CredentialsFilePath))
                    File.Delete(CredentialsFilePath);

                // Delete entire characters folder
                if (Directory.Exists(CharactersDirectory))
                    Directory.Delete(CharactersDirectory, recursive: true);

                AppServices.TraceService?.Trace("All JSON files cleared");
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace($"Error clearing JSON files: {ex.Message}");
            }
            finally
            {
                writeLock?.Release();
            }
        }

        /// <summary>
        /// Clears JSON files to force re-migration from XML.
        /// Used when restoring settings from a backup.
        /// </summary>
        public static void ClearForReMigration()
        {
            AppServices.TraceService?.Trace("begin");

            try
            {
                // Clear JSON files - they'll be recreated from XML on next startup
                ClearAllJsonFiles();

                // Also restore the migrated settings file if it exists
                string migratedPath = LegacySettingsFilePath + ".migrated";
                if (File.Exists(migratedPath) && !File.Exists(LegacySettingsFilePath))
                {
                    File.Move(migratedPath, LegacySettingsFilePath);
                    AppServices.TraceService?.Trace("Restored settings.xml.migrated to settings.xml");
                }

                AppServices.TraceService?.Trace("done - JSON cleared for re-migration");
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace($"Error clearing for re-migration: {ex.Message}");
            }
        }

        #endregion

        #region Config (UI Settings)

        /// <summary>
        /// Loads the config.json file.
        /// </summary>
        public static async Task<JsonConfig> LoadConfigAsync()
        {
            AppServices.TraceService?.Trace("begin");

            // Try primary file
            var config = await TryLoadJsonAsync<JsonConfig>(ConfigFilePath);
            if (config != null)
            {
                AppServices.TraceService?.Trace("done - loaded from primary");
                return config;
            }

            // Try backup file (.bak created by atomic writes)
            config = await TryLoadJsonAsync<JsonConfig>(ConfigFilePath + ".bak");
            if (config != null)
            {
                AppServices.TraceService?.Trace("Recovered config from backup");
                try
                {
                    // Restore the primary from the backup
                    string json = JsonSerializer.Serialize(config, s_jsonOptions);
                    await WriteFileAtomicAsync(ConfigFilePath, json);
                }
                catch
                {
                    // Best-effort restore — we still have the loaded data
                }
                return config;
            }

            AppServices.TraceService?.Trace("No config found, returning defaults");
            return new JsonConfig();
        }

        /// <summary>
        /// Saves the config.json file.
        /// </summary>
        public static async Task SaveConfigAsync(JsonConfig config)
        {
            AppServices.TraceService?.Trace("begin");

            try
            {
                EnsureDirectoriesExist();
                string json = JsonSerializer.Serialize(config, s_jsonOptions);
                await WriteFileAtomicAsync(ConfigFilePath, json);
                AppServices.TraceService?.Trace($"done - saved {json.Length} bytes");
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace($"Error: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Credentials (ESI Tokens)

        /// <summary>
        /// Loads the credentials.json file.
        /// </summary>
        public static async Task<JsonCredentials> LoadCredentialsAsync()
        {
            AppServices.TraceService?.Trace("begin");

            // Try primary file
            var creds = await TryLoadJsonAsync<JsonCredentials>(CredentialsFilePath);
            if (creds != null)
            {
                AppServices.TraceService?.Trace($"done - loaded {creds.EsiKeys?.Count ?? 0} ESI keys");
                return creds;
            }

            // Try backup file
            creds = await TryLoadJsonAsync<JsonCredentials>(CredentialsFilePath + ".bak");
            if (creds != null)
            {
                AppServices.TraceService?.Trace("Recovered credentials from backup");
                try
                {
                    string json = JsonSerializer.Serialize(creds, s_jsonOptions);
                    await WriteFileAtomicAsync(CredentialsFilePath, json);
                }
                catch { }
                return creds;
            }

            AppServices.TraceService?.Trace("No credentials found, returning empty");
            return new JsonCredentials();
        }

        /// <summary>
        /// Saves the credentials.json file.
        /// </summary>
        public static async Task SaveCredentialsAsync(JsonCredentials credentials)
        {
            AppServices.TraceService?.Trace("begin");

            try
            {
                EnsureDirectoriesExist();

                if (credentials != null)
                    credentials.LastSaved = DateTime.UtcNow;

                string json = JsonSerializer.Serialize(credentials, s_jsonOptions);
                await WriteFileAtomicAsync(CredentialsFilePath, json);
                AppServices.TraceService?.Trace($"done - saved {credentials?.EsiKeys?.Count ?? 0} ESI keys");
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace($"Error: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Character Index

        /// <summary>
        /// Loads the character index (list of all characters).
        /// </summary>
        public static async Task<JsonCharacterIndex> LoadCharacterIndexAsync()
        {
            AppServices.TraceService?.Trace("begin");

            // Try primary file
            var index = await TryLoadJsonAsync<JsonCharacterIndex>(CharacterIndexFilePath);
            if (index != null)
            {
                AppServices.TraceService?.Trace($"done - loaded {index.Characters?.Count ?? 0} character entries");
                return index;
            }

            // Try backup file
            index = await TryLoadJsonAsync<JsonCharacterIndex>(CharacterIndexFilePath + ".bak");
            if (index != null)
            {
                AppServices.TraceService?.Trace("Recovered character index from backup");
                try
                {
                    string json = JsonSerializer.Serialize(index, s_jsonOptions);
                    await WriteFileAtomicAsync(CharacterIndexFilePath, json);
                }
                catch { }
                return index;
            }

            AppServices.TraceService?.Trace("No character index found, returning empty");
            return new JsonCharacterIndex();
        }

        /// <summary>
        /// Saves the character index.
        /// </summary>
        public static async Task SaveCharacterIndexAsync(JsonCharacterIndex index)
        {
            AppServices.TraceService?.Trace("begin");

            try
            {
                EnsureDirectoriesExist();
                string json = JsonSerializer.Serialize(index, s_jsonOptions);
                await WriteFileAtomicAsync(CharacterIndexFilePath, json);
                AppServices.TraceService?.Trace($"done - saved {index?.Characters?.Count ?? 0} character entries");
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace($"Error: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Character Data

        /// <summary>
        /// Loads a specific character's data.
        /// </summary>
        public static async Task<JsonCharacterData?> LoadCharacterAsync(long characterId)
        {
            AppServices.TraceService?.Trace($"begin - character {characterId}");

            string filePath = GetCharacterFilePath(characterId);

            // Try primary file
            var character = await TryLoadJsonAsync<JsonCharacterData>(filePath);
            if (character != null)
            {
                AppServices.TraceService?.Trace($"done - loaded character {characterId}");
                return character;
            }

            // Try backup file
            character = await TryLoadJsonAsync<JsonCharacterData>(filePath + ".bak");
            if (character != null)
            {
                AppServices.TraceService?.Trace($"Recovered character {characterId} from backup");
                try
                {
                    string json = JsonSerializer.Serialize(character, s_jsonOptions);
                    await WriteFileAtomicAsync(filePath, json);
                }
                catch { }
                return character;
            }

            AppServices.TraceService?.Trace($"Character file not found: {characterId}");
            return null;
        }

        /// <summary>
        /// Saves a specific character's data.
        /// </summary>
        public static async Task SaveCharacterAsync(JsonCharacterData character)
        {
            if (character == null)
                return;

            AppServices.TraceService?.Trace($"begin - character {character.CharacterId}");

            try
            {
                EnsureDirectoriesExist();
                string filePath = GetCharacterFilePath(character.CharacterId);
                string json = JsonSerializer.Serialize(character, s_jsonOptions);
                await WriteFileAtomicAsync(filePath, json);
                AppServices.TraceService?.Trace($"done - saved character {character.CharacterId} ({json.Length} bytes)");
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace($"Error saving character {character.CharacterId}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Deletes a character's data file.
        /// </summary>
        public static void DeleteCharacter(long characterId)
        {
            AppServices.TraceService?.Trace($"begin - character {characterId}");

            string filePath = GetCharacterFilePath(characterId);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                AppServices.TraceService?.Trace($"done - deleted character {characterId}");
            }
        }

        /// <summary>
        /// Gets all character IDs that have data files.
        /// </summary>
        public static IEnumerable<long> GetSavedCharacterIds()
        {
            if (!Directory.Exists(CharactersDirectory))
                yield break;

            foreach (string file in Directory.GetFiles(CharactersDirectory, "*.json"))
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                if (fileName == "index")
                    continue;

                if (long.TryParse(fileName, out long characterId))
                    yield return characterId;
            }
        }

        #endregion

        #region Atomic File Writing

        /// <summary>
        /// Writes a file atomically using a temp file and rename.
        /// </summary>
        private static async Task WriteFileAtomicAsync(string filePath, string content)
        {
            string directory = Path.GetDirectoryName(filePath) ?? Path.GetTempPath();
            string tempPath = Path.Combine(directory, $".{Path.GetFileName(filePath)}.tmp");

            try
            {
                // Write to temp file first
                await File.WriteAllTextAsync(tempPath, content);

                if (File.Exists(filePath))
                {
                    // File.Replace is a single OS operation: replaces target with source
                    // and moves the old target to the backup path (.bak).
                    // This is truly atomic — no window where both files are missing.
                    string backupPath = filePath + ".bak";
                    File.Replace(tempPath, filePath, backupPath);
                }
                else
                {
                    // No existing file to replace — just move temp into place
                    File.Move(tempPath, filePath);
                }
            }
            finally
            {
                // Clean up temp file if it still exists (e.g. Replace/Move failed)
                if (File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); }
                    catch { /* Ignore cleanup errors */ }
                }
            }
        }

        /// <summary>
        /// Attempts to load and deserialize a JSON file. Returns null on any failure.
        /// </summary>
        private static async Task<T?> TryLoadJsonAsync<T>(string filePath) where T : class
        {
            if (!File.Exists(filePath))
                return null;

            try
            {
                string json = await File.ReadAllTextAsync(filePath);
                return JsonSerializer.Deserialize<T>(json, s_jsonReadOptions);
            }
            catch
            {
                return null;
            }
        }

        #endregion

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
                EnsureDirectoriesExist();

                string json;
                try
                {
                    json = JsonSerializer.Serialize(settings, DirectJsonOptions);
                    AppServices.TraceService?.Trace(
                        $"SaveFromSerializableSettingsAsync: Serialized {settings.Characters.Count} chars, {settings.Plans.Count} plans ({json.Length} bytes)");
                }
                catch (Exception serEx)
                {
                    var inner = serEx;
                    while (inner.InnerException != null) inner = inner.InnerException;
                    AppServices.TraceService?.Trace(
                        $"SaveFromSerializableSettingsAsync: JSON serialization failed: {inner.GetType().Name}: {inner.Message}");
                    throw;
                }

                try
                {
                    await WriteFileAtomicAsync(SettingsJsonFilePath, json);
                }
                catch (Exception ioEx)
                {
                    // WriteFileAtomicAsync with File.Replace can fail on some Linux filesystems.
                    // Fall back to direct write.
                    AppServices.TraceService?.Trace(
                        $"SaveFromSerializableSettingsAsync: Atomic write failed ({ioEx.Message}), falling back to direct write");
                    await File.WriteAllTextAsync(SettingsJsonFilePath, json).ConfigureAwait(false);
                }

                AppServices.TraceService?.Trace(
                    $"SaveFromSerializableSettingsAsync: Saved to {SettingsJsonFilePath}");
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

        #region Combined Backup Format

        /// <summary>
        /// Exports all settings to a single combined JSON backup file.
        /// Uses direct serialization of SerializableSettings — zero translation.
        /// Used by File > Save Settings menu.
        /// </summary>
        /// <param name="filePath">The path to save the backup to.</param>
        /// <param name="settings">The settings to export.</param>
        public static async Task ExportBackupAsync(string filePath, SerializableSettings settings)
        {
            AppServices.TraceService?.Trace($"begin - exporting to {filePath}");

            if (settings == null)
            {
                AppServices.TraceService?.Trace("No settings to export");
                return;
            }

            try
            {
                string json = JsonSerializer.Serialize(settings, DirectJsonOptions);
                await WriteFileAtomicAsync(filePath, json);

                AppServices.TraceService?.Trace(
                    $"done - exported {settings.Characters.Count} characters ({json.Length} bytes)");
            }
            catch (Exception ex)
            {
                var inner = ex;
                while (inner.InnerException != null) inner = inner.InnerException;
                AppServices.TraceService?.Trace(
                    $"Error exporting backup: {inner.GetType().FullName}: {inner.Message} at {inner.StackTrace?.Split('\n').FirstOrDefault()?.Trim()}");
                throw;
            }
        }

        /// <summary>
        /// Imports settings from a JSON backup file.
        /// Tries new direct format first, then legacy JsonBackup format.
        /// Used by File > Restore Settings menu.
        /// </summary>
        /// <param name="filePath">The path to the backup file.</param>
        /// <returns>True if import was successful.</returns>
        public static async Task<bool> ImportBackupAsync(string filePath)
        {
            AppServices.TraceService?.Trace($"begin - importing from {filePath}");

            try
            {
                string json = await File.ReadAllTextAsync(filePath);

                // Try direct SerializableSettings format first
                SerializableSettings? settings = null;
                try
                {
                    settings = JsonSerializer.Deserialize<SerializableSettings>(json, DirectJsonOptions);
                }
                catch
                {
                    // Not in direct format, try legacy
                }

                // Detect genuine direct-format deserialization vs half-parsed old-format.
                // A real SerializableSettings will have ForkId, non-zero Revision, or actual data.
                bool isDirectFormat = settings != null &&
                    (settings.ForkId != null || settings.Revision > 0 ||
                     settings.Characters.Count > 0 || settings.ESIKeys.Count > 0);
                if (isDirectFormat)
                {
                    // Direct format — save it
                    ClearAllJsonFiles();
                    await SaveFromSerializableSettingsAsync(settings);
                    AppServices.TraceService?.Trace(
                        $"done - imported {settings.Characters.Count} characters from direct format backup");
                    return true;
                }

                // Fall back to legacy JsonBackup format
                var backup = JsonSerializer.Deserialize<JsonBackup>(json, s_jsonOptions);

                if (backup == null)
                {
                    AppServices.TraceService?.Trace("Failed to deserialize backup");
                    return false;
                }

                // Clear existing JSON files
                ClearAllJsonFiles();
                EnsureDirectoriesExist();

                // Save config
                if (backup.Config != null)
                {
                    await SaveConfigAsync(backup.Config);
                }

                // Save credentials
                if (backup.Credentials != null)
                {
                    await SaveCredentialsAsync(backup.Credentials);
                }

                // Save characters
                var index = new JsonCharacterIndex
                {
                    Version = 1,
                    LastSaved = DateTime.UtcNow,
                    MonitoredCharacterIds = backup.MonitoredCharacterIds ?? new List<long>()
                };

                foreach (var character in backup.Characters ?? new List<JsonCharacterData>())
                {
                    await SaveCharacterAsync(character);
                    index.Characters.Add(new JsonCharacterIndexEntry
                    {
                        CharacterId = character.CharacterId,
                        Name = character.Name,
                        CorporationName = character.CorporationName,
                        AllianceName = character.AllianceName,
                        IsUriCharacter = !string.IsNullOrEmpty(character.UriAddress),
                        LastUpdated = DateTime.UtcNow
                    });
                }

                await SaveCharacterIndexAsync(index);

                AppServices.TraceService?.Trace($"done - imported {backup.Characters?.Count ?? 0} characters from legacy backup");
                return true;
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace($"Error importing backup: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if a file is a JSON backup file (direct or legacy format).
        /// </summary>
        public static bool IsJsonBackupFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            if (extension != ".json")
                return false;

            try
            {
                // Quick check - look for format markers
                string content = File.ReadAllText(filePath);
                // Direct format: has "Characters" and "ESIKeys" (SerializableSettings)
                // Legacy format: has "ForkId" and "Characters" (JsonBackup)
                return content.Contains("\"Characters\"") &&
                    (content.Contains("\"ESIKeys\"") || content.Contains("\"ForkId\""));
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Load from JSON to SerializableSettings

        /// <summary>
        /// Loads settings from JSON. Tries the new direct format (settings.json) first,
        /// then falls back to the legacy multi-file format (config.json + credentials.json + characters/).
        /// </summary>
        /// <returns>SerializableSettings populated from JSON files, or null if loading fails.</returns>
        public static async Task<SerializableSettings?> LoadToSerializableSettingsAsync()
        {
            AppServices.TraceService?.Trace("begin - loading from JSON format");

            // Priority 1: New direct format (settings.json = SerializableSettings)
            if (File.Exists(SettingsJsonFilePath))
            {
                try
                {
                    string json = await File.ReadAllTextAsync(SettingsJsonFilePath);
                    var settings = JsonSerializer.Deserialize<SerializableSettings>(json, DirectJsonOptions);

                    if (settings != null)
                    {
                        AppServices.TraceService?.Trace(
                            $"done - loaded {settings.Characters.Count} characters from settings.json (direct format)");
                        return settings;
                    }
                }
                catch (Exception ex)
                {
                    AppServices.TraceService?.Trace($"Failed to load settings.json: {ex.Message}");
                }

                // Try backup
                string backupPath = SettingsJsonFilePath + ".bak";
                if (File.Exists(backupPath))
                {
                    try
                    {
                        string backupJson = await File.ReadAllTextAsync(backupPath);
                        var settings = JsonSerializer.Deserialize<SerializableSettings>(backupJson, DirectJsonOptions);
                        if (settings != null)
                        {
                            AppServices.TraceService?.Trace("Recovered settings from settings.json.bak");
                            return settings;
                        }
                    }
                    catch
                    {
                        // Backup also corrupt
                    }
                }
            }

            // Priority 1b: settings.json.bak when primary doesn't exist
            string settingsBackupPath = SettingsJsonFilePath + ".bak";
            if (!File.Exists(SettingsJsonFilePath) && File.Exists(settingsBackupPath))
            {
                try
                {
                    string backupJson = await File.ReadAllTextAsync(settingsBackupPath);
                    var settings = JsonSerializer.Deserialize<SerializableSettings>(backupJson, DirectJsonOptions);
                    if (settings != null)
                    {
                        AppServices.TraceService?.Trace("Recovered settings from settings.json.bak (primary missing)");
                        return settings;
                    }
                }
                catch
                {
                    // Backup also corrupt
                }
            }

            // Priority 2: Legacy multi-file format (config.json + credentials.json + characters/)
            if (File.Exists(ConfigFilePath))
            {
                return await LoadFromLegacyJsonFormatAsync();
            }

            AppServices.TraceService?.Trace("No JSON settings found");
            return null;
        }

        /// <summary>
        /// Loads settings from the legacy multi-file JSON format (config.json, credentials.json, characters/).
        /// This is kept for backward compatibility with users who migrated before the direct format was introduced.
        /// </summary>
        private static async Task<SerializableSettings?> LoadFromLegacyJsonFormatAsync()
        {
            AppServices.TraceService?.Trace("Loading from legacy multi-file JSON format");

            try
            {
                // Load config
                var config = await LoadConfigAsync();
                if (config == null)
                {
                    AppServices.TraceService?.Trace("Failed to load config.json");
                    return null;
                }

                // Load credentials
                var credentials = await LoadCredentialsAsync();

                // Load character index
                var index = await LoadCharacterIndexAsync();

                // Create SerializableSettings
                var settings = new SerializableSettings
                {
                    ForkId = config.ForkId ?? "aliacollins",
                    ForkVersion = config.ForkVersion ?? string.Empty,
                    Revision = Settings.Revision,
                    SSOClientID = config.SSOClientID ?? string.Empty,
                    SSOClientSecret = config.SSOClientSecret ?? string.Empty,
                    UI = config.UI ?? new UISettings(),
                    G15 = config.G15 ?? new G15Settings(),
                    Proxy = config.Proxy ?? new ProxySettings(),
                    Updates = config.Updates ?? new UpdateSettings(),
                    Calendar = config.Calendar ?? new CalendarSettings(),
                    Exportation = config.Exportation ?? new ExportationSettings(),
                    MarketPricer = config.MarketPricer ?? new MarketPricerSettings(),
                    Notifications = config.Notifications ?? new NotificationSettings(),
                    LoadoutsProvider = config.LoadoutsProvider ?? new LoadoutsProviderSettings(),
                    PortableEveInstallations = config.PortableEveInstallations ?? new PortableEveInstallationsSettings(),
                    CloudStorageServiceProvider = config.CloudStorageServiceProvider ?? new CloudStorageServiceProviderSettings(),
                    Scheduler = config.Scheduler ?? new SchedulerSettings()
                };

                // Convert ESI keys
                foreach (var esiKey in credentials?.EsiKeys ?? new List<JsonEsiKey>())
                {
                    var serialKey = new SerializableESIKey
                    {
                        ID = esiKey.CharacterId,
                        RefreshToken = esiKey.RefreshToken ?? string.Empty,
                        Monitored = esiKey.Monitored,
                    };

                    // Migrate: use AuthorizedScopes if present, else derive from legacy AccessMask
                    if (esiKey.AuthorizedScopes != null && esiKey.AuthorizedScopes.Count > 0)
                    {
                        serialKey.AuthorizedScopes = new List<string>(esiKey.AuthorizedScopes);
                    }
#pragma warning disable CS0618 // AccessMask is obsolete
                    else if (esiKey.AccessMask == ulong.MaxValue)
                    {
                        serialKey.AuthorizedScopes = new List<string>(Services.EsiScopePresets.AllScopes);
                    }
#pragma warning restore CS0618

                    settings.ESIKeys.Add(serialKey);
                }

                // Build maps for character ID to Guid and UISettings
                var characterIdToGuid = new Dictionary<long, Guid>();
                var characterIdToUISettings = new Dictionary<long, CharacterUISettings>();

                // Load each character
                foreach (var entry in index?.Characters ?? new List<JsonCharacterIndexEntry>())
                {
                    var characterData = await LoadCharacterAsync(entry.CharacterId);
                    if (characterData == null)
                        continue;

                    var character = ConvertToSerializableCharacter(characterData, entry.IsUriCharacter);
                    if (character != null)
                    {
                        settings.Characters.Add(character);
                        characterIdToGuid[characterData.CharacterId] = character.Guid;

                        // Store UISettings if available
                        if (characterData.UISettings != null)
                        {
                            characterIdToUISettings[characterData.CharacterId] = characterData.UISettings;
                        }

                        // Convert plans for this character
                        foreach (var plan in characterData.Plans ?? new List<JsonPlan>())
                        {
                            var serializablePlan = ConvertToSerializablePlan(plan, character.Guid);
                            if (serializablePlan != null)
                            {
                                settings.Plans.Add(serializablePlan);
                            }
                        }
                    }
                }

                // Set monitored characters with their UISettings
                foreach (var charId in index?.MonitoredCharacterIds ?? new List<long>())
                {
                    if (characterIdToGuid.TryGetValue(charId, out Guid guid))
                    {
                        var monitored = new MonitoredCharacterSettings { CharacterGuid = guid };

                        // Restore UISettings if available
                        if (characterIdToUISettings.TryGetValue(charId, out var uiSettings))
                        {
                            monitored.Settings = uiSettings;
                        }

                        settings.MonitoredCharacters.Add(monitored);
                    }
                }

                AppServices.TraceService?.Trace($"done - loaded {settings.Characters.Count} characters from legacy JSON format");
                return settings;
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace($"Error loading from legacy JSON: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Converts JsonCharacterData to SerializableSettingsCharacter.
        /// </summary>
        private static SerializableSettingsCharacter? ConvertToSerializableCharacter(JsonCharacterData json, bool isUriCharacter)
        {
            if (json == null)
                return null;

            SerializableSettingsCharacter character;

            if (isUriCharacter)
            {
                character = new SerializableUriCharacter
                {
                    Address = json.UriAddress ?? string.Empty
                };
            }
            else
            {
                var ccpCharacter = new SerializableCCPCharacter();

                // Skill queue
                foreach (var queueEntry in json.SkillQueue ?? new List<JsonSkillQueueEntry>())
                {
                    ccpCharacter.SkillQueue.Add(new SerializableQueuedSkill
                    {
                        ID = queueEntry.TypeId,
                        Level = queueEntry.Level,
                        StartTime = queueEntry.StartTime,
                        EndTime = queueEntry.EndTime,
                        StartSP = queueEntry.StartSP,
                        EndSP = queueEntry.EndSP
                    });
                }

                character = ccpCharacter;
            }

            // Common properties
            character.Guid = Guid.NewGuid(); // Generate new Guid for internal tracking
            character.ID = json.CharacterId;
            character.Name = json.Name ?? string.Empty;
            character.Birthday = json.Birthday;
            character.Race = json.Race ?? string.Empty;
            character.BloodLine = json.Bloodline ?? string.Empty;
            character.Ancestry = json.Ancestry ?? string.Empty;
            character.Gender = json.Gender ?? string.Empty;
            character.CorporationID = json.CorporationId;
            character.CorporationName = json.CorporationName ?? string.Empty;
            character.AllianceID = json.AllianceId;
            character.AllianceName = json.AllianceName ?? string.Empty;
            character.FactionID = (int)json.FactionId;
            character.FactionName = json.FactionName ?? string.Empty;
            character.Balance = json.Balance;
            character.HomeStationID = json.HomeStationId;
            character.FreeSkillPoints = json.FreeSkillPoints;

            // Character status and settings
            character.CloneState = json.CloneState ?? "Auto";
            character.Label = json.Label ?? string.Empty;
            character.ShipName = json.ShipName ?? string.Empty;
            character.ShipTypeName = json.ShipTypeName ?? string.Empty;
            character.SecurityStatus = json.SecurityStatus;
            // Note: LastKnownLocation is derived from location data, not stored directly

            // Remaps and jump clones
            character.FreeRespecs = (short)json.FreeRespecs;
            character.CloneJumpDate = json.CloneJumpDate;
            character.LastRespecDate = json.LastRespecDate;
            character.LastTimedRespec = json.LastTimedRespec;
            character.RemoteStationDate = json.RemoteStationDate;
            character.JumpActivationDate = json.JumpActivationDate;
            character.JumpFatigueDate = json.JumpFatigueDate;
            character.JumpLastUpdateDate = json.JumpLastUpdateDate;

            // Employment history
            foreach (var record in json.EmploymentHistory ?? new List<JsonEmploymentRecord>())
            {
                character.EmploymentHistory.Add(new SerializableEmploymentHistory
                {
                    CorporationID = record.CorporationId,
                    CorporationName = record.CorporationName ?? string.Empty,
                    StartDate = record.StartDate
                });
            }

            // Attributes
            character.Attributes = new SerializableCharacterAttributes
            {
                Intelligence = json.Intelligence,
                Memory = json.Memory,
                Charisma = json.Charisma,
                Perception = json.Perception,
                Willpower = json.Willpower
            };

            // Skills
            foreach (var skill in json.Skills ?? new List<JsonSkill>())
            {
                character.Skills.Add(new SerializableCharacterSkill
                {
                    ID = skill.TypeId,
                    Name = skill.Name ?? string.Empty,
                    Level = skill.Level,
                    ActiveLevel = skill.ActiveLevel,
                    Skillpoints = skill.Skillpoints,
                    IsKnown = skill.IsKnown,
                    OwnsBook = skill.OwnsBook
                });
            }

            // Implant sets
            if (json.ImplantSets?.Count > 0)
            {
                character.ImplantSets = new SerializableImplantSetCollection();

                foreach (var implantSet in json.ImplantSets)
                {
                    var set = ConvertToSerializableImplantSet(implantSet);
                    if (set != null)
                    {
                        // Classify by Type field (active/jump/custom).
                        // Falls back to name prefix for backward compat with older JSON without Type.
                        string type = implantSet.Type ?? "custom";
                        if (type == "active" || character.ImplantSets.ActiveClone == null)
                        {
                            character.ImplantSets.ActiveClone = set;
                        }
                        else if (type == "jump" || implantSet.Name?.StartsWith("Jump Clone") == true)
                        {
                            character.ImplantSets.JumpClones.Add(set);
                        }
                        else
                        {
                            character.ImplantSets.CustomSets.Add(set);
                        }
                    }
                }
            }

            return character;
        }

        /// <summary>
        /// Converts JsonImplantSet to SerializableSettingsImplantSet.
        /// </summary>
        private static SerializableSettingsImplantSet? ConvertToSerializableImplantSet(JsonImplantSet json)
        {
            if (json == null)
                return null;

            var set = new SerializableSettingsImplantSet
            {
                Name = json.Name ?? string.Empty
            };

            // Map implants by slot
            foreach (var implant in json.Implants ?? new List<JsonImplant>())
            {
                string value = !string.IsNullOrEmpty(implant.Name) ? implant.Name : implant.TypeId.ToString();

                switch (implant.Slot)
                {
                    case 1: set.Intelligence = value; break;
                    case 2: set.Memory = value; break;
                    case 3: set.Willpower = value; break;
                    case 4: set.Perception = value; break;
                    case 5: set.Charisma = value; break;
                    case 6: set.Slot6 = value; break;
                    case 7: set.Slot7 = value; break;
                    case 8: set.Slot8 = value; break;
                    case 9: set.Slot9 = value; break;
                    case 10: set.Slot10 = value; break;
                }
            }

            return set;
        }

        /// <summary>
        /// Converts JsonPlan to SerializablePlan.
        /// </summary>
        private static SerializablePlan? ConvertToSerializablePlan(JsonPlan json, Guid characterGuid)
        {
            if (json == null)
                return null;

            var plan = new SerializablePlan
            {
                Name = json.Name ?? string.Empty,
                Description = json.Description ?? string.Empty,
                Owner = characterGuid,
                SortingPreferences = new PlanSorting
                {
                    Criteria = Enum.TryParse<PlanEntrySort>(json.SortCriteria, out var criteria) ? criteria : PlanEntrySort.None,
                    Order = Enum.TryParse<ThreeStateSortOrder>(json.SortOrder, out var order) ? order : ThreeStateSortOrder.None,
                    GroupByPriority = json.GroupByPriority
                }
            };

            foreach (var entry in json.Entries ?? new List<JsonPlanEntry>())
            {
                var planEntry = new SerializablePlanEntry
                {
                    ID = entry.SkillId,
                    SkillName = entry.SkillName ?? string.Empty,
                    Level = entry.Level,
                    Type = Enum.TryParse<PlanEntryType>(entry.Type, out var type) ? type : PlanEntryType.Planned,
                    Priority = entry.Priority,
                    Notes = entry.Notes ?? string.Empty
                };

                // Restore plan groups
                if (entry.PlanGroups != null)
                {
                    foreach (var group in entry.PlanGroups)
                        planEntry.PlanGroups.Add(group);
                }

                // Restore remapping point if present
                if (entry.Remapping != null)
                {
                    planEntry.Remapping = new SerializableRemappingPoint
                    {
                        Status = Enum.TryParse<RemappingPointStatus>(entry.Remapping.Status, out var status)
                            ? status : RemappingPointStatus.NotComputed,
                        Perception = entry.Remapping.Perception,
                        Intelligence = entry.Remapping.Intelligence,
                        Memory = entry.Remapping.Memory,
                        Willpower = entry.Remapping.Willpower,
                        Charisma = entry.Remapping.Charisma,
                        Description = entry.Remapping.Description ?? string.Empty
                    };
                }

                plan.Entries.Add(planEntry);
            }

            // Restore invalid entries
            foreach (var invalid in json.InvalidEntries ?? new List<JsonInvalidPlanEntry>())
            {
                plan.InvalidEntries.Add(new SerializableInvalidPlanEntry
                {
                    SkillName = invalid.SkillName ?? string.Empty,
                    PlannedLevel = invalid.PlannedLevel,
                    Acknowledged = invalid.Acknowledged
                });
            }

            return plan;
        }

        #endregion
    }

    #region JSON Data Classes

    /// <summary>
    /// Combined backup format - all settings in one file for export/import.
    /// </summary>
    public class JsonBackup
    {
        public int Version { get; set; } = 1;
        public string? ForkId { get; set; }
        public string? ForkVersion { get; set; }
        public DateTime ExportedAt { get; set; }
        public JsonConfig? Config { get; set; }
        public JsonCredentials? Credentials { get; set; }
        public List<JsonCharacterData>? Characters { get; set; } = new List<JsonCharacterData>();
        public List<long>? MonitoredCharacterIds { get; set; } = new List<long>();
    }

    /// <summary>
    /// Root config.json structure - UI settings and preferences.
    /// </summary>
    public class JsonConfig
    {
        public int Version { get; set; } = 1;
        public string? ForkId { get; set; } = "aliacollins";
        public string? ForkVersion { get; set; }
        public DateTime LastSaved { get; set; } = DateTime.UtcNow;

        // SSO credentials (custom overrides persisted from user settings)
        public string? SSOClientID { get; set; }
        public string? SSOClientSecret { get; set; }

        // Settings objects (will be populated from existing settings classes)
        public UISettings? UI { get; set; }
        public G15Settings? G15 { get; set; }
        public ProxySettings? Proxy { get; set; }
        public UpdateSettings? Updates { get; set; }
        public CalendarSettings? Calendar { get; set; }
        public ExportationSettings? Exportation { get; set; }
        public MarketPricerSettings? MarketPricer { get; set; }
        public NotificationSettings? Notifications { get; set; }
        public LoadoutsProviderSettings? LoadoutsProvider { get; set; }
        public PortableEveInstallationsSettings? PortableEveInstallations { get; set; }
        public CloudStorageServiceProviderSettings? CloudStorageServiceProvider { get; set; }
        public SchedulerSettings? Scheduler { get; set; }
    }

    /// <summary>
    /// Root credentials.json structure - ESI authentication tokens.
    /// </summary>
    public class JsonCredentials
    {
        public int Version { get; set; } = 1;
        public DateTime LastSaved { get; set; } = DateTime.UtcNow;
        public List<JsonEsiKey> EsiKeys { get; set; } = new List<JsonEsiKey>();
    }

    /// <summary>
    /// ESI key data for credentials.json.
    /// </summary>
    public class JsonEsiKey
    {
        public long CharacterId { get; set; }
        public string? RefreshToken { get; set; }

        /// <summary>
        /// Legacy bitflag access mask. Kept for backward-compatible deserialization.
        /// </summary>
        [Obsolete("Use AuthorizedScopes instead.")]
        public ulong AccessMask { get; set; }

        public bool Monitored { get; set; }

        /// <summary>
        /// ESI scope strings that were granted when this key was authenticated.
        /// </summary>
        public List<string> AuthorizedScopes { get; set; } = new();
    }

    /// <summary>
    /// Character index structure - lightweight list of all characters.
    /// </summary>
    public class JsonCharacterIndex
    {
        public int Version { get; set; } = 1;
        public DateTime LastSaved { get; set; } = DateTime.UtcNow;
        public List<JsonCharacterIndexEntry> Characters { get; set; } = new List<JsonCharacterIndexEntry>();
        public List<long> MonitoredCharacterIds { get; set; } = new List<long>();
    }

    /// <summary>
    /// Lightweight character entry for the index.
    /// </summary>
    public class JsonCharacterIndexEntry
    {
        public long CharacterId { get; set; }
        public string? Name { get; set; }
        public string? CorporationName { get; set; }
        public string? AllianceName { get; set; }
        public bool IsUriCharacter { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// Full character data structure for {characterId}.json.
    /// </summary>
    public class JsonCharacterData
    {
        public int Version { get; set; } = 1;
        public long CharacterId { get; set; }
        public DateTime LastSaved { get; set; } = DateTime.UtcNow;

        // Character identity
        public string? Name { get; set; }
        public DateTime Birthday { get; set; }
        public string? Race { get; set; }
        public string? Bloodline { get; set; }
        public string? Ancestry { get; set; }
        public string? Gender { get; set; }

        // Corporation/Alliance
        public long CorporationId { get; set; }
        public string? CorporationName { get; set; }
        public long AllianceId { get; set; }
        public string? AllianceName { get; set; }
        public long FactionId { get; set; }
        public string? FactionName { get; set; }

        // Attributes
        public int Intelligence { get; set; }
        public int Memory { get; set; }
        public int Charisma { get; set; }
        public int Perception { get; set; }
        public int Willpower { get; set; }

        // Financial
        public decimal Balance { get; set; }
        public long HomeStationId { get; set; }

        // UriCharacter source address (file path or URL for imported characters)
        public string? UriAddress { get; set; }

        // Character status and settings
        public string? CloneState { get; set; } = "Auto";  // Auto, Alpha, Omega
        public string? Label { get; set; }  // Custom character label
        public string? ShipName { get; set; }
        public string? ShipTypeName { get; set; }
        public double SecurityStatus { get; set; }
        public string? LastKnownLocation { get; set; }

        // Remaps and jump clones
        public int FreeRespecs { get; set; }
        public DateTime CloneJumpDate { get; set; }
        public DateTime LastRespecDate { get; set; }
        public DateTime LastTimedRespec { get; set; }
        public DateTime RemoteStationDate { get; set; }
        public DateTime JumpActivationDate { get; set; }
        public DateTime JumpFatigueDate { get; set; }
        public DateTime JumpLastUpdateDate { get; set; }

        // Skills and training
        public List<JsonSkill> Skills { get; set; } = new List<JsonSkill>();
        public List<JsonSkillQueueEntry> SkillQueue { get; set; } = new List<JsonSkillQueueEntry>();
        public int FreeSkillPoints { get; set; }

        // Implants
        public List<JsonImplantSet> ImplantSets { get; set; } = new List<JsonImplantSet>();

        // Plans
        public List<JsonPlan> Plans { get; set; } = new List<JsonPlan>();

        // Employment history
        public List<JsonEmploymentRecord> EmploymentHistory { get; set; } = new List<JsonEmploymentRecord>();

        // Character UI settings (per-character preferences)
        public CharacterUISettings? UISettings { get; set; }

        // Cached API data
        public List<JsonMarketOrder> MarketOrders { get; set; } = new List<JsonMarketOrder>();
        public List<JsonContract> Contracts { get; set; } = new List<JsonContract>();
        public List<JsonIndustryJob> IndustryJobs { get; set; } = new List<JsonIndustryJob>();
        public List<JsonAsset> Assets { get; set; } = new List<JsonAsset>();
        public List<JsonWalletJournalEntry> WalletJournal { get; set; } = new List<JsonWalletJournalEntry>();
        public List<JsonWalletTransaction> WalletTransactions { get; set; } = new List<JsonWalletTransaction>();

        // Last update times for API data
        public Dictionary<string, DateTime> LastApiUpdates { get; set; } = new Dictionary<string, DateTime>();
    }

    // Placeholder classes for nested data - will be implemented fully later
    public class JsonSkill
    {
        public int TypeId { get; set; }
        public string? Name { get; set; }
        public int Level { get; set; }
        public int ActiveLevel { get; set; }  // Active level for Alpha/Omega display
        public long Skillpoints { get; set; }
        public bool IsKnown { get; set; }
        public bool OwnsBook { get; set; }
    }

    public class JsonSkillQueueEntry
    {
        public int TypeId { get; set; }
        public int Level { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int StartSP { get; set; }
        public int EndSP { get; set; }
    }

    public class JsonImplantSet
    {
        public string? Name { get; set; }
        /// <summary>
        /// Distinguishes jump clones from user-created custom sets.
        /// "active" = active clone, "jump" = jump clone, "custom" = user-created.
        /// </summary>
        public string Type { get; set; } = "custom";
        public List<JsonImplant> Implants { get; set; } = new List<JsonImplant>();
    }

    public class JsonImplant
    {
        public int Slot { get; set; }
        public int TypeId { get; set; }
        public int Bonus { get; set; }
        public string? Name { get; set; }
    }

    public class JsonPlan
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public List<JsonPlanEntry> Entries { get; set; } = new List<JsonPlanEntry>();
        public List<JsonInvalidPlanEntry> InvalidEntries { get; set; } = new List<JsonInvalidPlanEntry>();

        // Sorting settings
        public string SortCriteria { get; set; } = "None";
        public string SortOrder { get; set; } = "None";
        public bool GroupByPriority { get; set; }
    }

    public class JsonPlanEntry
    {
        public int SkillId { get; set; }
        public string? SkillName { get; set; }  // Human-readable name
        public int Level { get; set; }
        public string? Type { get; set; }
        public int Priority { get; set; }
        public string? Notes { get; set; }
        public List<string> PlanGroups { get; set; } = new List<string>();  // Grouping within plan
        public JsonRemappingPoint? Remapping { get; set; }  // Attribute remapping point (optional)
    }

    public class JsonInvalidPlanEntry
    {
        public string? SkillName { get; set; }
        public long PlannedLevel { get; set; }
        public bool Acknowledged { get; set; }
    }

    public class JsonRemappingPoint
    {
        public string? Status { get; set; }  // RemappingPointStatus enum as string
        public long Perception { get; set; }
        public long Intelligence { get; set; }
        public long Memory { get; set; }
        public long Willpower { get; set; }
        public long Charisma { get; set; }
        public string? Description { get; set; }
    }

    public class JsonEmploymentRecord
    {
        public long CorporationId { get; set; }
        public string? CorporationName { get; set; }
        public DateTime StartDate { get; set; }
    }

    public class JsonMarketOrder { /* Will be fully implemented */ }
    public class JsonContract { /* Will be fully implemented */ }
    public class JsonIndustryJob { /* Will be fully implemented */ }
    public class JsonAsset { /* Will be fully implemented */ }
    public class JsonWalletJournalEntry { /* Will be fully implemented */ }
    public class JsonWalletTransaction { /* Will be fully implemented */ }

    #endregion
}
