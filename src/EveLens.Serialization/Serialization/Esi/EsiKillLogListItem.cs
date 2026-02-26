// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Runtime.Serialization;

namespace EveLens.Common.Serialization.Esi
{
    [DataContract]
    public sealed class EsiKillLogListItem
    {
        [DataMember(Name = "killmail_id")]
        public int KillID { get; set; }

        [DataMember(Name = "killmail_hash")]
        public string? Hash { get; set; }
    }
}
