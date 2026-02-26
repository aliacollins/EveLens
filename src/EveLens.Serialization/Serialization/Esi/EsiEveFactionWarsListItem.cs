// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Serialization.Eve;
using System.Runtime.Serialization;

namespace EveLens.Common.Serialization.Esi
{
    [DataContract]
    public sealed class EsiEveFactionWarsListItem
    {
        [DataMember(Name = "faction_id")]
        public int FactionID { get; set; }
        
        [DataMember(Name = "against_id")]
        public int AgainstID { get; set; }

        public SerializableEveFactionWarsListItem ToXMLItem()
        {
            return new SerializableEveFactionWarsListItem()
            {
                FactionID = FactionID,
                AgainstID = AgainstID
            };
        }
    }
}
