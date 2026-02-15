using System;
using System.Threading.Tasks;

namespace EVEMon.Core.Interfaces
{
    /// <summary>
    /// Provides image loading with caching.
    /// Breaks Model -> ImageService dependency (6 call sites, 6 files).
    /// Returns object typed as System.Drawing.Image at runtime.
    /// </summary>
    public interface IImageService
    {
        /// <summary>
        /// Downloads or retrieves from cache an image at the given URL.
        /// </summary>
        /// <param name="url">The image URL.</param>
        /// <param name="useCache">When true, checks the cache before downloading.</param>
        /// <returns>The image object, or null if loading failed.</returns>
        Task<object?> GetImageAsync(Uri url, bool useCache = true);
    }
}
