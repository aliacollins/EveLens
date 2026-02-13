using System.Runtime.Serialization;

namespace EVEMon.Common.Serialization.Fuzzworks
{
    [DataContract]
    public sealed class SerializableFuzzworksPriceItem
    {
        [DataMember(Name = "buy")]
        public SerializableFuzzworksPriceListItem? Buy { get; set; }

        [DataMember(Name = "sell")]
        public SerializableFuzzworksPriceListItem? Sell { get; set; }
    }
}
