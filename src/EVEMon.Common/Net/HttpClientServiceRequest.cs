// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using EVEMon.Common.Constants;
using EVEMon.Common.Enumerations;
using EVEMon.Common.Models;
using EVEMon.Common.Services;
using EVEMon.Common.SettingsObjects;
using Microsoft.Extensions.Logging;

namespace EVEMon.Common.Net
{
    internal class HttpClientServiceRequest
    {
        private static readonly Lazy<ILogger?> s_logger = new(() =>
            AppServices.LoggerFactory?.CreateLogger<HttpClientServiceRequest>());
        private static TimeSpan s_timeout;
        private Uri m_url;
        private Uri m_referrer;
        private string m_accept;
        private int m_redirectsRemaining;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpClientServiceRequest"/> class.
        /// </summary>
        public HttpClientServiceRequest()
        {
            m_redirectsRemaining = HttpWebClientServiceState.MaxRedirects;

            // Pull the timeout from the settings
            TimeSpan timeoutSetting = TimeSpan.FromSeconds(Settings.Updates.HttpTimeout);
            s_timeout = (timeoutSetting < TimeSpan.FromSeconds(1) || timeoutSetting >
                TimeSpan.FromMinutes(5)) ? TimeSpan.FromSeconds(20) : timeoutSetting;
        }

        /// <summary>
        /// Gets the base URL.
        /// </summary>
        /// <value>
        /// The base URL.
        /// </value>
        public Uri BaseUrl => m_url;

        /// <summary>
        /// Asynchronously sends a request to the specified url.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="param">The request parameters.</param>
        /// <param name="accept">The content types to accept.</param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> SendAsync(Uri url, RequestParams param,
            string accept)
        {
            var newParams = param ?? new RequestParams();
            while (true)
            {
                // Store params
                m_url = url;
                m_accept = accept;
                HttpResponseMessage response = null;
                try
                {
                    var request = GetHttpRequest(newParams);
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    response = await GetHttpResponseAsync(request).ConfigureAwait(false);
                    sw.Stop();
                    s_logger.Value?.LogInformation(new EventId(3, "ESI"),
                        "{Method} {Path} → {StatusCode} {Reason} ({ElapsedMs}ms)",
                        request.Method, url.AbsolutePath, (int)response.StatusCode,
                        response.ReasonPhrase, sw.ElapsedMilliseconds);
                    EnsureSuccessStatusCode(response);
                }
                catch (HttpWebClientServiceException)
                {
                    // Seems pointless but prevents the exception from getting wrapped again
                    throw;
                }
                catch (HttpRequestException ex)
                {
                    // Strip a layer of exceptions if a web exception occurred
                    if (ex.InnerException is WebException)
                        throw HttpWebClientServiceException.HttpWebClientException(url,
                            ex.InnerException);
                    // Throw default exception if no response
                    if (response == null)
                        throw HttpWebClientServiceException.Exception(url, ex);
                    // Throw for 404, 500, etc.
                    if (response.StatusCode != HttpStatusCode.Redirect && response.
                            StatusCode != HttpStatusCode.MovedPermanently)
                        throw HttpWebClientServiceException.HttpWebClientException(url, ex,
                            response.StatusCode);
                }
                catch (TaskCanceledException ex)
                {
                    // We throw a request timeout if the task gets cancelled due to the
                    // timeout setting
                    throw HttpWebClientServiceException.HttpWebClientException(url,
                        new HttpRequestException(ex.Message), HttpStatusCode.RequestTimeout);
                }
                catch (Exception ex)
                {
                    throw HttpWebClientServiceException.Exception(url, ex);
                }
                if (response.StatusCode != HttpStatusCode.Redirect && response.StatusCode !=
                        HttpStatusCode.MovedPermanently)
                    return response;

                // When the address has been redirected, connects to the redirection
                Uri target = response.Headers.Location;
                response.Dispose();
                if (m_redirectsRemaining-- <= 0)
                    throw HttpWebClientServiceException.RedirectsExceededException(m_url);
                m_referrer = m_url;
                m_url = new Uri(m_url, target);
                url = m_url;
            }
        }

        private static void EnsureSuccessStatusCode(HttpResponseMessage response)
        {
            var code = response.StatusCode;
            if ((int)code < 100)
            {
                response.StatusCode = HttpStatusCode.OK;
                response.ReasonPhrase = "OK";
            } else if (code != HttpStatusCode.NotModified)
                // Allow "not modified" so that it will be detected by the front end
                response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Gets the web proxy.
        /// </summary>
        /// <returns></returns>
        public static IWebProxy GetWebProxy()
        {
            var proxy = HttpWebClientServiceState.Proxy;
            if (!proxy.Enabled)
                return WebRequest.DefaultWebProxy;
            // If proxy is enabled but no host specified, use empty proxy (bypass all)
            if (string.IsNullOrEmpty(proxy.Host))
                return new WebProxy();

            var wp = new WebProxy(proxy.Host, proxy.Port);
            switch (proxy.Authentication)
            {
                case ProxyAuthentication.None:
                    wp.UseDefaultCredentials = false;
                    wp.Credentials = null;
                    break;
                case ProxyAuthentication.SystemDefault:
                    wp.UseDefaultCredentials = true;
                    break;
                case ProxyAuthentication.Specified:
                    wp.UseDefaultCredentials = false;
                    wp.Credentials = new NetworkCredential(proxy.Username, Util.Decrypt(
                        proxy.Password, proxy.Username));
                    break;
            }
            return wp;
        }

        /// <summary>
        /// Gets the HTTP response.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        private static async Task<HttpResponseMessage> GetHttpResponseAsync(
            HttpRequestMessage request)
        {
            // Don't dispose the client - it's a shared instance in .NET 8
            var client = HttpWebClientService.GetHttpClient();
            using (var cts = new System.Threading.CancellationTokenSource(s_timeout))
            {
                return await client.SendAsync(request, cts.Token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets the HTTP request.
        /// </summary>
        /// <param name="token">If not null, adds the specified ESI token to the headers.</param>
        /// <param name="dataContentType">The content type of the input data.</param>
        /// <returns>The HTTP request.</returns>
        private HttpRequestMessage GetHttpRequest(RequestParams param)
        {
            var method = param.MethodChecked;
            // GET has content in the URL
            if (method == HttpMethod.Get && param.Content != null)
                m_url = new Uri(m_url.AbsoluteUri + param.Content);
            var request = new HttpRequestMessage
            {
                RequestUri = m_url,
                Method = method,
            };
            var headers = request.Headers;
            // Authorization
            var token = param.AuthHeader;
            if (token != null)
                headers.Authorization = token;
            // Accept type, character set, language
            headers.Accept.ParseAdd(m_accept);
            headers.AcceptCharset.TryParseAdd("ISO-8859-1,utf-8;q=0.8,*;q=0.7");
            headers.AcceptLanguage.TryParseAdd("en-us,en;q=0.5");
            headers.Host = m_url.Host;
            // E-Tag (must have quotes)
            if (!string.IsNullOrWhiteSpace(param.ETag))
                headers.IfNoneMatch.Add(new EntityTagHeaderValue(param.ETag, false));
            // Expiration
            if (param.IfModifiedSince != null)
                headers.IfModifiedSince = param.IfModifiedSince;
            headers.Pragma.TryParseAdd("no-cache");
            headers.UserAgent.TryParseAdd(HttpWebClientServiceState.UserAgent);
            // Encoding support
            if (param.AcceptEncoded)
                headers.AcceptEncoding.ParseAdd("gzip,deflate;q=0.8");
            // Referrer from previous URL
            if (m_referrer != null)
                headers.Referrer = m_referrer;
            var postData = param.GetEncodedContent();
            if (postData != null && method != HttpMethod.Get)
            {
                // Data with encoding
                string dataContentType = param.ContentType;
                var content = new ByteArrayContent(postData.ToArray());
                if (!string.IsNullOrEmpty(dataContentType))
                    content.Headers.ContentType = MediaTypeHeaderValue.Parse(dataContentType);
                // If we are going to send a compressed request set the appropriate header
                var compression = param.Compression;
                if (compression != DataCompression.None)
                    content.Headers.ContentEncoding.Add(compression.ToString().
                        ToLowerInvariant());
                request.Content = content;
            }
            return request;
        }
    }
}
