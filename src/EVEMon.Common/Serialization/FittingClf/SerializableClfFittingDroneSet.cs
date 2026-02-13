using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace EVEMon.Common.Serialization.FittingClf
{
    [DataContract]
    public sealed class SerializableClfFittingDroneSet
    {
        private Collection<SerializableClfFittingDroneType> m_inBayDrones = new();
        private Collection<SerializableClfFittingDroneType> m_inSpaceDrones = new();

        [DataMember(Name = "presetname")]
        public string? Name { get; set; }

        [DataMember(Name = "inbay")]
        public Collection<SerializableClfFittingDroneType> InBay => m_inBayDrones ?? (m_inBayDrones = new Collection<SerializableClfFittingDroneType>());

        [DataMember(Name = "inspace")]
        public Collection<SerializableClfFittingDroneType> InSpace => m_inSpaceDrones ?? (m_inSpaceDrones = new Collection<SerializableClfFittingDroneType>());
    }
}