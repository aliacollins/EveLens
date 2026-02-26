// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Enumerations.CCPAPI;
using EveLens.Common.Extensions;

namespace EveLens.Common.Models.Extended
{
    public static class ESIMethods
    {
        private static readonly List<Enum> s_items = new List<Enum>();

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        internal static void Initialize()
        {
            s_items.AddRange(EnumExtensions.GetValues<ESIAPIGenericMethods>().Cast<Enum>());
            s_items.AddRange(EnumExtensions.GetValues<ESIAPICharacterMethods>().Cast<Enum>());
            s_items.AddRange(EnumExtensions.GetValues<ESIAPICorporationMethods>().Cast<Enum>());
        }

        /// <summary>
        /// Gets the methods.
        /// </summary>
        /// <value>The methods.</value>
        public static IEnumerable<Enum> Methods => s_items;
    }
}
