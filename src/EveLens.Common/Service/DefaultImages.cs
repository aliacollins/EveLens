// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using EveLens.Common.Helpers;
using SkiaSharp;

namespace EveLens.Common.Service
{
    /// <summary>
    /// Cross-platform source of fallback/placeholder images, decoded as SkiaSharp
    /// <see cref="SKBitmap"/> from embedded PNG resources.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This exists to fix a hard crash on Linux and macOS. The auto-generated
    /// <c>Properties.Resources</c> getters (e.g. <c>DefaultCharacterImage32</c>) return
    /// <see cref="System.Drawing.Bitmap"/>, which lives in <c>System.Drawing.Common</c>.
    /// On .NET 6+ that package throws <see cref="PlatformNotSupportedException"/> on any
    /// non-Windows platform the instant the GDI+ type initializer runs. Model fallback-image
    /// properties (Standing, Contact, EmploymentRecord, Loyalty) read those getters and so
    /// crashed the whole process when their tab was opened on Linux/macOS.
    /// </para>
    /// <para>
    /// The rest of the app already uses SkiaSharp <see cref="SKBitmap"/> via
    /// <see cref="ImageService"/>. This helper makes the default/fallback path use the same
    /// cross-platform pipeline, so the model layer never touches GDI+. Bitmaps are decoded
    /// once and cached; callers must NOT dispose the returned instance (it is shared).
    /// </para>
    /// </remarks>
    public static class DefaultImages
    {
        private const string ResourcePrefix = "EveLens.Common.Resources.Images.";

        private static readonly Assembly s_assembly = typeof(DefaultImages).Assembly;
        private static readonly ConcurrentDictionary<string, SKBitmap?> s_cache = new();

        /// <summary>Default character/agent portrait placeholder (32x32).</summary>
        public static SKBitmap? Character => Load("DefaultCharacterImage32.png");

        /// <summary>Default corporation logo placeholder (32x32).</summary>
        public static SKBitmap? Corporation => Load("DefaultCorporationImage32.png");

        /// <summary>Default alliance logo placeholder (32x32).</summary>
        public static SKBitmap? Alliance => Load("DefaultAllianceImage32.png");

        /// <summary>
        /// Loads and decodes an embedded image resource as a shared, cached
        /// <see cref="SKBitmap"/>. Returns <c>null</c> if the resource is missing or
        /// cannot be decoded — callers treat a missing placeholder as "no image" rather
        /// than crashing.
        /// </summary>
        /// <param name="fileName">
        /// The image file name (e.g. <c>"DefaultCharacterImage32.png"</c>), without the
        /// embedded-resource namespace prefix.
        /// </param>
        public static SKBitmap? Load(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return null;

            return s_cache.GetOrAdd(fileName, static name =>
            {
                try
                {
                    using Stream? stream = s_assembly.GetManifestResourceStream(ResourcePrefix + name);
                    if (stream == null)
                        return null;

                    using var ms = new MemoryStream();
                    stream.CopyTo(ms);
                    return SKBitmap.Decode(ms.ToArray());
                }
                catch (Exception ex)
                {
                    // A missing/corrupt placeholder must never crash the app — degrade to no image.
                    ExceptionHandler.LogException(ex, false);
                    return null;
                }
            });
        }
    }
}
