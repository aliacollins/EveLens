using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.EveMarketData.MarketPricer
{
    public sealed class SerializableEMDItemPriceList
    {
        private readonly Collection<SerializableEMDItemPriceListItem> m_itemPrices;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializableEMDItemPriceList"/> class.
        /// </summary>
        public SerializableEMDItemPriceList()
        {
            m_itemPrices = new Collection<SerializableEMDItemPriceListItem>();
        }

        [XmlArray("rowset")]
        [XmlArrayItem("row")]
        public Collection<SerializableEMDItemPriceListItem> ItemPrices => m_itemPrices;
    }
}
