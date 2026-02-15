using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.Eve
{
    public class SerializableKillLogVictim : SerializableCharacterListItem
    {
        [XmlAttribute("damageTaken")]
        public int DamageTaken { get; set; }
    }
}