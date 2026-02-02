using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Serialization;
using EVEMon.Common.Extensions;

namespace EVEMon.Common.SettingsObjects
{
    /// <summary>
    /// Settings for Wallet Transactions.
    /// </summary>
    /// <remarks>
    /// This is the optimized way to implement the object as serializable and satisfy all FxCop rules.
    /// Don't use auto-property with private setter for the collections as it does not work with XmlSerializer.
    /// </remarks>
    public sealed class WalletTransactionSettings
    {
        private readonly Collection<WalletTransactionColumnSettings> m_columns;

        /// <summary>
        /// Initializes a new instance of the <see cref="WalletTransactionSettings"/> class.
        /// </summary>
        public WalletTransactionSettings()
        {
            m_columns = new Collection<WalletTransactionColumnSettings>();
        }

        /// <summary>
        /// Gets or sets the columns.
        /// </summary>
        /// <value>The columns.</value>
        [XmlArray("columns")]
        [XmlArrayItem("column")]
        public Collection<WalletTransactionColumnSettings> Columns
        {
            get => m_columns;
            set
            {
                m_columns.Clear();
                if (value != null)
                {
                    foreach (var item in value)
                        m_columns.Add(item);
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [number abs format].
        /// </summary>
        /// <value><c>true</c> if [number abs format]; otherwise, <c>false</c>.</value>
        [XmlElement("numberAbsFormat")]
        public bool NumberAbsFormat { get; set; }

        /// <summary>
        /// Gets the default columns.
        /// </summary>
        /// <value>The default columns.</value>
        public IEnumerable<WalletTransactionColumnSettings> DefaultColumns
        {
            get
            {
                WalletTransactionColumn[] defaultColumns = new[]
                                                               {
                                                                   WalletTransactionColumn.Date,
                                                                   WalletTransactionColumn.ItemName,
                                                                   WalletTransactionColumn.Price,
                                                                   WalletTransactionColumn.Quantity,
                                                                   WalletTransactionColumn.Credit,
                                                                   WalletTransactionColumn.Client,
                                                                   WalletTransactionColumn.Station
                                                               };

                return EnumExtensions.GetValues<WalletTransactionColumn>().Where(
                    column => column != WalletTransactionColumn.None).Where(
                        column => Columns.All(columnSetting => columnSetting.Column != column)).Select(
                            column => new WalletTransactionColumnSettings
                                          {
                                              Column = column,
                                              Visible = defaultColumns.Contains(column),
                                              Width = -2
                                          });
            }
        }
    }
}
