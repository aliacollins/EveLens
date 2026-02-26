// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Runtime.Serialization;
using System.Collections.Generic;
using EveLens.Common.Serialization.Eve;

namespace EveLens.Common.Serialization.Esi
{
    [DataContract]
    public sealed class EsiJumpCloneListItem : EsiLocationBase
    {
        [DataMember(Name = "jump_clone_id")]
        public int JumpCloneID { get; set; }

        [DataMember(Name = "name", EmitDefaultValue = false, IsRequired = false)]
        public string? Name { get; set; }

        [DataMember(Name = "implants")]
        public List<int> Implants { get; set; } = new();
    }
}
