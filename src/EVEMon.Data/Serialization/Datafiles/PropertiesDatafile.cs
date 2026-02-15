using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.Datafiles
{
    /// <summary>
    /// Represents our properties datafile.
    /// </summary>
    /// <remarks>
    /// This is the optimized way to implement the object as serializable and satisfy all FxCop rules.
    /// Don't use auto-property with private setter for the collections as it does not work with XmlSerializer.
    /// </remarks>
    [XmlRoot("propertiesDatafile")]
    public sealed class PropertiesDatafile
    {
        private readonly Collection<SerializablePropertyCategory> m_categories;

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertiesDatafile"/> class.
        /// </summary>
        public PropertiesDatafile()
        {
            m_categories = new Collection<SerializablePropertyCategory>();
        }

        /// <summary>
        /// Gets the categories.
        /// </summary>
        /// <value>The categories.</value>
        [XmlElement("category")]
        public Collection<SerializablePropertyCategory> Categories => m_categories;
    }
}