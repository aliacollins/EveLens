// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Runtime.Serialization;

namespace EVEMon.Common.Serialization.Esi
{
    [DataContract]
    public sealed class EsiCharacterNameListItem
    {
        [DataMember(Name = "id")]
        public long ID { get; set; }

        [DataMember(Name = "name")]
        public string? Name { get; set; }
    }
}
