// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace EveLens.Common.Serialization.Settings
{
    /// <summary>
    /// Represents a collection of implants sets.
    /// </summary>
    public sealed class SerializableImplantSetCollection
    {
        private readonly Collection<SerializableSettingsImplantSet> m_customSets;

        public SerializableImplantSetCollection()
        {
            ActiveClone = new SerializableSettingsImplantSet();
            JumpClones = new Collection<SerializableSettingsImplantSet>();
            m_customSets = new Collection<SerializableSettingsImplantSet>();
        }

        [XmlElement("activeCloneSet")]
        public SerializableSettingsImplantSet ActiveClone { get; set; }

        [XmlElement("jumpCloneSet")]
        public Collection<SerializableSettingsImplantSet> JumpClones { get; set; }

        [XmlElement("customSet")]
        public Collection<SerializableSettingsImplantSet> CustomSets => m_customSets;

        [XmlElement("selectedIndex")]
        public int SelectedIndex { get; set; }
    }
}