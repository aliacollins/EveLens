using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace EVEMon.Common.SettingsObjects
{
    public sealed class SkillPieChartSettings
    {
        private readonly Collection<SerializableColor> m_colors; 

        /// <summary>
        /// Initializes a new instance of the <see cref="SkillPieChartSettings"/> class.
        /// </summary>
        public SkillPieChartSettings()
        {
            m_colors = new Collection<SerializableColor>();
        }

        /// <summary>
        /// Gets or sets a value indicating whether [sort by size].
        /// </summary>
        /// <value><c>true</c> if [sort by size]; otherwise, <c>false</c>.</value>
        [XmlElement("sortBySize")]
        public bool SortBySize { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [merge minor groups].
        /// </summary>
        /// <value><c>true</c> if [merge minor groups]; otherwise, <c>false</c>.</value>
        [XmlElement("mergeMinorGroups")]
        public bool MergeMinorGroups { get; set; }

        /// <summary>
        /// Gets or sets the height of the slice.
        /// </summary>
        /// <value>The height of the slice.</value>
        [XmlElement("sliceHeight")]
        public float SliceHeight { get; set; }

        /// <summary>
        /// Gets or sets the initial angle.
        /// </summary>
        /// <value>The initial angle.</value>
        [XmlElement("initialAngle")]
        public float InitialAngle { get; set; }

        /// <summary>
        /// Gets or sets the colors.
        /// </summary>
        /// <value>The colors.</value>
        [XmlElement("colors")]
        public Collection<SerializableColor> Colors
        {
            get => m_colors;
            set
            {
                m_colors.Clear();
                if (value != null)
                {
                    foreach (var item in value)
                        m_colors.Add(item);
                }
            }
        }
    }
}