// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Enumerations;
using EveLens.Common.Serialization.Esi;
using EveLens.Common.Serialization.Settings;

namespace EveLens.Common.Models
{
    /// <summary>
    /// This class represents a sell order.
    /// </summary>
    public sealed class SellOrder : MarketOrder
    {
        /// <summary>
        /// Constructor from the API.
        /// </summary>
        /// <param name="src">The source.</param>
        /// <param name="issuedFor">Whether the order was issued for a corporation or a
        /// character.</param>
        internal SellOrder(EsiOrderListItem src, IssuedFor issuedFor, CCPCharacter character)
            : base(src, issuedFor, character)
        {
        }

        /// <summary>
        /// Constructor from an object deserialized from the settings file.
        /// </summary>
        /// <param name="src"></param>
        internal SellOrder(SerializableOrderBase src, CCPCharacter character)
            : base(src, character)
        {
        }

        /// <summary>
        /// Exports the given object to a serialization object.
        /// </summary>
        /// <returns></returns>
        public override SerializableOrderBase Export() => Export(new SerializableSellOrder());
    }
}
