using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Serialization;
using EVEMon.Common.Extensions;

namespace EVEMon.Common.SettingsObjects
{
    public sealed class ContractSettings
    {
        private readonly Collection<ContractColumnSettings> m_columns;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContractSettings"/> class.
        /// </summary>
        public ContractSettings()
        {
            m_columns = new Collection<ContractColumnSettings>();

            HideInactiveContracts = true;
        }

        /// <summary>
        /// Gets or sets the columns.
        /// </summary>
        /// <value>The columns.</value>
        [XmlArray("columns")]
        [XmlArrayItem("column")]
        public Collection<ContractColumnSettings> Columns
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
        /// Gets or sets a value indicating whether [hide inactive orders].
        /// </summary>
        /// <value><c>true</c> if [hide inactive orders]; otherwise, <c>false</c>.</value>
        [XmlElement("hideInactiveContracts")]
        public bool HideInactiveContracts { get; set; }

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
        public IEnumerable<ContractColumnSettings> DefaultColumns
        {
            get
            {
                ContractColumn[] defaultColumns = new[]
                                                         {
                                                             ContractColumn.Status,
                                                             ContractColumn.ContractText,
                                                             ContractColumn.ContractType,
                                                             ContractColumn.Issuer,
                                                             ContractColumn.Assignee,
                                                             ContractColumn.Issued,
                                                             ContractColumn.Expiration
                                                         };

                return EnumExtensions.GetValues<ContractColumn>().Where(
                    column => column != ContractColumn.None).Where(
                        column => Columns.All(columnSetting => columnSetting.Column != column)).Select(
                            column => new ContractColumnSettings
                                              {
                                                  Column = column,
                                                  Visible = defaultColumns.Contains(column),
                                                  Width = -2
                                              });
            }
        }
    }
}
