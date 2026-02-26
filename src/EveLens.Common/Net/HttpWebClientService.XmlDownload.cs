// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.XPath;
using EveLens.Common.Extensions;

namespace EveLens.Common.Net
{
    static partial class HttpWebClientService
    {
        private const string XmlAccept = "text/xml,application/xml,application/xhtml+xml;q=0.8,*/*;q=0.5";

        /// <summary>
        /// Asynchronously downloads an xml file from the specified url.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="param">The request parameters. If null, defaults will be used.</param>
        public static async Task<DownloadResult<IXPathNavigable>> DownloadXmlAsync(Uri url,
            RequestParams? param = null)
        {
            string urlValidationError;
            if (!IsValidURL(url, out urlValidationError))
                throw new ArgumentException(urlValidationError);
            var request = new HttpClientServiceRequest();
            try
            {
                var response = await request.SendAsync(url, param, XmlAccept).
                    ConfigureAwait(false);
                using (response)
                {
                    Stream stream = await response.Content.ReadAsStreamAsync().
                        ConfigureAwait(false);
                    return GetXmlDocument(request.BaseUrl, stream, response);
                }
            }
            catch (HttpWebClientServiceException ex)
            {
                return new DownloadResult<IXPathNavigable>(new XmlDocument(), ex);
            }
        }

        /// <summary>
        /// Helper method to return an Xml document from the completed request.
        /// </summary>
        /// <param name="requestBaseUrl">The request base URL.</param>
        /// <param name="stream">The stream.</param>
        /// <param name="response">The response from the server.</param>
        private static DownloadResult<IXPathNavigable> GetXmlDocument(Uri requestBaseUrl,
            Stream stream, HttpResponseMessage response)
        {
            XmlDocument xmlDoc = new XmlDocument();
            HttpWebClientServiceException error = null;
            var param = new ResponseParams(response);
            if (stream == null)
            {
                error = HttpWebClientServiceException.Exception(requestBaseUrl,
                    new ArgumentNullException(nameof(stream)));
                return new DownloadResult<IXPathNavigable>(xmlDoc, error, param);
            }
            try
            {
                xmlDoc.Load(Util.ZlibUncompress(stream));
            }
            catch (XmlException ex)
            {
                error = HttpWebClientServiceException.XmlException(requestBaseUrl, ex);
            }
            return new DownloadResult<IXPathNavigable>(xmlDoc, error, param);
        }
    }
}
