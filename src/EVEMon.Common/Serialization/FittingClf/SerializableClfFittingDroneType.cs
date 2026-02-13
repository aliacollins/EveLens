using System.Runtime.Serialization;
using EVEMon.Common.Data;

namespace EVEMon.Common.Serialization.FittingClf
{
    [DataContract]
    public sealed class SerializableClfFittingDroneType
    {
        [DataMember(Name = "typeid")]
        public int TypeID
        {
            get { return Item?.ID ?? 0; }
            set
            {
                Item = StaticItems.GetItemByID(value) ?? Item.UnknownItem;
            }
        }

        [DataMember(Name = "quantity")]
        public int Quantity { get; set; }

        public Item? Item { get; set; }
    }
}