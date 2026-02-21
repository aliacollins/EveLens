// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Runtime.Serialization;

namespace EVEMon.Common.Serialization.Esi
{
    [DataContract]
    public sealed class EsiPlanetaryContentsListItem
    {
        [DataMember(Name = "type_id")]
        public int TypeID { get; set; }

        [DataMember(Name = "amount")]
        public int Amount { get; set; }
    }
}
