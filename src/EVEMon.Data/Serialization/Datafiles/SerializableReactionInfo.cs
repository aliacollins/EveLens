using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.Datafiles
{
    public sealed class SerializableReactionInfo : SerializableMaterialQuantity
    {
        /// <summary>
        /// Gets or sets a value indicating whether this instance is input.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is input; otherwise, <c>false</c>.
        /// </value>
        [XmlAttribute("input")]
        public bool IsInput { get; set; }
    }
}