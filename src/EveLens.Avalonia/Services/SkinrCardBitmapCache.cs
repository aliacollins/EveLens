// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using EveLens.Avalonia.Converters;
using EveLens.Common.Helpers;
using EveLens.Common.Service;

namespace EveLens.Avalonia.Services
{
    /// <summary>
    /// Decoded card art for the SKINR window, decoded ONCE and reused across grid
    /// rebuilds. Before this cache, every market refresh re-decoded every card's
    /// PNG — and re-ran the SKBitmap→PNG→Bitmap conversion for every hull render —
    /// on the UI thread. A macOS hang report showed the main thread pinned inside
    /// SkiaSharp decode with a 4 GB footprint: hundreds of cards times a refresh
    /// per prerenderer event, every image paid for from scratch each time.
    /// </summary>
    internal sealed class SkinrCardBitmapCache : IDisposable
    {
        /// <summary>Cards render at ~200px; decoding a thumbnail wider than this
        /// only buys memory. CCP hull renders are requested at 256 anyway.</summary>
        private const int DecodeWidth = 256;

        private readonly Dictionary<string, Bitmap?> _files = new(
            StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<int, Task<Bitmap?>> _hulls = new();
        private bool _disposed;

        /// <summary>A design thumbnail from disk, decoded to card width once. Null
        /// when the file is unreadable — the caller shows its placeholder glyph.</summary>
        public Bitmap? GetFile(string path)
        {
            if (_disposed)
                return null;
            if (_files.TryGetValue(path, out Bitmap? cached))
                return cached;
            Bitmap? bitmap = null;
            try
            {
                using FileStream fs = File.OpenRead(path);
                bitmap = Bitmap.DecodeToWidth(fs, DecodeWidth);
            }
            catch (Exception)
            {
                // A truncated cache file must not break the grid; the null is
                // remembered so the bad file is not re-read on every rebuild.
            }
            _files[path] = bitmap;
            return bitmap;
        }

        /// <summary>
        /// CCP's official hull render, fetched and converted once per type id.
        /// Single-flight: five designs on the same hull share one conversion
        /// instead of racing five (UI-thread callers, so no lock needed).
        /// </summary>
        public Task<Bitmap?> GetHullRenderAsync(int typeId)
        {
            if (_disposed)
                return Task.FromResult<Bitmap?>(null);
            if (_hulls.TryGetValue(typeId, out Task<Bitmap?>? inFlight))
                return inFlight;
            Task<Bitmap?> task = FetchHullAsync(typeId);
            _hulls[typeId] = task;
            return task;
        }

        private static async Task<Bitmap?> FetchHullAsync(int typeId)
        {
            try
            {
                var drawing = await ImageService.GetImageAsync(
                    ImageHelper.GetTypeRenderURL(typeId, DecodeWidth));
                if (drawing == null)
                    return null;
                object? converted = DrawingImageToAvaloniaConverter.Instance.Convert(
                    drawing, typeof(Bitmap), null,
                    System.Globalization.CultureInfo.InvariantCulture);
                return converted as Bitmap;
            }
            catch (Exception)
            {
                return null;   // best-effort art; the hull-name text stays visible
            }
        }

        /// <summary>A fresher capture just landed at this path — forget the old
        /// decode so the next rebuild shows the new art. The old bitmap is NOT
        /// disposed here: an Image on screen may still be drawing it, and painting
        /// a disposed bitmap is a crash. The GC collects it once the rebuild drops
        /// the last reference.</summary>
        public void Invalidate(string path) => _files.Remove(path);

        public void Dispose()
        {
            _disposed = true;
            foreach (Bitmap? b in _files.Values)
                b?.Dispose();
            _files.Clear();
            _hulls.Clear();
        }
    }
}
