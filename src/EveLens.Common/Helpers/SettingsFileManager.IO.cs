// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using EveLens.Common.Services;
using EveLens.Common.SettingsObjects;

namespace EveLens.Common.Helpers
{
    public static partial class SettingsFileManager
    {
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
        /// Priority: 1) Component folder (characters/{id}/), 2) Legacy flat file (characters/{id}.json) with auto-migration.
        /// </summary>
        public static async Task<JsonCharacterData?> LoadCharacterAsync(long characterId)
        {
            AppServices.TraceService?.Trace($"begin - character {characterId}");

            // Priority 1: Component folder (characters/{id}/)
            string charDir = GetCharacterDirectory(characterId);
            if (Directory.Exists(charDir))
            {
                var result = await LoadCharacterFromComponentsAsync(characterId);
                if (result != null)
                    return result;
            }

            // Priority 2: Legacy flat file (characters/{id}.json) — auto-migrate to component folder
            string legacyPath = GetLegacyCharacterFilePath(characterId);
            var character = await TryLoadJsonAsync<JsonCharacterData>(legacyPath);
            if (character == null)
                character = await TryLoadJsonAsync<JsonCharacterData>(legacyPath + ".bak");

            if (character != null)
            {
                AppServices.TraceService?.Trace($"Auto-migrating character {characterId} from flat file to component folder");
                try
                {
                    await SaveCharacterComponentsAsync(character);

                    // Clean up legacy flat file
                    if (File.Exists(legacyPath))
                        File.Delete(legacyPath);
                    string bakPath = legacyPath + ".bak";
                    if (File.Exists(bakPath))
                        File.Delete(bakPath);
                }
                catch (Exception ex)
                {
                    AppServices.TraceService?.Trace($"Auto-migration failed (non-critical): {ex.Message}");
                }
                return character;
            }

            AppServices.TraceService?.Trace($"Character not found: {characterId}");
            return null;
        }

        /// <summary>
        /// Saves a specific character's data as component files.
        /// </summary>
        public static async Task SaveCharacterAsync(JsonCharacterData character)
        {
            if (character == null)
                return;

            AppServices.TraceService?.Trace($"begin - character {character.CharacterId}");

            try
            {
                await SaveCharacterComponentsAsync(character);
                AppServices.TraceService?.Trace($"done - saved character {character.CharacterId} (component files)");
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace($"Error saving character {character.CharacterId}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Deletes a character's data (component folder and/or legacy flat file).
        /// </summary>
        public static void DeleteCharacter(long characterId)
        {
            AppServices.TraceService?.Trace($"begin - character {characterId}");

            // Delete component folder
            string charDir = GetCharacterDirectory(characterId);
            if (Directory.Exists(charDir))
            {
                Directory.Delete(charDir, recursive: true);
                AppServices.TraceService?.Trace($"Deleted character directory: {characterId}");
            }

            // Clean up legacy flat file
            string legacyPath = GetLegacyCharacterFilePath(characterId);
            if (File.Exists(legacyPath))
            {
                File.Delete(legacyPath);
                AppServices.TraceService?.Trace($"Deleted legacy character file: {characterId}");
            }
            string bakPath = legacyPath + ".bak";
            if (File.Exists(bakPath))
                File.Delete(bakPath);
        }

        /// <summary>
        /// Gets all character IDs that have data (component folders and/or legacy flat files).
        /// </summary>
        public static IEnumerable<long> GetSavedCharacterIds()
        {
            if (!Directory.Exists(CharactersDirectory))
                yield break;

            var seen = new HashSet<long>();

            // Component folders (characters/{id}/)
            foreach (string dir in Directory.GetDirectories(CharactersDirectory))
            {
                string dirName = Path.GetFileName(dir);
                if (long.TryParse(dirName, out long characterId) && seen.Add(characterId))
                    yield return characterId;
            }

            // Legacy flat files (characters/{id}.json)
            foreach (string file in Directory.GetFiles(CharactersDirectory, "*.json"))
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                if (fileName == "index")
                    continue;

                if (long.TryParse(fileName, out long characterId) && seen.Add(characterId))
                    yield return characterId;
            }
        }

        #endregion
    }
}
