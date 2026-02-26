// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace EveLens.Common.Serialization.Esi
{
    /// <summary>
    /// Would inherit from SerializableMailMessagesListItem, but 'is_read' is changed to
    /// 'read'. CCPls, had to make an intermediate base instead
    /// </summary>
    [DataContract]
    public sealed class EsiAPIMailBody : EsiMailBase
    {
        [DataMember(Name = "body", EmitDefaultValue = false, IsRequired = false)]
        public string? Body { get; set; }

        [DataMember(Name = "read", IsRequired = false)]
        public bool Read { get; set; }
    }
}
