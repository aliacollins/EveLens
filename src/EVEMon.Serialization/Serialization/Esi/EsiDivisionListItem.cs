// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EVEMon.Common.Serialization.Eve;
using System.Runtime.Serialization;

namespace EVEMon.Common.Serialization.Esi
{
    /// <summary>
    /// Used for both wallet and hangar divisions.
    /// </summary>
    [DataContract]
    public class EsiDivisionListItem
    {
        // 1 through 7 inclusive
        [DataMember(Name = "division")]
        public int Division { get; set; }
        
        [DataMember(Name = "name", EmitDefaultValue = false, IsRequired = false)]
        public string? Description { get; set; }
    }
}
