using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Serialization;
using EVEMon.Common.Extensions;

namespace EVEMon.Common.SettingsObjects
{
    /// <summary>
    /// Settings for Planetary Colonies.
    /// </summary>
    /// <remarks>
    /// This is the optimized way to implement the object as serializable and satisfy all FxCop rules.
    /// Don't use auto-property with private setter for the collections as it does not work with XmlSerializer.
    /// </remarks>
    public sealed class PlanetarySettings
    {
        private readonly Collection<PlanetaryColumnSettings> m_columns;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlanetarySettings"/> class.
        /// </summary>
        public PlanetarySettings()
        {
            m_columns = new Collection<PlanetaryColumnSettings>();
        }
        
        /// <summary>
        /// Gets or sets the columns.
        /// </summary>
        /// <value>The columns.</value>
        [XmlArray("columns")]
        [XmlArrayItem("column")]
        public Collection<PlanetaryColumnSettings> Columns
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
        /// Gets or sets a value indicating whether to show only the ECU.
        /// </summary>
        /// <value>
        ///   <c>true</c> if to show only the ECU; otherwise, <c>false</c>.
        /// </value>
        [XmlElement("showEcuOnly")]
        public bool ShowEcuOnly { get; set; }

        /// <summary>
        /// Gets the default columns.
        /// </summary>
        /// <value>The default columns.</value>
        public IEnumerable<PlanetaryColumnSettings> DefaultColumns
        {
            get
            {
                PlanetaryColumn[] defaultColumns =
                {
                    PlanetaryColumn.State,
                    PlanetaryColumn.TTC,
                    PlanetaryColumn.TypeName,
                    PlanetaryColumn.ContentTypeName,
                    PlanetaryColumn.QuantityPerCycle,
                    PlanetaryColumn.Quantity,
                    PlanetaryColumn.Volume,
                };

                return EnumExtensions.GetValues<PlanetaryColumn>().Where(
                    column => column != PlanetaryColumn.None).Where(
                        column => Columns.All(columnSetting => columnSetting.Column != column)).Select(
                            column => new PlanetaryColumnSettings
                            {
                                Column = column,
                                Visible = defaultColumns.Contains(column),
                                Width = -2
                            });
            }
        }
    }
}
