using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using EVEMon.Common.Data;

namespace EVEMon.Common.Serialization.FittingClf
{
    [DataContract]
    public sealed class SerializableClfFittingModule
    {
        private Collection<SerializableClfFittingChargeType> m_charges = new();

        [DataMember(Name = "typeid")]
        public int TypeID
        {
            get { return Item?.ID ?? 0; }
            set
            {
                Item = StaticItems.GetItemByID(value) ?? Item.UnknownItem;
            }
        }

        [DataMember(Name = "charges")]
        public Collection<SerializableClfFittingChargeType> Charges => m_charges ?? (m_charges = new Collection<SerializableClfFittingChargeType>());

        public Item? Item { get; set; }
    }
}