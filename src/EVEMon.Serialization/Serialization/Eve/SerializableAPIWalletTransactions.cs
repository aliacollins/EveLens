using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.Eve
{
    /// <summary>
    /// Represents a serializable version of wallet transactions. Used for querying CCP.
    /// </summary>
    public sealed class SerializableAPIWalletTransactions
    {
        private readonly Collection<SerializableWalletTransactionsListItem> m_walletTransactions;

        public SerializableAPIWalletTransactions()
        {
            m_walletTransactions = new Collection<SerializableWalletTransactionsListItem>();
        }

        [XmlArray("transactions")]
        [XmlArrayItem("transaction")]
        public Collection<SerializableWalletTransactionsListItem> WalletTransactions => m_walletTransactions;
    }
}
