// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia.Data.Converters;

namespace EVEMon.Avalonia.Converters
{
    /// <summary>
    /// Converts a System.Drawing.Image or System.Drawing.Bitmap to an Avalonia.Media.Imaging.Bitmap.
    /// This bridges the WinForms image infrastructure in EVEMon.Common to Avalonia's rendering pipeline.
    /// </summary>
    public sealed class DrawingImageToAvaloniaConverter : IValueConverter
    {
        public static readonly DrawingImageToAvaloniaConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not System.Drawing.Bitmap drawingBitmap)
            {
                if (value is System.Drawing.Image drawingImage)
                {
                    // Image may not be a Bitmap directly; convert it
                    drawingBitmap = new System.Drawing.Bitmap(drawingImage);
                }
                else
                {
                    return null;
                }
            }

            try
            {
                using var ms = new MemoryStream();
                drawingBitmap.Save(ms, ImageFormat.Png);
                ms.Position = 0;
                return new global::Avalonia.Media.Imaging.Bitmap(ms);
            }
            catch (ExternalException)
            {
                // GDI+ error — image may be disposed or corrupt
                return null;
            }
            catch (ArgumentException)
            {
                // Invalid image data
                return null;
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException("DrawingImageToAvaloniaConverter is one-way only.");
        }
    }
}
