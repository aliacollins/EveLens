// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Enumerations;
using EveLens.Common.Serialization.Eve;
using System.Runtime.Serialization;

namespace EveLens.Common.Serialization.Esi
{
    [DataContract]
    public sealed class EsiStandingsListItem
    {
        [DataMember(Name = "from_id")]
        public int ID { get; set; }

        // One of: agent, npc_corp, faction
        [DataMember(Name = "from_type")]
        private string GroupJson
        {
            get
            {
                // Convert from EveLens enumeration
                switch (Group)
                {
                case StandingGroup.NPCCorporations:
                    return "npc_corp";
                case StandingGroup.Agents:
                    return "agent";
                case StandingGroup.Factions:
                    return "faction";
                default:
                    return string.Empty;
                }
            }
            set
            {
                // Convert to EveLens enumeration
                switch (value)
                {
                case "npc_corp":
                    Group = StandingGroup.NPCCorporations;
                    break;
                case "agent":
                    Group = StandingGroup.Agents;
                    break;
                case "faction":
                    Group = StandingGroup.Factions;
                    break;
                default:
                    break;
                }
            }
        }

        [DataMember(Name = "standing")]
        public double StandingValue { get; set; }

        [IgnoreDataMember]
        public StandingGroup Group { get; set; }
    }
}
