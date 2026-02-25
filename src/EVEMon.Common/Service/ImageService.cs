// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EVEMon.Common.Constants;
using EVEMon.Common.Enumerations;
using EVEMon.Common.Helpers;
using EVEMon.Common.Net;
using EVEMon.Common.Services;
using SkiaSharp;

namespace EVEMon.Common.Service
{
    public static class ImageService
    {
        /// <summary>
        /// Gets the image server base URI.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public static Uri GetImageServerBaseUri(string path) => new Uri(
            NetworkConstants.EVEImageServerBase + path);

        /// <summary>
        /// Asynchronously downloads a character portrait from its ID.
        /// </summary>
        /// <param name="charId"></param>
        public static async Task<SKBitmap> GetCharacterImageAsync(long charId)
        {
            string path = string.Format(CultureConstants.InvariantCulture,
                NetworkConstants.CCPPortraits, charId, (int)EveImageSize.x128);

            return await GetImageAsync(GetImageServerBaseUri(path), false).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously downloads an alliance image.
        /// </summary>
        /// <param name="allianceID">The alliance ID.</param>
        /// <param name="size">The image size in pixels.</param>
        public static async Task<SKBitmap> GetAllianceImageAsync(long allianceID, int size = 128)
        {
            string path = string.Format(CultureConstants.InvariantCulture, NetworkConstants.
                CCPAllianceLogo, allianceID, size);
            return await GetImageAsync(GetImageServerBaseUri(path)).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously downloads a corporation image.
        /// </summary>
        /// <param name="corporationID">The corporation ID.</param>
        /// <param name="size">The image size in pixels.</param>
        public static async Task<SKBitmap> GetCorporationImageAsync(long corporationID, int size = 128)
        {
            string path = string.Format(CultureConstants.InvariantCulture, NetworkConstants.
                CCPCorporationLogo, corporationID, size);
            return await GetImageAsync(GetImageServerBaseUri(path)).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously downloads an image from the provided url.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="useCache">if set to <c>true</c> [use cache].</param>
        public static async Task<SKBitmap> GetImageAsync(Uri url, bool useCache = true)
        {
            DownloadResult<SKBitmap> result;

            // Cache not to be used ?
            if (!useCache)
            {
                result = await HttpWebClientService.DownloadImageAsync(url).ConfigureAwait(false);
                return GetImage(result);
            }

            SKBitmap image = GetImageFromCache(GetCacheName(url));
            if (image != null)
                return image;

            // Downloads the image and adds it to cache
            result = await HttpWebClientService.DownloadImageAsync(url).ConfigureAwait(false);
            image = GetImage(result);

            if (image != null)
                await AddImageToCacheAsync(image, GetCacheName(url)).ConfigureAwait(false);

            return image;
        }

        /// <summary>
        /// Asynchronously gets the character image from cache.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="directory">The directory.</param>
        /// <returns></returns>
        public static SKBitmap GetImageFromCache(string filename, string directory = null)
        {
            // First check whether the image exists in cache
            EveMonClient.EnsureCacheDirInit();
            string cacheFileName = Path.Combine(directory ?? AppServices.ApplicationPaths.ImageCacheDirectory,
                filename);

            if (!File.Exists(cacheFileName))
                return null;

            try
            {
                // Load the data into a MemoryStream before returning the image to avoid file
                // locking
                byte[] imageBytes = File.ReadAllBytes(cacheFileName);
                SKBitmap image = SKBitmap.Decode(imageBytes);
                return image;
            }
            catch (ArgumentException e)
            {
                ExceptionHandler.LogException(e, false);
                FileHelper.DeleteFile(cacheFileName);
            }
            catch (IOException e)
            {
                ExceptionHandler.LogException(e, false);
            }
            catch (UnauthorizedAccessException e)
            {
                ExceptionHandler.LogException(e, false);
            }

            return null;
        }

        /// <summary>
        /// Callback used when images are downloaded.
        /// </summary>
        /// <param name="result">The result.</param>
        private static SKBitmap GetImage(DownloadResult<SKBitmap> result)
        {
            if (result.Error == null)
                return result.Result;

            if (result.Error.Status == HttpWebClientServiceExceptionStatus.Timeout)
                AppServices.TraceService?.Trace(result.Error.Message);
            else
                ExceptionHandler.LogException(result.Error, true);

            return null;
        }

        /// <summary>
        /// Adds the image to the cache.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="filename">The filename.</param>
        /// <param name="directory">The directory.</param>
        /// <returns></returns>
        public static async Task AddImageToCacheAsync(SKBitmap image, string filename,
            string directory = null)
        {
            // Saves the image file
            try
            {
                // Write this image to the cache file
                EveMonClient.EnsureCacheDirInit();
                string cacheFileName = Path.Combine(directory ?? EveMonClient.
                    EVEMonImageCacheDir, filename);
                await FileHelper.OverwriteOrWarnTheUserAsync(cacheFileName,
                    async fs =>
                    {
                        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                        data.SaveTo(fs);
                        await fs.FlushAsync();
                        return true;
                    }).ConfigureAwait(false);
            }
            catch (IOException ex)
            {
                // Anything but "file in use"
                if (ex.HResult != -2147024864)
                    ExceptionHandler.LogException(ex, true);
            }
            catch (Exception ex)
            {
                ExceptionHandler.LogRethrowException(ex);
                throw;
            }
        }

        /// <summary>
        /// From a given url, computes a cache file name.
        /// </summary>
        private static string GetCacheName(Uri url)
        {
            Stream stream = Util.GetMemoryStream(Encoding.UTF8.GetBytes(url.AbsoluteUri));
            string md5Sum = Util.CreateMD5(stream);
            // Extensions are no longer part of the requested URLs
            return string.Concat(md5Sum, ".png");
        }
    }
}
