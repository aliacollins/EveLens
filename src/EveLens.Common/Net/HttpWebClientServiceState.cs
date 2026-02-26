// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EveLens.Common.Services;
using EveLens.Common.SettingsObjects;

namespace EveLens.Common.Net
{
    /// <summary>
    /// Conainer class for HttpWebService settings and state
    /// </summary>
    public static class HttpWebClientServiceState
    {
        private static readonly object s_syncLock = new object();
        private static ProxySettings s_proxy = new ProxySettings();

        /// <summary>
        /// The maximum size of a download section.
        /// </summary>
        public static int MaxBufferSize => 8192;

        /// <summary>
        /// The minimum size if a download section.
        /// </summary>
        public static int MinBufferSize => 1024;

        /// <summary>
        /// The maintainer contact for ESI User-Agent header.
        /// Per ESI best practices, this should be set to allow CCP to contact developers.
        /// Accepted formats: email, discord:username, or eve:charactername
        /// </summary>
        public static string MaintainerContact { get; set; } = "eve:Alia Collins";

        /// <summary>
        /// The source code URL for ESI User-Agent header.
        /// </summary>
        public static string SourceUrl { get; set; } = "https://github.com/aliacollins/evelens";

        /// <summary>
        /// The user agent string for requests.
        /// Follows ESI best practices: https://developers.eveonline.com/docs/best-practices/
        /// Format: AppName/Version (email; +sourceUrl) (OS; arch)
        /// </summary>
        public static string UserAgent
        {
            get
            {
                var architecture = Environment.Is64BitOperatingSystem
                    ? "x64"
                    : "x86";
                var productName = AppServices.FileVersionInfo.ProductName;
                var version = AppServices.FileVersionInfo.FileVersion;

                // Build user agent per ESI best practices
                // Format: AppName/Version (contact info) (OS info)
                return $"{productName}/{version} ({MaintainerContact}; +{SourceUrl}) ({Environment.OSVersion.VersionString}; {architecture})";
            }
        }

        /// <summary>
        /// The maximum redirects allowed for a request.
        /// </summary>
        public static int MaxRedirects => 5;

        /// <summary>
        /// A ProxySetting instance for the custom proxy to be used.
        /// </summary>
        public static ProxySettings Proxy
        {
            get
            {
                lock (s_syncLock)
                {
                    return s_proxy;
                }
            }
            set
            {
                lock (s_syncLock)
                {
                    s_proxy = value;
                }
            }
        }
    }
}