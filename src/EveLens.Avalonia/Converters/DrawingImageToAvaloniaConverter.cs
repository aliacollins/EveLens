// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Globalization;
using System.IO;
using Avalonia.Data.Converters;
using SkiaSharp;

namespace EveLens.Avalonia.Converters
{
    /// <summary>
    /// Converts an SkiaSharp.SKBitmap to an Avalonia.Media.Imaging.Bitmap.
    /// This bridges the SkiaSharp image infrastructure in EveLens.Common to Avalonia's rendering pipeline.
    /// </summary>
    public sealed class DrawingImageToAvaloniaConverter : IValueConverter
    {
        public static readonly DrawingImageToAvaloniaConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not SKBitmap skBitmap)
                return null;

            try
            {
                using var data = skBitmap.Encode(SKEncodedImageFormat.Png, 100);
                using var stream = new MemoryStream(data.ToArray());
                return new global::Avalonia.Media.Imaging.Bitmap(stream);
            }
            catch (Exception)
            {
                // Image may be disposed or corrupt
                return null;
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException("DrawingImageToAvaloniaConverter is one-way only.");
        }
    }
}
