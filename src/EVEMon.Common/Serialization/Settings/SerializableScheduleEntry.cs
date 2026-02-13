using System;
using System.Xml.Serialization;
using EVEMon.Common.Scheduling;

namespace EVEMon.Common.Serialization.Settings
{
    public class SerializableScheduleEntry
    {
        [XmlAttribute("startDate")]
        public DateTime StartDate { get; set; }

        [XmlAttribute("endDate")]
        public DateTime EndDate { get; set; }

        [XmlElement("title")]
        public string? Title { get; set; }

        [XmlElement("options")]
        public ScheduleEntryOptions Options { get; set; }
    }
}