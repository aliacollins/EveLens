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
    public static partial class SettingsFileManager
    {
        #region Load from JSON to SerializableSettings

        /// <summary>
        /// Loads settings from JSON. Priority order:
        /// 1. Multi-file format (config.json + credentials.json + characters/) — primary
        /// 2. Legacy single-file (settings.json) — auto-migrates to multi-file, renames to .migrated
        /// 3. Legacy single-file backup (settings.json.bak) — same recovery
        /// </summary>
        /// <returns>SerializableSettings populated from JSON files, or null if loading fails.</returns>
        public static async Task<SerializableSettings?> LoadToSerializableSettingsAsync()
        {
            AppServices.TraceService?.Trace("begin - loading from JSON format");

            // Priority 1: Multi-file format (config.json or config.json.bak exists)
            if (File.Exists(ConfigFilePath) || File.Exists(ConfigFilePath + ".bak"))
            {
                var result = await LoadFromMultiFileFormatAsync();
                if (result != null)
                    return result;
            }

            // Priority 2: Legacy single-file format (settings.json) — auto-migrate
            if (File.Exists(SettingsJsonFilePath))
            {
                var settings = await TryLoadSettingsJsonAsync(SettingsJsonFilePath);
                if (settings != null)
                {
                    AppServices.TraceService?.Trace(
                        $"Loaded {settings.Characters.Count} characters from settings.json — auto-migrating to multi-file");
                    await AutoMigrateFromSettingsJson(settings);
                    return settings;
                }

                // Try backup of settings.json
                string backupPath = SettingsJsonFilePath + ".bak";
                if (File.Exists(backupPath))
                {
                    settings = await TryLoadSettingsJsonAsync(backupPath);
                    if (settings != null)
                    {
                        AppServices.TraceService?.Trace("Recovered from settings.json.bak — auto-migrating");
                        await AutoMigrateFromSettingsJson(settings);
                        return settings;
                    }
                }
            }

            // Priority 2b: settings.json.bak when primary doesn't exist
            string settingsBackupPath = SettingsJsonFilePath + ".bak";
            if (!File.Exists(SettingsJsonFilePath) && File.Exists(settingsBackupPath))
            {
                var settings = await TryLoadSettingsJsonAsync(settingsBackupPath);
                if (settings != null)
                {
                    AppServices.TraceService?.Trace("Recovered from settings.json.bak (primary missing) — auto-migrating");
                    await AutoMigrateFromSettingsJson(settings);
                    return settings;
                }
            }

            AppServices.TraceService?.Trace("No JSON settings found");
            return null;
        }

        /// <summary>
        /// Attempts to load SerializableSettings from a single settings.json file.
        /// </summary>
        private static async Task<SerializableSettings?> TryLoadSettingsJsonAsync(string path)
        {
            try
            {
                string json = await File.ReadAllTextAsync(path);
                return JsonSerializer.Deserialize<SerializableSettings>(json, DirectJsonOptions);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Auto-migrates from single settings.json to multi-file format.
        /// Writes multi-file, then renames settings.json → settings.json.migrated.
        /// </summary>
        private static async Task AutoMigrateFromSettingsJson(SerializableSettings settings)
        {
            try
            {
                await SaveMultiFileAsync(settings);

                // Rename old settings.json → settings.json.migrated
                string migratedPath = SettingsJsonFilePath + ".migrated";
                if (File.Exists(SettingsJsonFilePath))
                {
                    if (File.Exists(migratedPath))
                        File.Delete(migratedPath);
                    File.Move(SettingsJsonFilePath, migratedPath);
                    AppServices.TraceService?.Trace("Auto-migrated: settings.json → settings.json.migrated");
                }

                // Also clean up .bak from the old format
                string bakPath = SettingsJsonFilePath + ".bak";
                if (File.Exists(bakPath))
                {
                    try { File.Delete(bakPath); }
                    catch { /* best effort */ }
                }
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace($"Auto-migration from settings.json failed (non-critical): {ex.Message}");
            }
        }

        /// <summary>
        /// Loads settings from the multi-file JSON format (config.json, credentials.json, characters/).
        /// This is the primary format for EveLens.
        /// </summary>
        private static async Task<SerializableSettings?> LoadFromMultiFileFormatAsync()
        {
            AppServices.TraceService?.Trace("Loading from multi-file JSON format");

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
                    Revision = config.Revision > 0 ? config.Revision : Settings.Revision,
                    SSOClientID = config.SSOClientID ?? string.Empty,
                    SSOClientSecret = config.SSOClientSecret ?? string.Empty,
                    Compatibility = Enum.TryParse<CompatibilityMode>(config.Compatibility, out var compat)
                        ? compat : CompatibilityMode.Default,
                    EsiScopePreset = config.EsiScopePreset ?? "FullMonitoring",
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

                // Restore ESI custom scopes
                foreach (var scope in config.EsiCustomScopes ?? new List<string>())
                    settings.EsiCustomScopes.Add(scope);

                // Restore character groups
                foreach (var group in config.CharacterGroups ?? new List<JsonCharacterGroupSettings>())
                {
                    var cgs = new CharacterGroupSettings { Name = group.Name ?? string.Empty };
                    foreach (var guid in group.CharacterGuids)
                        cgs.CharacterGuids.Add(guid);
                    settings.CharacterGroups.Add(cgs);
                }

                // Restore global plan templates
                foreach (var jt in config.GlobalPlanTemplates ?? new List<JsonGlobalPlanTemplate>())
                {
                    settings.GlobalPlanTemplates.Add(new GlobalPlanTemplate
                    {
                        Id = jt.Id,
                        Name = jt.Name,
                        Description = jt.Description,
                        CreatedDate = jt.CreatedDate,
                        SubscribedCharacterGuids = new List<Guid>(jt.SubscribedCharacterGuids),
                        Entries = jt.Entries.Select(e => new GlobalPlanTemplateEntry
                        {
                            SkillID = e.SkillID,
                            SkillName = e.SkillName,
                            Level = e.Level
                        }).ToList()
                    });
                }

                // Convert ESI keys
                foreach (var esiKey in credentials?.EsiKeys ?? new List<JsonEsiKey>())
                {
                    var serialKey = new SerializableESIKey
                    {
                        ID = esiKey.CharacterId,
                        RefreshToken = esiKey.RefreshToken ?? string.Empty,
                        Monitored = esiKey.Monitored,
                    };

#pragma warning disable CS0618 // AccessMask is obsolete — preserve for backward compat
                    serialKey.AccessMask = esiKey.AccessMask;
#pragma warning restore CS0618

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

                AppServices.TraceService?.Trace($"done - loaded {settings.Characters.Count} characters from multi-file JSON format");
                return settings;
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace($"Error loading from multi-file JSON: {ex.Message}");
                return null;
            }
        }

        #endregion
    }
}
