// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using EveLens.Common.Enumerations;

namespace EveLens.Common.Serialization.Settings
{
    /// <summary>
    /// Represents a market sell order.
    /// </summary>
    [JsonDerivedType(typeof(SerializableBuyOrder), "buy")]
    [JsonDerivedType(typeof(SerializableSellOrder), "sell")]
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