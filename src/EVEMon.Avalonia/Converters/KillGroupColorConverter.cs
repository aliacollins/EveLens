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
    /// Converts a kill group key string to a color brush: "Kills" → Green, "Losses" → Red.
    /// </summary>
    public sealed class KillGroupColorConverter : IValueConverter
    {
        public static readonly KillGroupColorConverter Instance = new();

        private static readonly IBrush KillBrush = new SolidColorBrush(Color.Parse("#FF81C784"));
        private static readonly IBrush LossBrush = new SolidColorBrush(Color.Parse("#FFCF6679"));
        private static readonly IBrush DefaultBrush = new SolidColorBrush(Color.Parse("#FFAAAAAA"));

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string key)
            {
                if (key.Equals("Kills", StringComparison.OrdinalIgnoreCase))
                    return KillBrush;
                if (key.Equals("Losses", StringComparison.OrdinalIgnoreCase))
                    return LossBrush;
            }

            if (value is bool isLoss)
                return isLoss ? LossBrush : KillBrush;

            return DefaultBrush;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException("KillGroupColorConverter is one-way only.");
        }
    }
}
