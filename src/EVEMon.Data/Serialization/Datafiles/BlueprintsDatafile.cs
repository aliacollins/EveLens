using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.Datafiles
{
    /// <summary>
    /// Represents our blueprints datafile.
    /// </summary>
    /// <remarks>
    /// This is the optimized way to implement the object as serializable and satisfy all FxCop rules.
    /// Don't use auto-property with private setter for the collections as it does not work with XmlSerializer.
    /// </remarks>
    [XmlRoot("blueprintsDatafile")]
    public sealed class BlueprintsDatafile
    {
        private readonly Collection<SerializableBlueprintMarketGroup> m_marketGroups;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlueprintsDatafile"/> class.
        /// </summary>
        public BlueprintsDatafile()
        {
            m_marketGroups = new Collection<SerializableBlueprintMarketGroup>();
        }

        /// <summary>
        /// Gets the market groups.
        /// </summary>
        /// <value>The market groups.</value>
        [XmlElement("group")]
        public Collection<SerializableBlueprintMarketGroup> MarketGroups => m_marketGroups;
    }
}