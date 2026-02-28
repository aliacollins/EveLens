// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using EveLens.Common.CloudStorageServices;

namespace EveLens.Common.SettingsObjects
{

    public sealed class CloudStorageServiceProviderSettings
    {
        private static Dictionary<string, CloudStorageServiceProvider>? s_cloudStorageServiceProviders;

        private static Dictionary<string, CloudStorageServiceProvider> Providers
        {
            get
            {
                if (s_cloudStorageServiceProviders == null)
                {
                    s_cloudStorageServiceProviders = new Dictionary<string, CloudStorageServiceProvider>();
                    try
                    {
                        foreach (CloudStorageServiceProvider provider in CloudStorageServiceProvider.Providers)
                            s_cloudStorageServiceProviders[provider.Name] = provider;
                    }
                    catch
                    {
                        // System.Drawing.Common not available on Linux/macOS — providers use it for logos.
                        // Cloud storage is not active in the Avalonia UI, so this is safe to skip.
                    }
                }
                return s_cloudStorageServiceProviders;
            }
        }

        public CloudStorageServiceProviderSettings()
        {
            ProviderName = string.Empty;
        }

        /// <summary>
        /// Gets or sets the provider name.
        /// </summary>
        /// <value>
        /// The name of the provider.
        /// </value>
        [XmlAttribute("provider")]
        public string ProviderName { get; set; }

        /// <summary>
        /// Gets the provider.
        /// </summary>
        /// <value>
        /// The provider.
        /// </value>
        [XmlIgnore]
        [JsonIgnore]
        public CloudStorageServiceProvider Provider
        {
            get
            {
                var providers = Providers;
                if (providers.Count == 0)
                    return null!;

                if (providers.ContainsKey(ProviderName))
                    return providers[ProviderName];

                ProviderName = providers.FirstOrDefault().Key ?? string.Empty;
                return providers.FirstOrDefault().Value;
            }
        }
    }
}
