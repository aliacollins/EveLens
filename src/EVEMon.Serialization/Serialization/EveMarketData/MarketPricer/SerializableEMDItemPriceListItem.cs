using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.EveMarketData.MarketPricer
{
    public sealed class SerializableEMDItemPriceListItem
    {
        [XmlAttribute("typeID")]
        public int ID { get; set; }

        [XmlAttribute("buysell")]
        public string? BuySell { get; set; }

        [XmlAttribute("price")]
        public double Price { get; set; }
    }
}
