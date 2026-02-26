// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace EveLens.Common.Serialization.Esi
{
    [DataContract]
    public class EsiAPISkills
    {
        public EsiAPISkills()
        {
            UnallocatedSP = 0;
        }

        [DataMember(Name = "total_sp")]
        public int TotalSP { get; set; }

        [DataMember(Name = "unallocated_sp")]
        public int UnallocatedSP { get; set; }

        [DataMember(Name = "skills")]
        public List<EsiSkillListItem> Skills { get; set; } = new();
    }
}
