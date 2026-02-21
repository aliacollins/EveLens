// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace EVEMon.Common.Net
{
    /// <summary>
    /// Provides HTTP client services for EVEMon network requests.
    /// </summary>
    /// <remarks>
    /// <para>Architecture Decision: HttpClient Management</para>
    /// <para>
    /// This implementation uses a singleton HttpClient with SocketsHttpHandler for .NET 8+.
    /// While IHttpClientFactory is the recommended pattern for ASP.NET Core services, it was
    /// evaluated and determined to be unnecessary for this desktop application because:
    /// </para>
    /// <list type="bullet">
    /// <item>IHttpClientFactory requires Microsoft.Extensions.Http and DI container</item>
    /// <item>Adding DI to a WinForms app would require significant architectural changes</item>
    /// <item>SocketsHttpHandler with PooledConnectionLifetime already handles DNS rotation</item>
    /// <item>Connection pooling (MaxConnectionsPerServer=20) prevents socket exhaustion</item>
    /// <item>The singleton pattern is appropriate for a single-process desktop application</item>
    /// </list>
    /// <para>
    /// If EVEMon is ever refactored to use dependency injection, consider migrating to
    /// IHttpClientFactory with named clients for ESI and other services.
    /// </para>
    /// </remarks>
    public static partial class HttpWebClientService
    {
        // Shared HttpClient instance to prevent socket exhaustion in .NET 8
        // See: https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines
        private static HttpClient s_sharedClient;
        private static readonly object s_clientLock = new object();

        /// <summary>
        /// Gets or creates the shared HttpClient with current proxy settings.
        /// </summary>
        private static HttpClient GetOrCreateSharedClient()
        {
            if (s_sharedClient == null)
            {
                lock (s_clientLock)
                {
                    if (s_sharedClient == null)
                    {
                        s_sharedClient = CreateHttpClient();
                    }
                }
            }
            return s_sharedClient;
        }

        /// <summary>
        /// Creates a new HttpClient with the current proxy settings.
        /// </summary>
        private static HttpClient CreateHttpClient()
        {
            var handler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),
                MaxConnectionsPerServer = 20,
                AllowAutoRedirect = false,
                Proxy = HttpClientServiceRequest.GetWebProxy(),
                UseProxy = true
            };
            var client = new HttpClient(handler);
            // Set a reasonable default timeout - can't be changed per-request on shared client
            client.Timeout = TimeSpan.FromSeconds(60);
            return client;
        }

        /// <summary>
        /// Resets the shared HttpClient (call when proxy settings change).
        /// </summary>
        public static void ResetSharedClient()
        {
            lock (s_clientLock)
            {
                var oldClient = s_sharedClient;
                s_sharedClient = null;
                // Don't dispose immediately - let pending requests finish
                // The old client will be garbage collected
            }
        }

        /// <summary>
        /// Initializes the <see cref="HttpWebClientService"/> class.
        /// </summary>
        /// <remarks>
        /// In .NET 8, ServicePointManager is ignored when using SocketsHttpHandler.
        /// Connection limits and 100-Continue behavior are now configured directly on
        /// the SocketsHttpHandler in CreateHttpClient().
        /// </remarks>
        static HttpWebClientService()
        {
            // Note: ServicePointManager settings removed as they have no effect with SocketsHttpHandler
            // in .NET 8. Connection pooling is now configured in CreateHttpClient().
        }

        /// <summary>
        /// Gets the web client.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// WebClient is deprecated in .NET 8 (SYSLIB0014) but still functional.
        /// Used for download progress reporting in UpdateDownloadForm and DataUpdateDownloadForm.
        /// TODO: Migrate to HttpClient with IProgress&lt;T&gt; for download progress reporting.
        /// </remarks>
#pragma warning disable SYSLIB0014 // WebClient is obsolete but still needed for progress reporting
        public static WebClient GetWebClient() => new WebClient
        {
            Proxy = HttpClientServiceRequest.GetWebProxy()
        };
#pragma warning restore SYSLIB0014

        /// <summary>
        /// Gets the shared HTTP client instance.
        /// </summary>
        /// <returns>The shared HttpClient configured with proxy settings.</returns>
        public static HttpClient GetHttpClient()
        {
            return GetOrCreateSharedClient();
        }

        /// <summary>
        /// Validates a Url as acceptable for an HttpWebServiceRequest.
        /// </summary>
        /// <param name="url">A url <see cref="string"/> for the request. The string must specify HTTP or HTTPS as its scheme.</param>
        /// <param name="errorMsg">Is url is invalid, contains a descriptive message of the reason</param>
        public static bool IsValidURL(Uri url, out string errorMsg)
        {
            if (string.IsNullOrWhiteSpace(url.AbsoluteUri))
            {
                errorMsg = "Url may not be null or an empty string.";
                return false;
            }

            if (!Uri.IsWellFormedUriString(url.AbsoluteUri, UriKind.Absolute))
            {
                errorMsg = $"\"{url}\" is not a well-formed URL.";

                return false;
            }

            try
            {
                if (url.Scheme != Uri.UriSchemeHttp && url.Scheme != Uri.UriSchemeHttps)
                {
                    errorMsg = $"The specified scheme ({url.Scheme}) is not supported.";

                    return false;
                }
            }
            catch (UriFormatException)
            {
                errorMsg = $"\"{url}\" is not a valid URL for an HTTP or HTTPS request.";

                return false;
            }

            errorMsg = string.Empty;
            return true;
        }
    }
}
