using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.Eve
{
    public sealed class SerializableAPIContracts
    {
        private readonly Collection<SerializableContractListItem> m_contracts;

        public SerializableAPIContracts()
        {
            m_contracts = new Collection<SerializableContractListItem>();
        }

        [XmlArray("contracts")]
        [XmlArrayItem("contract")]
        public Collection<SerializableContractListItem> Contracts => m_contracts;
    }
}
