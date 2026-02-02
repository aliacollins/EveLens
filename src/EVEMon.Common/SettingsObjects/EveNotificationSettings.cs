using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Serialization;
using EVEMon.Common.Enumerations.UISettings;
using EVEMon.Common.Extensions;

namespace EVEMon.Common.SettingsObjects
{
    /// <summary>
    /// Settings for EVE Notifications.
    /// </summary>
    /// <remarks>
    /// This is the optimized way to implement the object as serializable and satisfy all FxCop rules.
    /// Don't use auto-property with private setter for the collections as it does not work with XmlSerializer.
    /// </remarks>
    public sealed class EveNotificationSettings
    {
        private readonly Collection<EveNotificationColumnSettings> m_columns;

        /// <summary>
        /// Initializes a new instance of the <see cref="EveNotificationSettings"/> class.
        /// </summary>
        public EveNotificationSettings()
        {
            m_columns = new Collection<EveNotificationColumnSettings>();

            ReadingPanePosition = ReadingPanePositioning.Off;
        }

        /// <summary>
        /// Gets or sets the columns.
        /// </summary>
        /// <value>The columns.</value>
        [XmlArray("columns")]
        [XmlArrayItem("column")]
        public Collection<EveNotificationColumnSettings> Columns
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
        public IEnumerable<EveNotificationColumnSettings> DefaultColumns
        {
            get
            {
                EveNotificationColumn[] defaultColumns = new[]
                                                              {
                                                                  EveNotificationColumn.SenderName,
                                                                  EveNotificationColumn.Type,
                                                                  EveNotificationColumn.SentDate
                                                              };

                return EnumExtensions.GetValues<EveNotificationColumn>().Where(
                    column => column != EveNotificationColumn.None).Where(
                        column => Columns.All(columnSetting => columnSetting.Column != column)).Select(
                            column => new EveNotificationColumnSettings
                                              {
                                                  Column = column,
                                                  Visible = defaultColumns.Contains(column),
                                                  Width = -2
                                              });
            }
        }
    }
}