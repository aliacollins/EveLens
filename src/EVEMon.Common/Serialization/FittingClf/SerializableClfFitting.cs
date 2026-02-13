using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace EVEMon.Common.Serialization.FittingClf
{
    [DataContract]
    public sealed class SerializableClfFitting
    {
        private Collection<SerializableClfFittingPreset> m_presets = new();
        private Collection<SerializableClfFittingDroneSet> m_drones = new();

        [DataMember(Name = "clf-version")]
        public string? ClfVersion { get; set; }

        [DataMember(Name = "metadata")]
        public SerializableClfFittingMetaData? MetaData { get; set; }

        [DataMember(Name = "ship")]
        public SerializableClfFittingShipType? Ship { get; set; }

        [DataMember(Name = "presets")]
        public Collection<SerializableClfFittingPreset> Presets => m_presets ?? (m_presets = new Collection<SerializableClfFittingPreset>());

        [DataMember(Name = "drones")]
        public Collection<SerializableClfFittingDroneSet> Drones => m_drones ?? (m_drones = new Collection<SerializableClfFittingDroneSet>());
    }
}
