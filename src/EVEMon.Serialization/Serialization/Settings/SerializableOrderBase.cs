using System;
using System.Xml.Serialization;
using EVEMon.Common.Enumerations;

namespace EVEMon.Common.Serialization.Settings
{
    /// <summary>
    /// Represents a market sell order.
    /// </summary>
    public class SerializableOrderBase
    {
        /// <summary>
        /// Unique order ID for this order. Note that these are not guaranteed to be unique forever, they can recycle. 
        /// But they are unique for the purpose of one data pull. 
        /// </summary>
        [XmlAttribute("orderID")]
        public long OrderID { get; set; }

        /// <summary>
        /// The state of the order.
        /// </summary>
        [XmlAttribute("orderState")]
        public OrderState State { get; set; }

        /// <summary>
        /// The cost per unit for this order.
        /// </summary>
        [XmlAttribute("price")]
        public decimal UnitaryPrice { get; set; }

        /// <summary>
        /// The remaining volume of the order.
        /// </summary>
        [XmlAttribute("volRemaining")]
        public int RemainingVolume { get; set; }

        /// <summary>
        /// The time this order was issued.
        /// </summary>
        [XmlAttribute("issued")]
        public DateTime Issued { get; set; }

        /// <summary>
        /// Which this order was issued for.
        /// </summary>
        [XmlAttribute("issuedFor")]
        public IssuedFor IssuedFor { get; set; }

        /// <summary>
        /// The time this order state was last changed.
        /// </summary>
        [XmlAttribute("lastStateChange")]
        public DateTime LastStateChange { get; set; }
    }
}