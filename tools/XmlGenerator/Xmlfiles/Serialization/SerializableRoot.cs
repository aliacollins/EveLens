using System.Xml.Serialization;

namespace EveLens.XmlGenerator.Xmlfiles.Serialization
{
    public class SerializableRoot<T>
    {
        [XmlElement("rowset")]
        public SerialiazableRowset<T> Rowset { get; set; }
    }
}