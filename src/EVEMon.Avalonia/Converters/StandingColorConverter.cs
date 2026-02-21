// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace EVEMon.Avalonia.Converters
{
    /// <summary>
    /// Converts a standing value to a color brush:
    /// positive = EveStandingPositiveBrush (#64B5F6 blue),
    /// negative = EveStandingNegativeBrush (#CF6679 red),
    /// zero = EveStandingNeutralBrush (#707070 gray).
    /// </summary>
    public sealed class StandingColorConverter : IValueConverter
    {
        public static readonly StandingColorConverter Instance = new();

        private static readonly IBrush PositiveBrush = new SolidColorBrush(Color.Parse("#FF64B5F6"));
        private static readonly IBrush NegativeBrush = new SolidColorBrush(Color.Parse("#FFCF6679"));
        private static readonly IBrush NeutralBrush = new SolidColorBrush(Color.Parse("#FF707070"));

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is null)
                return NeutralBrush;

            try
            {
                var standing = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
                if (standing > 0) return PositiveBrush;
                if (standing < 0) return NegativeBrush;
                return NeutralBrush;
            }
            catch
            {
                return NeutralBrush;
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException("StandingColorConverter is one-way only.");
        }
    }
}
