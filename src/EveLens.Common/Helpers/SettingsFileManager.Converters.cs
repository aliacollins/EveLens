// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Enumerations;
using EveLens.Common.Enumerations.UISettings;
using EveLens.Common.Serialization.Eve;
using EveLens.Common.Serialization.Settings;
using EveLens.Common.SettingsObjects;

namespace EveLens.Common.Helpers
{
    public static partial class SettingsFileManager
    {
        #region Converters (JSON → Serializable)

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

            // Common properties — use stored Guid if present, generate new one otherwise
            character.Guid = json.Guid != Guid.Empty ? json.Guid : Guid.NewGuid();
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
}
