using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Serialization;
using EVEMon.Common.Enumerations.UISettings;
using EVEMon.Common.Extensions;

namespace EVEMon.Common.SettingsObjects
{
    /// <summary>
    /// Settings for EVE Mail Messages.
    /// </summary>
    /// <remarks>
    /// This is the optimized way to implement the object as serializable and satisfy all FxCop rules.
    /// Don't use auto-property with private setter for the collections as it does not work with XmlSerializer.
    /// </remarks>
    public sealed class EveMailMessageSettings
    {
        private readonly Collection<EveMailMessageColumnSettings> m_columns;

        /// <summary>
        /// Initializes a new instance of the <see cref="EveMailMessageSettings"/> class.
        /// </summary>
        public EveMailMessageSettings()
        {
            m_columns = new Collection<EveMailMessageColumnSettings>();

            ReadingPanePosition = ReadingPanePositioning.Off;
        }

        /// <summary>
        /// Gets or sets the columns.
        /// </summary>
        /// <value>The columns.</value>
        [XmlArray("columns")]
        [XmlArrayItem("column")]
        public Collection<EveMailMessageColumnSettings> Columns
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
        /// Gets or sets the reading pane position.
        /// </summary>
        /// <value>The reading pane position.</value>
        [XmlElement("readingPanePosition")]
        public ReadingPanePositioning ReadingPanePosition { get; set; }

        /// <summary>
        /// Gets the default columns.
        /// </summary>
        /// <value>The default columns.</value>
        public IEnumerable<EveMailMessageColumnSettings> DefaultColumns
        {
            get
            {
                EveMailMessageColumn[] defaultColumns =
                {
                    EveMailMessageColumn.SenderName,
                    EveMailMessageColumn.Title,
                    EveMailMessageColumn.SentDate,
                    EveMailMessageColumn.ToCharacters
                };

                return EnumExtensions.GetValues<EveMailMessageColumn>().Where(
                    column => column != EveMailMessageColumn.None).Where(
                        column => Columns.All(columnSetting => columnSetting.Column != column)).Select(
                            column => new EveMailMessageColumnSettings
                                      {
                                          Column = column,
                                          Visible = defaultColumns.Contains(column),
                                          Width = -2
                                      });
            }
        }
    }
}