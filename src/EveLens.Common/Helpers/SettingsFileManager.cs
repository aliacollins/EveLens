// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using EveLens.Common.Services;

namespace EveLens.Common.Helpers
{
    /// <summary>
    /// Manages the split settings file structure:
    /// - config.json: UI settings and preferences
    /// - credentials.json: ESI tokens (portable)
    /// - characters/{id}/: Per-character component files (identity.json, skills.json, etc.)
    /// - characters/index.json: Character index
    /// </summary>
    public static partial class SettingsFileManager
    {
        #region Constants

        private const string SettingsJsonFileName = "settings.json";
        private const string ConfigFileName = "config.json";
        private const string CredentialsFileName = "credentials.json";
        private const string CharactersFolderName = "characters";
        private const string CharacterIndexFileName = "index.json";
        private const string LegacySettingsFileName = "settings.xml";

        // Component file names for per-character folder storage
        private const string IdentityFileName = "identity.json";
        private const string SkillsFileName = "skills.json";
        private const string PlansFileName = "plans.json";
        private const string ImplantsFileName = "implants.json";
        private const string WalletFileName = "wallet.json";
        private const string AssetsFileName = "assets.json";
        private const string CharacterSettingsFileName = "settings.json";

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
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate,
            Converters = { new JsonStringEnumConverter() }
        };

        // Legacy options for old Json* class format (backward compat during load)
        private static readonly JsonSerializerOptions s_jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
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
        /// Gets the directory path for a specific character's component files.
        /// </summary>
        public static string GetCharacterDirectory(long characterId)
            => Path.Combine(CharactersDirectory, characterId.ToString());

        /// <summary>
        /// Gets the file path for a specific character component.
        /// </summary>
        public static string GetCharacterComponentPath(long characterId, string componentFileName)
            => Path.Combine(GetCharacterDirectory(characterId), componentFileName);

        /// <summary>
        /// Gets the legacy flat-file path for a specific character.
        /// Kept for migration detection and backward compatibility.
        /// </summary>
        public static string GetLegacyCharacterFilePath(long characterId)
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

        /// <summary>
        /// Ensures the character component directory exists (characters/{id}/).
        /// </summary>
        public static void EnsureCharacterDirectoryExists(long characterId)
        {
            EnsureDirectoriesExist();
            string dir = GetCharacterDirectory(characterId);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        #endregion

        #region Detection

        /// <summary>
        /// Checks if JSON settings exist (new direct format or legacy multi-file format).
        /// </summary>
        public static bool JsonSettingsExist()
            => File.Exists(SettingsJsonFilePath) || File.Exists(ConfigFilePath) || File.Exists(ConfigFilePath + ".bak");

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
                // Delete settings.json (legacy single-file format)
                if (File.Exists(SettingsJsonFilePath))
                    File.Delete(SettingsJsonFilePath);

                // Delete settings.json.migrated (auto-migration artifact)
                string migratedPath = SettingsJsonFilePath + ".migrated";
                if (File.Exists(migratedPath))
                    File.Delete(migratedPath);

                // Delete config.json (multi-file format)
                if (File.Exists(ConfigFilePath))
                    File.Delete(ConfigFilePath);

                // Delete credentials.json
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
    }
}
