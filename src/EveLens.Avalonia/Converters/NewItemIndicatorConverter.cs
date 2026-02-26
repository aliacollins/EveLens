// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace EveLens.Avalonia.Converters
{
    /// <summary>
    /// Multi-value converter that determines if an item is "new" by comparing
    /// an item timestamp against the last-viewed timestamp. Returns true if the
    /// item is newer than the last-viewed time.
    /// Values[0] = DateTime (item timestamp), Values[1] = DateTime (last viewed).
    /// </summary>
    public sealed class NewItemIndicatorConverter : IMultiValueConverter
    {
        public static readonly NewItemIndicatorConverter Instance = new();

        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count < 2)
                return false;

            if (values[0] is DateTime itemTimestamp && values[1] is DateTime lastViewed)
            {
                return itemTimestamp > lastViewed;
            }

            return false;
        }
    }
}
