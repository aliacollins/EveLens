// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace EVEMon.Avalonia.Converters
{
    /// <summary>
    /// Converts a standing value to a bar width: |standing| / 10.0 * 60.0 (half bar width = 60px).
    /// </summary>
    public sealed class StandingWidthConverter : IValueConverter
    {
        public static readonly StandingWidthConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is null)
                return 0.0;

            try
            {
                var standing = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
                return Math.Abs(standing) / 10.0 * 60.0;
            }
            catch
            {
                return 0.0;
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException("StandingWidthConverter is one-way only.");
        }
    }
}
