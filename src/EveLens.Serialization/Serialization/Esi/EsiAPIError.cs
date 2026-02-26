// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Serialization.Eve;
using System.Runtime.Serialization;

namespace EveLens.Common.Serialization.Esi
{
    /// <summary>
    /// Matches the ESI server response when an error occurs.
    /// </summary>
    [DataContract]
    public class EsiAPIError
    {
        [DataMember(Name = "error", IsRequired = false)]
        public string? Error { get; set; }
    }
}
