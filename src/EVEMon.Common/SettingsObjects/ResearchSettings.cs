using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Serialization;
using EVEMon.Common.Extensions;

namespace EVEMon.Common.SettingsObjects
{
    /// <summary>
    /// Settings for Research.
    /// </summary>
    /// <remarks>
    /// This is the optimized way to implement the object as serializable and satisfy all FxCop rules.
    /// Don't use auto-property with private setter for the collections as it does not work with XmlSerializer.
    /// </remarks>
    public sealed class ResearchSettings
    {
        private readonly Collection<ResearchColumnSettings> m_columns;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResearchSettings"/> class.
        /// </summary>
        public ResearchSettings()
        {
            m_columns = new Collection<ResearchColumnSettings>();
        }

        /// <summary>
        /// Gets or sets the columns.
        /// </summary>
        /// <value>The columns.</value>
        [XmlArray("columns")]
        [XmlArrayItem("column")]
        public Collection<ResearchColumnSettings> Columns
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
        /// Gets the default columns.
        /// </summary>
        /// <value>The default columns.</value>
        public IEnumerable<ResearchColumnSettings> DefaultColumns
        {
            get
            {
                ResearchColumn[] defaultColumns = new[]
                                                      {
                                                          ResearchColumn.Agent,
                                                          ResearchColumn.Field,
                                                          ResearchColumn.CurrentRP,
                                                          ResearchColumn.PointsPerDay,
                                                          ResearchColumn.Station
                                                      };

                return EnumExtensions.GetValues<ResearchColumn>().Where(
                    column => column != ResearchColumn.None).Where(
                        column => Columns.All(columnSetting => columnSetting.Column != column)).Select(
                            column => new ResearchColumnSettings
                                              {
                                                  Column = column,
                                                  Visible = defaultColumns.Contains(column),
                                                  Width = -2
                                              });
            }
        }
    }
}