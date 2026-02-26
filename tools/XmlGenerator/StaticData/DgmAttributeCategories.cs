using System.Xml.Serialization;
using EveLens.XmlGenerator.Interfaces;

namespace EveLens.XmlGenerator.StaticData
{
    public sealed class DgmAttributeCategories : IHasID
    {
        [XmlElement("categoryID")]
        public int ID { get; set; }

        [XmlElement("categoryName")]
        public string Name { get; set; }

        [XmlElement("categoryDescription")]
        public string Description { get; set; }
    }
}