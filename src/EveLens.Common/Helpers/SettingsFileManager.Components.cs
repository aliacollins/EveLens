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
        #region Character Component Storage

        /// <summary>
        /// Saves a character's data as component files (identity.json, skills.json, etc.).
        /// Synchronous variant for Settings.Save() and shutdown paths.
        /// </summary>
        public static void SaveCharacterComponentsSync(JsonCharacterData data)
        {
            if (data == null)
                return;

            EnsureCharacterDirectoryExists(data.CharacterId);

            // 1. identity.json — always written
            var identity = ProjectToIdentityFile(data);
            string identityJson = JsonSerializer.Serialize(identity, s_jsonOptions);
            WriteFileSyncAtomic(GetCharacterComponentPath(data.CharacterId, IdentityFileName), identityJson);

            // 2. skills.json — written if skills or queue non-empty
            if (data.Skills.Count > 0 || data.SkillQueue.Count > 0)
            {
                var skills = new JsonCharacterSkillsFile { Skills = data.Skills, SkillQueue = data.SkillQueue };
                string skillsJson = JsonSerializer.Serialize(skills, s_jsonOptions);
                WriteFileSyncAtomic(GetCharacterComponentPath(data.CharacterId, SkillsFileName), skillsJson);
            }
            else
            {
                TryDeleteComponentFile(data.CharacterId, SkillsFileName);
            }

            // 3. plans.json — written if plans exist
            if (data.Plans.Count > 0)
            {
                var plans = new JsonCharacterPlansFile { Plans = data.Plans };
                string plansJson = JsonSerializer.Serialize(plans, s_jsonOptions);
                WriteFileSyncAtomic(GetCharacterComponentPath(data.CharacterId, PlansFileName), plansJson);
            }
            else
            {
                TryDeleteComponentFile(data.CharacterId, PlansFileName);
            }

            // 4. implants.json — written if implant sets exist
            if (data.ImplantSets.Count > 0)
            {
                var implants = new JsonCharacterImplantsFile { ImplantSets = data.ImplantSets };
                string implantsJson = JsonSerializer.Serialize(implants, s_jsonOptions);
                WriteFileSyncAtomic(GetCharacterComponentPath(data.CharacterId, ImplantsFileName), implantsJson);
            }
            else
            {
                TryDeleteComponentFile(data.CharacterId, ImplantsFileName);
            }

            // 5. wallet.json — written if any wallet data exists
            bool hasWalletData = data.Balance != 0m ||
                data.MarketOrders.Count > 0 || data.Contracts.Count > 0 ||
                data.IndustryJobs.Count > 0 || data.WalletJournal.Count > 0 ||
                data.WalletTransactions.Count > 0;
            if (hasWalletData)
            {
                var wallet = new JsonCharacterWalletFile
                {
                    Balance = data.Balance,
                    MarketOrders = data.MarketOrders,
                    Contracts = data.Contracts,
                    IndustryJobs = data.IndustryJobs,
                    WalletJournal = data.WalletJournal,
                    WalletTransactions = data.WalletTransactions
                };
                string walletJson = JsonSerializer.Serialize(wallet, s_jsonOptions);
                WriteFileSyncAtomic(GetCharacterComponentPath(data.CharacterId, WalletFileName), walletJson);
            }
            else
            {
                TryDeleteComponentFile(data.CharacterId, WalletFileName);
            }

            // 6. assets.json — written if assets exist
            if (data.Assets.Count > 0)
            {
                var assets = new JsonCharacterAssetsFile { Assets = data.Assets };
                string assetsJson = JsonSerializer.Serialize(assets, s_jsonOptions);
                WriteFileSyncAtomic(GetCharacterComponentPath(data.CharacterId, AssetsFileName), assetsJson);
            }
            else
            {
                TryDeleteComponentFile(data.CharacterId, AssetsFileName);
            }

            // 7. settings.json — written if UISettings non-null
            if (data.UISettings != null)
            {
                var settings = new JsonCharacterSettingsFile { UISettings = data.UISettings };
                string settingsJson = JsonSerializer.Serialize(settings, s_jsonOptions);
                WriteFileSyncAtomic(GetCharacterComponentPath(data.CharacterId, CharacterSettingsFileName), settingsJson);
            }
            else
            {
                TryDeleteComponentFile(data.CharacterId, CharacterSettingsFileName);
            }
        }

        /// <summary>
        /// Saves a character's data as component files (identity.json, skills.json, etc.).
        /// Async variant for migration and import paths.
        /// </summary>
        public static async Task SaveCharacterComponentsAsync(JsonCharacterData data)
        {
            if (data == null)
                return;

            EnsureCharacterDirectoryExists(data.CharacterId);

            // 1. identity.json — always written
            var identity = ProjectToIdentityFile(data);
            string identityJson = JsonSerializer.Serialize(identity, s_jsonOptions);
            await WriteFileAtomicAsync(GetCharacterComponentPath(data.CharacterId, IdentityFileName), identityJson);

            // 2. skills.json
            if (data.Skills.Count > 0 || data.SkillQueue.Count > 0)
            {
                var skills = new JsonCharacterSkillsFile { Skills = data.Skills, SkillQueue = data.SkillQueue };
                string skillsJson = JsonSerializer.Serialize(skills, s_jsonOptions);
                await WriteFileAtomicAsync(GetCharacterComponentPath(data.CharacterId, SkillsFileName), skillsJson);
            }
            else
            {
                TryDeleteComponentFile(data.CharacterId, SkillsFileName);
            }

            // 3. plans.json
            if (data.Plans.Count > 0)
            {
                var plans = new JsonCharacterPlansFile { Plans = data.Plans };
                string plansJson = JsonSerializer.Serialize(plans, s_jsonOptions);
                await WriteFileAtomicAsync(GetCharacterComponentPath(data.CharacterId, PlansFileName), plansJson);
            }
            else
            {
                TryDeleteComponentFile(data.CharacterId, PlansFileName);
            }

            // 4. implants.json
            if (data.ImplantSets.Count > 0)
            {
                var implants = new JsonCharacterImplantsFile { ImplantSets = data.ImplantSets };
                string implantsJson = JsonSerializer.Serialize(implants, s_jsonOptions);
                await WriteFileAtomicAsync(GetCharacterComponentPath(data.CharacterId, ImplantsFileName), implantsJson);
            }
            else
            {
                TryDeleteComponentFile(data.CharacterId, ImplantsFileName);
            }

            // 5. wallet.json
            bool hasWalletData = data.Balance != 0m ||
                data.MarketOrders.Count > 0 || data.Contracts.Count > 0 ||
                data.IndustryJobs.Count > 0 || data.WalletJournal.Count > 0 ||
                data.WalletTransactions.Count > 0;
            if (hasWalletData)
            {
                var wallet = new JsonCharacterWalletFile
                {
                    Balance = data.Balance,
                    MarketOrders = data.MarketOrders,
                    Contracts = data.Contracts,
                    IndustryJobs = data.IndustryJobs,
                    WalletJournal = data.WalletJournal,
                    WalletTransactions = data.WalletTransactions
                };
                string walletJson = JsonSerializer.Serialize(wallet, s_jsonOptions);
                await WriteFileAtomicAsync(GetCharacterComponentPath(data.CharacterId, WalletFileName), walletJson);
            }
            else
            {
                TryDeleteComponentFile(data.CharacterId, WalletFileName);
            }

            // 6. assets.json
            if (data.Assets.Count > 0)
            {
                var assets = new JsonCharacterAssetsFile { Assets = data.Assets };
                string assetsJson = JsonSerializer.Serialize(assets, s_jsonOptions);
                await WriteFileAtomicAsync(GetCharacterComponentPath(data.CharacterId, AssetsFileName), assetsJson);
            }
            else
            {
                TryDeleteComponentFile(data.CharacterId, AssetsFileName);
            }

            // 7. settings.json
            if (data.UISettings != null)
            {
                var settings = new JsonCharacterSettingsFile { UISettings = data.UISettings };
                string settingsJson = JsonSerializer.Serialize(settings, s_jsonOptions);
                await WriteFileAtomicAsync(GetCharacterComponentPath(data.CharacterId, CharacterSettingsFileName), settingsJson);
            }
            else
            {
                TryDeleteComponentFile(data.CharacterId, CharacterSettingsFileName);
            }
        }

        /// <summary>
        /// Loads a character from component files in characters/{id}/ directory.
        /// Returns null if identity.json is missing (required component).
        /// </summary>
        public static async Task<JsonCharacterData?> LoadCharacterFromComponentsAsync(long characterId)
        {
            AppServices.TraceService?.Trace($"begin - loading components for character {characterId}");

            // identity.json is required
            var identity = await TryLoadComponentAsync<JsonCharacterIdentityFile>(characterId, IdentityFileName);
            if (identity == null)
            {
                AppServices.TraceService?.Trace($"No identity.json found for character {characterId}");
                return null;
            }

            // Load optional components
            var skills = await TryLoadComponentAsync<JsonCharacterSkillsFile>(characterId, SkillsFileName);
            var plans = await TryLoadComponentAsync<JsonCharacterPlansFile>(characterId, PlansFileName);
            var implants = await TryLoadComponentAsync<JsonCharacterImplantsFile>(characterId, ImplantsFileName);
            var wallet = await TryLoadComponentAsync<JsonCharacterWalletFile>(characterId, WalletFileName);
            var assets = await TryLoadComponentAsync<JsonCharacterAssetsFile>(characterId, AssetsFileName);
            var charSettings = await TryLoadComponentAsync<JsonCharacterSettingsFile>(characterId, CharacterSettingsFileName);

            // Reassemble into single JsonCharacterData
            var data = new JsonCharacterData
            {
                Version = identity.Version,
                CharacterId = identity.CharacterId,
                Guid = identity.Guid,
                LastSaved = identity.LastSaved,
                Name = identity.Name,
                Birthday = identity.Birthday,
                Race = identity.Race,
                Bloodline = identity.Bloodline,
                Ancestry = identity.Ancestry,
                Gender = identity.Gender,
                CorporationId = identity.CorporationId,
                CorporationName = identity.CorporationName,
                AllianceId = identity.AllianceId,
                AllianceName = identity.AllianceName,
                FactionId = identity.FactionId,
                FactionName = identity.FactionName,
                Intelligence = identity.Intelligence,
                Memory = identity.Memory,
                Charisma = identity.Charisma,
                Perception = identity.Perception,
                Willpower = identity.Willpower,
                HomeStationId = identity.HomeStationId,
                UriAddress = identity.UriAddress,
                CloneState = identity.CloneState,
                Label = identity.Label,
                ShipName = identity.ShipName,
                ShipTypeName = identity.ShipTypeName,
                SecurityStatus = identity.SecurityStatus,
                LastKnownLocation = identity.LastKnownLocation,
                FreeRespecs = identity.FreeRespecs,
                CloneJumpDate = identity.CloneJumpDate,
                LastRespecDate = identity.LastRespecDate,
                LastTimedRespec = identity.LastTimedRespec,
                RemoteStationDate = identity.RemoteStationDate,
                JumpActivationDate = identity.JumpActivationDate,
                JumpFatigueDate = identity.JumpFatigueDate,
                JumpLastUpdateDate = identity.JumpLastUpdateDate,
                FreeSkillPoints = identity.FreeSkillPoints,
                EmploymentHistory = identity.EmploymentHistory,
                LastApiUpdates = identity.LastApiUpdates
            };

            // Skills
            if (skills != null)
            {
                data.Skills = skills.Skills;
                data.SkillQueue = skills.SkillQueue;
            }

            // Plans
            if (plans != null)
                data.Plans = plans.Plans;

            // Implants
            if (implants != null)
                data.ImplantSets = implants.ImplantSets;

            // Wallet
            if (wallet != null)
            {
                data.Balance = wallet.Balance;
                data.MarketOrders = wallet.MarketOrders;
                data.Contracts = wallet.Contracts;
                data.IndustryJobs = wallet.IndustryJobs;
                data.WalletJournal = wallet.WalletJournal;
                data.WalletTransactions = wallet.WalletTransactions;
            }

            // Assets
            if (assets != null)
                data.Assets = assets.Assets;

            // UI Settings
            if (charSettings != null)
                data.UISettings = charSettings.UISettings;

            AppServices.TraceService?.Trace($"done - loaded character {characterId} from components");
            return data;
        }

        /// <summary>
        /// Attempts to load a component file with .bak fallback.
        /// </summary>
        private static async Task<T?> TryLoadComponentAsync<T>(long characterId, string componentFileName) where T : class
        {
            string path = GetCharacterComponentPath(characterId, componentFileName);

            // Try primary
            var result = await TryLoadJsonAsync<T>(path);
            if (result != null)
                return result;

            // Try .bak fallback
            result = await TryLoadJsonAsync<T>(path + ".bak");
            if (result != null)
            {
                AppServices.TraceService?.Trace($"Recovered {componentFileName} for character {characterId} from backup");
                // Best-effort restore primary
                try
                {
                    string json = JsonSerializer.Serialize(result, s_jsonOptions);
                    await WriteFileAtomicAsync(path, json);
                }
                catch { }
            }

            return result;
        }

        /// <summary>
        /// Best-effort delete of a component file and its .bak backup.
        /// </summary>
        private static void TryDeleteComponentFile(long characterId, string componentFileName)
        {
            string path = GetCharacterComponentPath(characterId, componentFileName);
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
                string bakPath = path + ".bak";
                if (File.Exists(bakPath))
                    File.Delete(bakPath);
            }
            catch { /* best effort */ }
        }

        /// <summary>
        /// Projects a JsonCharacterData into a JsonCharacterIdentityFile for component storage.
        /// </summary>
        private static JsonCharacterIdentityFile ProjectToIdentityFile(JsonCharacterData data)
        {
            return new JsonCharacterIdentityFile
            {
                Version = data.Version,
                CharacterId = data.CharacterId,
                Guid = data.Guid,
                LastSaved = DateTime.UtcNow,
                Name = data.Name,
                Birthday = data.Birthday,
                Race = data.Race,
                Bloodline = data.Bloodline,
                Ancestry = data.Ancestry,
                Gender = data.Gender,
                CorporationId = data.CorporationId,
                CorporationName = data.CorporationName,
                AllianceId = data.AllianceId,
                AllianceName = data.AllianceName,
                FactionId = data.FactionId,
                FactionName = data.FactionName,
                Intelligence = data.Intelligence,
                Memory = data.Memory,
                Charisma = data.Charisma,
                Perception = data.Perception,
                Willpower = data.Willpower,
                HomeStationId = data.HomeStationId,
                UriAddress = data.UriAddress,
                CloneState = data.CloneState,
                Label = data.Label,
                ShipName = data.ShipName,
                ShipTypeName = data.ShipTypeName,
                SecurityStatus = data.SecurityStatus,
                LastKnownLocation = data.LastKnownLocation,
                FreeRespecs = data.FreeRespecs,
                CloneJumpDate = data.CloneJumpDate,
                LastRespecDate = data.LastRespecDate,
                LastTimedRespec = data.LastTimedRespec,
                RemoteStationDate = data.RemoteStationDate,
                JumpActivationDate = data.JumpActivationDate,
                JumpFatigueDate = data.JumpFatigueDate,
                JumpLastUpdateDate = data.JumpLastUpdateDate,
                FreeSkillPoints = data.FreeSkillPoints,
                EmploymentHistory = data.EmploymentHistory,
                LastApiUpdates = data.LastApiUpdates
            };
        }

        #endregion
    }
}
