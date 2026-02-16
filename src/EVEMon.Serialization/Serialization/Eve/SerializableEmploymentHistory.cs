using System;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using EVEMon.Common.Extensions;

namespace EVEMon.Common.Serialization.Eve
{
    public sealed class SerializableEmploymentHistory
    {
        [XmlAttribute("corporationID")]
        public long CorporationID { get; set; }

        [XmlAttribute("corporationName")]
        [JsonIgnore]
        public string? CorporationNameXml
        {
            get { return CorporationName; }
            set { CorporationName = value?.HtmlDecode() ?? string.Empty; }
        }

        [XmlAttribute("startDate")]
        [JsonIgnore]
        public string StartDateXml
        {
            get { return StartDate.DateTimeToTimeString(); }
            set
            {
                if (!string.IsNullOrEmpty(value))
                    StartDate = value.TimeStringToDateTime();
            }
        }

        [XmlIgnore]
        [JsonInclude]
        public string? CorporationName { get; set; }

        [XmlIgnore]
        [JsonInclude]
        public DateTime StartDate { get; set; }
    }
}
