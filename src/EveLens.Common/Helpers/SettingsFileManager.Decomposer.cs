// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Serialization.Eve;
using EveLens.Common.Serialization.Settings;
using EveLens.Common.SettingsObjects;

namespace EveLens.Common.Helpers
{
    public static partial class SettingsFileManager
    {
        #region Decompose Settings (Reverse Converters)

        /// <summary>
        /// Decomposes a SerializableSettings into the multi-file JSON format.
        /// This is the inverse of LoadFromLegacyJsonFormatAsync.
        /// </summary>
        public static (JsonConfig Config, JsonCredentials Credentials, JsonCharacterIndex Index, List<JsonCharacterData> Characters)
            DecomposeSettings(SerializableSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            // Build config
            var config = new JsonConfig
            {
                Version = 1,
                ForkId = settings.ForkId ?? "aliacollins",
                ForkVersion = settings.ForkVersion,
                LastSaved = DateTime.UtcNow,
                Revision = settings.Revision,
                Compatibility = settings.Compatibility.ToString(),
                EsiScopePreset = settings.EsiScopePreset,
                EsiCustomScopes = settings.EsiCustomScopes.ToList(),
                CharacterGroups = settings.CharacterGroups.Select(g => new JsonCharacterGroupSettings
                {
                    Name = g.Name,
                    CharacterGuids = g.CharacterGuids.ToList()
                }).ToList(),
                SSOClientID = settings.SSOClientID,
                SSOClientSecret = settings.SSOClientSecret,
                GlobalPlanTemplates = settings.GlobalPlanTemplates.Select(t => new JsonGlobalPlanTemplate
                {
                    Id = t.Id,
                    Name = t.Name,
                    Description = t.Description,
                    CreatedDate = t.CreatedDate,
                    SubscribedCharacterGuids = new List<Guid>(t.SubscribedCharacterGuids),
                    Entries = t.Entries.Select(e => new JsonGlobalPlanTemplateEntry
                    {
                        SkillID = e.SkillID,
                        SkillName = e.SkillName,
                        Level = e.Level
                    }).ToList()
                }).ToList(),
                UI = settings.UI,
                G15 = settings.G15,
                Proxy = settings.Proxy,
                Updates = settings.Updates,
                Calendar = settings.Calendar,
                Exportation = settings.Exportation,
                MarketPricer = settings.MarketPricer,
                Notifications = settings.Notifications,
                LoadoutsProvider = settings.LoadoutsProvider,
                PortableEveInstallations = settings.PortableEveInstallations,
                CloudStorageServiceProvider = settings.CloudStorageServiceProvider,
                Scheduler = settings.Scheduler
            };

            // Build credentials
            var credentials = new JsonCredentials
            {
                Version = 1,
                LastSaved = DateTime.UtcNow
            };

            foreach (var esiKey in settings.ESIKeys)
            {
                var jsonKey = new JsonEsiKey
                {
                    CharacterId = esiKey.ID,
                    RefreshToken = esiKey.RefreshToken,
                    Monitored = esiKey.Monitored,
                    AuthorizedScopes = esiKey.AuthorizedScopes != null
                        ? new List<string>(esiKey.AuthorizedScopes)
                        : new List<string>()
                };

#pragma warning disable CS0618 // AccessMask is obsolete — preserved for backward compat round-trip
                jsonKey.AccessMask = esiKey.AccessMask;
#pragma warning restore CS0618

                credentials.EsiKeys.Add(jsonKey);
            }

            // Build character index and per-character data
            var index = new JsonCharacterIndex
            {
                Version = 1,
                LastSaved = DateTime.UtcNow
            };

            // Build a lookup: character Guid → MonitoredCharacterSettings
            var monitoredLookup = new Dictionary<Guid, MonitoredCharacterSettings>();
            foreach (var mc in settings.MonitoredCharacters)
                monitoredLookup[mc.CharacterGuid] = mc;

            // Build a lookup: character Guid → list of plans
            var plansLookup = new Dictionary<Guid, List<SerializablePlan>>();
            foreach (var plan in settings.Plans)
            {
                if (!plansLookup.TryGetValue(plan.Owner, out var list))
                {
                    list = new List<SerializablePlan>();
                    plansLookup[plan.Owner] = list;
                }
                list.Add(plan);
            }

            var characterDataList = new List<JsonCharacterData>();

            foreach (var character in settings.Characters)
            {
                bool isUri = character is SerializableUriCharacter;
                var charData = DecomposeCharacter(character, isUri);

                // Attach plans owned by this character
                if (plansLookup.TryGetValue(character.Guid, out var plans))
                {
                    foreach (var plan in plans)
                        charData.Plans.Add(DecomposePlan(plan));
                }

                // Attach UISettings from monitored character settings
                if (monitoredLookup.TryGetValue(character.Guid, out var monitored))
                    charData.UISettings = monitored.Settings;

                characterDataList.Add(charData);

                // Add index entry
                index.Characters.Add(new JsonCharacterIndexEntry
                {
                    CharacterId = character.ID,
                    Name = character.Name,
                    CorporationName = character.CorporationName,
                    AllianceName = character.AllianceName,
                    IsUriCharacter = isUri,
                    LastUpdated = DateTime.UtcNow
                });
            }

            // Set monitored character IDs
            foreach (var mc in settings.MonitoredCharacters)
            {
                // Find the character ID for this Guid
                var matchingChar = settings.Characters.FirstOrDefault(c => c.Guid == mc.CharacterGuid);
                if (matchingChar != null)
                    index.MonitoredCharacterIds.Add(matchingChar.ID);
            }

            return (config, credentials, index, characterDataList);
        }

        /// <summary>
        /// Decomposes a SerializableSettingsCharacter to JsonCharacterData.
        /// Inverse of ConvertToSerializableCharacter.
        /// </summary>
        private static JsonCharacterData DecomposeCharacter(SerializableSettingsCharacter character, bool isUri)
        {
            var data = new JsonCharacterData
            {
                Version = 1,
                CharacterId = character.ID,
                Guid = character.Guid,
                LastSaved = DateTime.UtcNow,
                Name = character.Name,
                Birthday = character.Birthday,
                Race = character.Race,
                Bloodline = character.BloodLine,
                Ancestry = character.Ancestry,
                Gender = character.Gender,
                CorporationId = character.CorporationID,
                CorporationName = character.CorporationName,
                AllianceId = character.AllianceID,
                AllianceName = character.AllianceName,
                FactionId = character.FactionID,
                FactionName = character.FactionName,
                Balance = character.Balance,
                HomeStationId = character.HomeStationID,
                FreeSkillPoints = character.FreeSkillPoints,
                CloneState = character.CloneState ?? "Auto",
                Label = character.Label,
                ShipName = character.ShipName,
                ShipTypeName = character.ShipTypeName,
                SecurityStatus = character.SecurityStatus,
                FreeRespecs = character.FreeRespecs,
                CloneJumpDate = character.CloneJumpDate,
                LastRespecDate = character.LastRespecDate,
                LastTimedRespec = character.LastTimedRespec,
                RemoteStationDate = character.RemoteStationDate,
                JumpActivationDate = character.JumpActivationDate,
                JumpFatigueDate = character.JumpFatigueDate,
                JumpLastUpdateDate = character.JumpLastUpdateDate,
                Intelligence = (int)(character.Attributes?.Intelligence ?? 0),
                Memory = (int)(character.Attributes?.Memory ?? 0),
                Charisma = (int)(character.Attributes?.Charisma ?? 0),
                Perception = (int)(character.Attributes?.Perception ?? 0),
                Willpower = (int)(character.Attributes?.Willpower ?? 0)
            };

            // URI character source address
            if (isUri && character is SerializableUriCharacter uriChar)
                data.UriAddress = uriChar.Address;

            // Skills
            foreach (var skill in character.Skills)
            {
                data.Skills.Add(new JsonSkill
                {
                    TypeId = skill.ID,
                    Name = skill.Name,
                    Level = (int)skill.Level,
                    ActiveLevel = (int)skill.ActiveLevel,
                    Skillpoints = skill.Skillpoints,
                    IsKnown = skill.IsKnown,
                    OwnsBook = skill.OwnsBook
                });
            }

            // Employment history
            foreach (var record in character.EmploymentHistory)
            {
                data.EmploymentHistory.Add(new JsonEmploymentRecord
                {
                    CorporationId = record.CorporationID,
                    CorporationName = record.CorporationName,
                    StartDate = record.StartDate
                });
            }

            // Skill queue (CCP characters only)
            if (character is SerializableCCPCharacter ccpChar)
            {
                foreach (var entry in ccpChar.SkillQueue)
                {
                    data.SkillQueue.Add(new JsonSkillQueueEntry
                    {
                        TypeId = entry.ID,
                        Level = entry.Level,
                        StartTime = entry.StartTime,
                        EndTime = entry.EndTime,
                        StartSP = entry.StartSP,
                        EndSP = entry.EndSP
                    });
                }
            }

            // Implant sets
            if (character.ImplantSets != null)
            {
                if (character.ImplantSets.ActiveClone != null)
                    data.ImplantSets.Add(DecomposeImplantSet(character.ImplantSets.ActiveClone, "active"));

                foreach (var jumpClone in character.ImplantSets.JumpClones)
                    data.ImplantSets.Add(DecomposeImplantSet(jumpClone, "jump"));

                foreach (var customSet in character.ImplantSets.CustomSets)
                    data.ImplantSets.Add(DecomposeImplantSet(customSet, "custom"));
            }

            return data;
        }

        /// <summary>
        /// Decomposes a SerializableSettingsImplantSet to JsonImplantSet.
        /// Inverse of ConvertToSerializableImplantSet.
        /// </summary>
        private static JsonImplantSet DecomposeImplantSet(SerializableSettingsImplantSet set, string type)
        {
            var json = new JsonImplantSet
            {
                Name = set.Name,
                Type = type
            };

            void AddImplant(int slot, string value)
            {
                if (string.IsNullOrEmpty(value) || value == "None")
                    return;
                var implant = new JsonImplant { Slot = slot };
                if (int.TryParse(value, out int typeId))
                    implant.TypeId = typeId;
                else
                    implant.Name = value;
                json.Implants.Add(implant);
            }

            AddImplant(1, set.Intelligence);
            AddImplant(2, set.Memory);
            AddImplant(3, set.Willpower);
            AddImplant(4, set.Perception);
            AddImplant(5, set.Charisma);
            AddImplant(6, set.Slot6);
            AddImplant(7, set.Slot7);
            AddImplant(8, set.Slot8);
            AddImplant(9, set.Slot9);
            AddImplant(10, set.Slot10);

            return json;
        }

        /// <summary>
        /// Decomposes a SerializablePlan to JsonPlan.
        /// Inverse of ConvertToSerializablePlan.
        /// </summary>
        private static JsonPlan DecomposePlan(SerializablePlan plan)
        {
            var json = new JsonPlan
            {
                Name = plan.Name,
                Description = plan.Description,
                LastActivity = plan.LastActivity != DateTime.MinValue ? plan.LastActivity : null,
                SortCriteria = plan.SortingPreferences?.Criteria.ToString() ?? "None",
                SortOrder = plan.SortingPreferences?.Order.ToString() ?? "None",
                GroupByPriority = plan.SortingPreferences?.GroupByPriority ?? false
            };

            foreach (var entry in plan.Entries)
            {
                var jsonEntry = new JsonPlanEntry
                {
                    SkillId = entry.ID,
                    SkillName = entry.SkillName,
                    Level = (int)entry.Level,
                    Type = entry.Type.ToString(),
                    Priority = entry.Priority,
                    Notes = entry.Notes,
                    PlanGroups = entry.PlanGroups.ToList()
                };

                if (entry.Remapping != null)
                {
                    jsonEntry.Remapping = new JsonRemappingPoint
                    {
                        Status = entry.Remapping.Status.ToString(),
                        Perception = entry.Remapping.Perception,
                        Intelligence = entry.Remapping.Intelligence,
                        Memory = entry.Remapping.Memory,
                        Willpower = entry.Remapping.Willpower,
                        Charisma = entry.Remapping.Charisma,
                        Description = entry.Remapping.Description
                    };
                }

                json.Entries.Add(jsonEntry);
            }

            foreach (var invalid in plan.InvalidEntries)
            {
                json.InvalidEntries.Add(new JsonInvalidPlanEntry
                {
                    SkillName = invalid.SkillName,
                    PlannedLevel = invalid.PlannedLevel,
                    Acknowledged = invalid.Acknowledged
                });
            }

            return json;
        }

        #endregion
    }
}
