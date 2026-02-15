using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.Settings
{
    public sealed class SerializablePortableEveInstallation
    {
        [XmlAttribute("path")]
        public string? Path { get; set; }
    }
}