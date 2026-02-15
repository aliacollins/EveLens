using System.Runtime.Serialization;

namespace EVEMon.Common.Serialization.Fuzzworks
{
    [DataContract]
    public sealed class SerializableFuzzworksPriceListItem
    {
        [DataMember(Name = "weightedAverage")]
        public double AveragePrice { get; set; }

        [DataMember(Name = "max")]
        public double MaxPrice { get; set; }

        [DataMember(Name = "min")]
        public double MinPrice { get; set; }

        [DataMember(Name = "median")]
        public double MedianPrice { get; set; }
    }
}
