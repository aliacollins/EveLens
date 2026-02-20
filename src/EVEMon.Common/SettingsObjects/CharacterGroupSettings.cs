using System;
using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace EVEMon.Common.SettingsObjects
{
    /// <summary>
    /// Stores a named group of characters for organizing the Overview.
    /// </summary>
    public sealed class CharacterGroupSettings
    {
        private readonly Collection<Guid> m_characterGuids;

        public CharacterGroupSettings()
        {
            Name = string.Empty;
            m_characterGuids = new Collection<Guid>();
        }

        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlArray("members")]
        [XmlArrayItem("guid")]
        public Collection<Guid> CharacterGuids => m_characterGuids;
    }
}
