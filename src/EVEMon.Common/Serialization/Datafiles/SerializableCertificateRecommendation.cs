using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.Datafiles
{
    /// <summary>
    /// Represents a recommendation for a ship
    /// </summary>
    public sealed class SerializableCertificateRecommendation
    {
        /// <summary>
        /// Gets or sets the ID.
        /// </summary>
        /// <value>The ID.</value>
        [XmlAttribute("id")]
        public int ID { get; set; }

        /// <summary>
        /// Gets or sets the ship.
        /// </summary>
        /// <value>The ship.</value>
        [XmlAttribute("name")]
        public string? ShipName { get; set; }
    }
}