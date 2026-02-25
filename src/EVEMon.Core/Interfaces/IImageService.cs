// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Threading.Tasks;

namespace EVEMon.Core.Interfaces
{
    /// <summary>
    /// Provides image downloading with disk caching.
    /// Breaks the Model to <c>ImageService</c> static service dependency (6 call sites, 6 files).
    /// </summary>
    /// <remarks>
    /// Images are cached on disk in <c>IApplicationPaths.ImageCacheDirectory</c>.
    /// When <paramref name="useCache"/> is true (default), the disk cache is checked before
    /// making an HTTP request. Returns null if the download fails or the URL is unreachable.
    ///
    /// Return type is <c>object?</c> (actually <c>SkiaSharp.SKBitmap</c> at runtime) because
    /// the Core assembly cannot reference <c>SkiaSharp</c>. Callers must cast to
    /// <c>SkiaSharp.SKBitmap</c>.
    ///
    /// Production: <c>ImageServiceAdapter</c> in <c>EVEMon.Common/Services/ImageServiceAdapter.cs</c>
    /// (delegates to static <c>ImageService.GetImageAsync()</c>).
    /// Testing: Provide a stub returning null or a known test image.
    /// </remarks>
    public interface IImageService
    {
        /// <summary>
        /// Downloads or retrieves from disk cache an image at the given URL.
        /// Returns the image as <c>SkiaSharp.SKBitmap</c> (typed as <c>object</c>),
        /// or null if the download failed or the URL was unreachable.
        /// </summary>
        /// <param name="url">The fully qualified image URL.</param>
        /// <param name="useCache">When true (default), checks the disk cache before downloading.</param>
        /// <returns>The image object, or null on failure.</returns>
        Task<object?> GetImageAsync(Uri url, bool useCache = true);
    }
}
