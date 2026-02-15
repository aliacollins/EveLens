using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.Datafiles
{
    /// <summary>
    /// Represents a connection between two jump gates.
    /// </summary>
    public sealed class SerializableJump
    {
        /// <summary>
        /// Gets or sets the first system ID.
        /// </summary>
        /// <value>The first system ID.</value>
        [XmlAttribute("id1")]
        public int FirstSystemID { get; set; }

        /// <summary>
        /// Gets or sets the second system ID.
        /// </summary>
        /// <value>The second system ID.</value>
        [XmlAttribute("id2")]
        public int SecondSystemID { get; set; }
    }
}