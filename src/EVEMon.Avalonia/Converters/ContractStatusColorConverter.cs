using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using EVEMon.Common.Enumerations;

namespace EVEMon.Avalonia.Converters
{
    /// <summary>
    /// Converts a <see cref="ContractState"/> value to a color brush for status display.
    /// Created/Assigned (active) = Yellow, Finished = Green, Expired/Failed/Rejected/Canceled = Red.
    /// </summary>
    public sealed class ContractStatusColorConverter : IValueConverter
    {
        public static readonly ContractStatusColorConverter Instance = new();

        // Yellow — outstanding/active contracts
        private static readonly IBrush ActiveBrush = new SolidColorBrush(Color.Parse("#FFFFD740"));
        // Green — completed/finished contracts
        private static readonly IBrush CompletedBrush = new SolidColorBrush(Color.Parse("#FF81C784"));
        // Red — expired, failed, rejected, canceled
        private static readonly IBrush FailedBrush = new SolidColorBrush(Color.Parse("#FFCF6679"));
        // Default — unknown or deleted
        private static readonly IBrush DefaultBrush = new SolidColorBrush(Color.Parse("#FFAAAAAA"));

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is ContractState state)
            {
                return state switch
                {
                    ContractState.Created => ActiveBrush,
                    ContractState.Assigned => ActiveBrush,
                    ContractState.Finished => CompletedBrush,
                    ContractState.Expired => FailedBrush,
                    ContractState.Failed => FailedBrush,
                    ContractState.Rejected => FailedBrush,
                    ContractState.Canceled => FailedBrush,
                    _ => DefaultBrush
                };
            }

            return DefaultBrush;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException("ContractStatusColorConverter is one-way only.");
        }
    }
}
