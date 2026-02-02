using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Serialization;
using EVEMon.Common.Extensions;

namespace EVEMon.Common.SettingsObjects
{
    /// <summary>
    /// Settings for Industry Jobs.
    /// </summary>
    /// <remarks>
    /// This is the optimized way to implement the object as serializable and satisfy all FxCop rules.
    /// Don't use auto-property with private setter for the collections as it does not work with XmlSerializer.
    /// </remarks>
    public sealed class IndustryJobSettings
    {
        private readonly Collection<IndustryJobColumnSettings> m_columns;

        /// <summary>
        /// Initializes a new instance of the <see cref="IndustryJobSettings"/> class.
        /// </summary>
        public IndustryJobSettings()
        {
            m_columns = new Collection<IndustryJobColumnSettings>();

            HideInactiveJobs = true;
        }

        /// <summary>
        /// Gets or sets the columns.
        /// </summary>
        /// <value>The columns.</value>
        [XmlArray("columns")]
        [XmlArrayItem("column")]
        public Collection<IndustryJobColumnSettings> Columns
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
        /// Gets or sets a value indicating whether [hide inactive jobs].
        /// </summary>
        /// <value><c>true</c> if [hide inactive jobs]; otherwise, <c>false</c>.</value>
        [XmlElement("hideInactiveJobs")]
        public bool HideInactiveJobs { get; set; }

        /// <summary>
        /// Gets the default columns.
        /// </summary>
        /// <value>The default columns.</value>
        public IEnumerable<IndustryJobColumnSettings> DefaultColumns
        {
            get
            {
                IndustryJobColumn[] defaultColumns =
                {
                    IndustryJobColumn.State,
                    IndustryJobColumn.TTC,
                    IndustryJobColumn.InstalledItem,
                    IndustryJobColumn.OutputItem
                };

                return EnumExtensions.GetValues<IndustryJobColumn>().Where(
                    column => column != IndustryJobColumn.None).Where(
                        column => Columns.All(columnSetting => columnSetting.Column != column)).Select(
                            column => new IndustryJobColumnSettings
                            {
                                Column = column,
                                Visible = defaultColumns.Contains(column),
                                Width = -2
                            });

            }
        }
    }
}