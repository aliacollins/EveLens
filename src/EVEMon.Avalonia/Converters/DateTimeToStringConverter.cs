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
    /// Converts a DateTime to a human-readable string.
    /// Pass a format string as the converter parameter (e.g., "yyyy-MM-dd HH:mm").
    /// Defaults to "yyyy-MM-dd HH:mm:ss" if no parameter is provided.
    /// </summary>
    public sealed class DateTimeToStringConverter : IValueConverter
    {
        public static readonly DateTimeToStringConverter Instance = new();

        private const string DefaultFormat = "yyyy-MM-dd HH:mm:ss";

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not DateTime dateTime)
                return null;

            if (dateTime == DateTime.MinValue)
                return string.Empty;

            string format = parameter as string ?? DefaultFormat;
            return dateTime.ToString(format, CultureInfo.InvariantCulture);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException("DateTimeToStringConverter is one-way only.");
        }
    }
}
