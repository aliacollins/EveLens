using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace EVEMon.Avalonia.Converters
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
