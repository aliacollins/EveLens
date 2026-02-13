using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace EVEMon.Common.Serialization.FittingClf
{
    [DataContract]
    public sealed class SerializableClfFittingPreset
    {
        private Collection<SerializableClfFittingModule> m_modules = new();

        [DataMember(Name = "presetname")]
        public string? Name { get; set; }

        [DataMember(Name = "modules")]
        public Collection<SerializableClfFittingModule> Modules => m_modules ?? (m_modules = new Collection<SerializableClfFittingModule>());
    }
}