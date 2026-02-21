// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace EVEMon.Common.Serialization.FittingClf
{
    [DataContract]
    public sealed class SerializableClfFittingPreset
    {
        private Collection<SerializableClfFittingModule> m_modules = new();

        [DataMember(Name = "presetname")]
        public string? Name { get; set; }

        [DataMember(Name = "modules")]
        public Collection<SerializableClfFittingModule> Modules => m_modules ?? (m_modules = new Collection<SerializableClfFittingModule>());
    }
}