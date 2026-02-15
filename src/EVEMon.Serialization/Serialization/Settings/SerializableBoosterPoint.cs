using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.Settings
{
    /// <summary>
    /// Represents a booster injection point in a skill plan.
    /// </summary>
    public sealed class SerializableBoosterPoint
    {
        [XmlAttribute("bonus")]
        public int Bonus { get; set; }

        [XmlAttribute("durationHours")]
        public int DurationHours { get; set; }
    }
}
