// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EVEMon.Common.Serialization.Eve;
using System.Runtime.Serialization;

namespace EVEMon.Common.Serialization.Esi
{
    [DataContract]
    public sealed class EsiCalendarEventAttendeeListItem
    {
        [DataMember(Name = "character_id", EmitDefaultValue = false, IsRequired = false)]
        public long CharacterID { get; set; }
        
        // One of: declined, not_responded, accepted, tentative
        [DataMember(Name = "event_response", EmitDefaultValue = false, IsRequired = false)]
        public string? Response { get; set; }
    }
}
