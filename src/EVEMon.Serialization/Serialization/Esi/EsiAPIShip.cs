// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Runtime.Serialization;

namespace EVEMon.Common.Serialization.Esi
{
    /// <summary>
    /// Represents a serializable version of a character's current ship.
    /// </summary>
    [DataContract]
    public sealed class EsiAPIShip
    {
        [DataMember(Name = "ship_type_id")]
        public int ShipTypeID { get; set; }

        // Unique to a particular ship until repackaged
        [DataMember(Name = "ship_item_id")]
        public long ShipItemID { get; set; }

        [DataMember(Name = "ship_name")]
        public string? ShipName { get; set; }
    }
}
