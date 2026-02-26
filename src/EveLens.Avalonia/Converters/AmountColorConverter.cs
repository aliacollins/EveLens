// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace EveLens.Avalonia.Converters
{
    /// <summary>
    /// Converts a numeric amount to a color brush: positive=Green, negative=Red, zero=Secondary.
    /// </summary>
    public sealed class AmountColorConverter : IValueConverter
    {
        public static readonly AmountColorConverter Instance = new();

        private static readonly IBrush PositiveBrush = new SolidColorBrush(Color.Parse("#FF81C784"));
        private static readonly IBrush NegativeBrush = new SolidColorBrush(Color.Parse("#FFCF6679"));
        private static readonly IBrush ZeroBrush = new SolidColorBrush(Color.Parse("#FFAAAAAA"));

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is null)
                return ZeroBrush;

            try
            {
                var amount = System.Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                if (amount > 0) return PositiveBrush;
                if (amount < 0) return NegativeBrush;
                return ZeroBrush;
            }
            catch
            {
                return ZeroBrush;
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException("AmountColorConverter is one-way only.");
        }
    }
}
