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
    }
}
