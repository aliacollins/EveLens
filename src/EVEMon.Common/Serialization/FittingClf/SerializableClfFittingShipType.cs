using System.Runtime.Serialization;
using EVEMon.Common.Data;

namespace EVEMon.Common.Serialization.FittingClf
{
    [DataContract]
    public sealed class SerializableClfFittingShipType
    {
        [DataMember(Name = "typeid")]
        public int TypeID
        {
            get { return Item?.ID ?? 0; }
            set { Item = StaticItems.GetItemByID(value) ?? Item.UnknownItem; }
        }

        public Item? Item { get; set; }
    }
}