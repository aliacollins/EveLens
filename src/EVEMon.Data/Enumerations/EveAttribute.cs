using System.Xml.Serialization;

namespace EVEMon.Common.Enumerations
{
    /// <summary>
    /// Enumeration of the attributes in Eve. None is -1, other range from 0 to 4,
    /// matching the attributes order on the ingame character sheets.
    /// </summary>
    public enum EveAttribute
    {
        [XmlEnum("none")]
        None = -1,

        [XmlEnum("intelligence")]
        Intelligence = 0,

        [XmlEnum("perception")]
        Perception = 1,

        [XmlEnum("charisma")]
        Charisma = 2,

        [XmlEnum("willpower")]
        Willpower = 3,

        [XmlEnum("memory")]
        Memory = 4
    }
}