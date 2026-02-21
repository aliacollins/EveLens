// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace EVEMon.Common.Serialization.FittingClf
{
    [DataContract]
    public sealed class SerializableClfFittingDroneSet
    {
        private Collection<SerializableClfFittingDroneType> m_inBayDrones = new();
        private Collection<SerializableClfFittingDroneType> m_inSpaceDrones = new();

        [DataMember(Name = "presetname")]
        public string? Name { get; set; }

        [DataMember(Name = "inbay")]
        public Collection<SerializableClfFittingDroneType> InBay => m_inBayDrones ?? (m_inBayDrones = new Collection<SerializableClfFittingDroneType>());

        [DataMember(Name = "inspace")]
        public Collection<SerializableClfFittingDroneType> InSpace => m_inSpaceDrones ?? (m_inSpaceDrones = new Collection<SerializableClfFittingDroneType>());
    }
}