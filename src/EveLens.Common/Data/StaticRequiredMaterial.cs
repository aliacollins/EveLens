// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EveLens.Common.Enumerations;
using EveLens.Common.Serialization.Datafiles;

namespace EveLens.Common.Data
{
    public class StaticRequiredMaterial : Item
    {
        #region Constructors

        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        /// <param name="src"></param>
        internal StaticRequiredMaterial(SerializableRequiredMaterial src)
            : base(src.ID, GetName(src.ID))
        {
            Quantity = src.Quantity;
            Activity = (BlueprintActivity)Enum.ToObject(typeof(BlueprintActivity), src.Activity);
        }

        #endregion


        #region Public Properties

        /// <summary>
        /// Gets or sets the quantity.
        /// </summary>
        public long Quantity { get; }

        /// <summary>
        /// Gets or sets the activity.
        /// </summary>
        public BlueprintActivity Activity { get; }

        #endregion


        #region Private Finders

        /// <summary>
        /// Gets the material's name.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns></returns>
        private static string GetName(int id)
        {
            Item item = StaticItems.GetItemByID(id);

            return item?.Name ?? string.Empty;
        }

        #endregion
    }
}