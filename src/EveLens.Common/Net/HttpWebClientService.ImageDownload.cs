// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using EveLens.Common.Extensions;
using SkiaSharp;

namespace EveLens.Common.Net
{
    static partial class HttpWebClientService
    {
        private const string ImageAccept = "image/*,*/*;q=0.5";

        /// <summary>
        /// Asynchronously downloads an image from the specified url.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="param">The request parameters. If null, defaults will be used.</param>
        public static async Task<DownloadResult<SKBitmap>> DownloadImageAsync(Uri url,
            RequestParams param = null)
        {
            string urlValidationError;
            if (!IsValidURL(url, out urlValidationError))
                throw new ArgumentException(urlValidationError);
            var request = new HttpClientServiceRequest();
            try
            {
                var response = await request.SendAsync(url, param, ImageAccept).
                    ConfigureAwait(false);
                using (response)
                {
                    Stream stream = await response.Content.ReadAsStreamAsync().
                        ConfigureAwait(false);
                    return GetImage(request.BaseUrl, stream, response);
                }
            }
            catch (HttpWebClientServiceException ex)
            {
                return new DownloadResult<SKBitmap>(null, ex);
            }
        }

        /// <summary>
        /// Gets the result.
        /// </summary>
        /// <param name="requestBaseUrl">The request base URL.</param>
        /// <param name="stream">The stream.</param>
        /// <param name="response">The response from the server.</param>
        /// <returns></returns>
        private static DownloadResult<SKBitmap> GetImage(Uri requestBaseUrl, Stream stream,
            HttpResponseMessage response)
        {
            SKBitmap image = null;
            HttpWebClientServiceException error = null;
            var param = new ResponseParams(response);
            if (stream == null)
            {
                error = HttpWebClientServiceException.Exception(requestBaseUrl,
                    new ArgumentNullException(nameof(stream)));
                return new DownloadResult<SKBitmap>(null, error, param);
            }
            try
            {
                image = SKBitmap.Decode(Util.ZlibUncompress(stream));
            }
            catch (ArgumentException ex)
            {
                error = HttpWebClientServiceException.ImageException(requestBaseUrl, ex);
            }
            return new DownloadResult<SKBitmap>(image, error, param);
        }
    }
}
