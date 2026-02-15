using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.Datafiles
{
    /// <summary>
    /// Represents our EVE geography datafile.
    /// </summary>
    /// <remarks>
    /// This is the optimized way to implement the object as serializable and satisfy all FxCop rules.
    /// Don't use auto-property with private setter for the collections as it does not work with XmlSerializer.
    /// </remarks>
    [XmlRoot("geographyDatafile")]
    public sealed class GeoDatafile
    {
        private readonly Collection<SerializableRegion> m_regions;
        private readonly Collection<SerializableJump> m_jumps;

        /// <summary>
        /// Initializes a new instance of the <see cref="GeoDatafile"/> class.
        /// </summary>
        public GeoDatafile()
        {
            m_regions = new Collection<SerializableRegion>();
            m_jumps = new Collection<SerializableJump>();
        }

        /// <summary>
        /// Gets the regions.
        /// </summary>
        /// <value>The regions.</value>
        [XmlArray("regions")]
        [XmlArrayItem("region")]
        public Collection<SerializableRegion> Regions => m_regions;

        /// <summary>
        /// Gets the jumps.
        /// </summary>
        /// <value>The jumps.</value>
        [XmlArray("jumps")]
        [XmlArrayItem("jump")]
        public Collection<SerializableJump> Jumps => m_jumps;
    }
}