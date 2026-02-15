using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.Eve
{
    public sealed class SerializableEveFactionalWarfareStatsListItem
    {
        [XmlAttribute("factionID")]
        public int FactionID { get; set; }

        [XmlAttribute("factionName")]
        public string? FactionName { get; set; }

        [XmlAttribute("pilots")]
        public int Pilots { get; set; }

        [XmlAttribute("systemsControlled")]
        public int SystemsControlled { get; set; }

        [XmlAttribute("killsYesterday")]
        public int KillsYesterday { get; set; }

        [XmlAttribute("killsLastWeek")]
        public int KillsLastWeek { get; set; }

        [XmlAttribute("killsTotal")]
        public int KillsTotal { get; set; }

        [XmlAttribute("victoryPointsYesterday")]
        public int VictoryPointsYesterday { get; set; }

        [XmlAttribute("victoryPointsLastWeek")]
        public int VictoryPointsLastWeek { get; set; }

        [XmlAttribute("victoryPointsTotal")]
        public int VictoryPointsTotal { get; set; }
    }
}