// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace EveLens.Avalonia.Converters
{
    /// <summary>
    /// Converts a numeric value to EVE ISK format (e.g., 1234567.89 -> "1,234,567.89 ISK").
    /// </summary>
    public sealed class ISKFormatConverter : IValueConverter
    {
        public static readonly ISKFormatConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is null)
                return null;

            decimal amount;
            try
            {
                amount = System.Convert.ToDecimal(value, CultureInfo.InvariantCulture);
            }
            catch (FormatException)
            {
                return value?.ToString();
            }
            catch (OverflowException)
            {
                return value?.ToString();
            }

            return amount.ToString("N2", CultureInfo.InvariantCulture) + " ISK";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException("ISKFormatConverter is one-way only.");
        }
    }
}
