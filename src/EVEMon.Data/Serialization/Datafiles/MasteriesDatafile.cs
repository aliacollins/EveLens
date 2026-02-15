using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.Datafiles
{
    /// <summary>
    /// Represents our masteries datafile
    /// </summary>
    /// <remarks>
    /// This is the optimized way to implement the object as serializable and satisfy all FxCop rules.
    /// Don't use auto-property with private setter for the collections as it does not work with XmlSerializer.
    /// </remarks>
    [XmlRoot("masteriesDatafile")]
    public sealed class MasteriesDatafile
    {
        private readonly Collection<SerializableMasteryShip> m_masteryShips;

        /// <summary>
        /// Initializes a new instance of the <see cref="MasteriesDatafile"/> class.
        /// </summary>
        public MasteriesDatafile()
        {
            m_masteryShips = new Collection<SerializableMasteryShip>();
        }

        /// <summary>
        /// Gets the masteries groups.
        /// </summary>
        /// <value>The groups.</value>
        [XmlElement("ship")]
        public Collection<SerializableMasteryShip> MasteryShips => m_masteryShips;
    }
}
