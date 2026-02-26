// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Globalization;
using System.Text;
using Avalonia.Data.Converters;

namespace EveLens.Avalonia.Converters
{
    /// <summary>
    /// Converts a TimeSpan to descriptive text (e.g., "2d 5h 30m").
    /// Returns "Done" for zero or negative timespans.
    /// </summary>
    public sealed class TimeSpanFormatConverter : IValueConverter
    {
        public static readonly TimeSpanFormatConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not TimeSpan timeSpan)
                return null;

            if (timeSpan <= TimeSpan.Zero)
                return "Done";

            var sb = new StringBuilder();

            if (timeSpan.Days > 0)
                sb.Append(timeSpan.Days).Append("d ");

            if (timeSpan.Hours > 0 || timeSpan.Days > 0)
                sb.Append(timeSpan.Hours).Append("h ");

            if (timeSpan.Minutes > 0 || timeSpan.Hours > 0 || timeSpan.Days > 0)
                sb.Append(timeSpan.Minutes).Append("m");

            // For very short timespans, show seconds
            if (timeSpan.Days == 0 && timeSpan.Hours == 0 && timeSpan.Minutes == 0)
                sb.Append(timeSpan.Seconds).Append("s");

            return sb.ToString().TrimEnd();
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException("TimeSpanFormatConverter is one-way only.");
        }
    }
}
