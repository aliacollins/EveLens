using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.EveMarketData.MarketPricer
{
    [XmlRoot("emd")]
    public sealed class SerializableEMDItemPrices
    {
        [XmlElement("result")]
        public SerializableEMDItemPriceList? Result { get; set; }
    }
}
