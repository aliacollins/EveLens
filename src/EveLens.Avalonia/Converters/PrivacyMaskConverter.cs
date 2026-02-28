// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using EveLens.Common.Helpers;
using EveLens.Common.Services;

namespace EveLens.Avalonia.Converters
{
    /// <summary>
    /// IValueConverter that masks values when privacy mode is active.
    /// Use ConverterParameter to append a suffix (e.g., "ISK") to the masked output.
    /// </summary>
    public sealed class PrivacyMaskConverter : IValueConverter
    {
        public static readonly PrivacyMaskConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (!AppServices.PrivacyModeEnabled)
                return value?.ToString() ?? string.Empty;

            string suffix = parameter as string ?? "";
            return suffix.Length > 0 ? $"{PrivacyHelper.Mask} {suffix}" : PrivacyHelper.Mask;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException("PrivacyMaskConverter is one-way only.");
        }
    }
}
