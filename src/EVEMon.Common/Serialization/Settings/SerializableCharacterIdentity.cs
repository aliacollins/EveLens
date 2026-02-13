using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.Settings
{
    /// <summary>
    /// Represents a character identity in our settings file
    /// </summary>
    public sealed class SerializableCharacterIdentity
    {
        [XmlAttribute("id")]
        public long ID { get; set; }

        [XmlAttribute("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Creates a copy of this instance.
        /// </summary>
        /// <returns>A new instance with the same values.</returns>
        internal SerializableCharacterIdentity Clone()
        {
            return new SerializableCharacterIdentity { ID = ID, Name = Name };
        }
    }
}