// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using EveLens.Common.Loadouts;

namespace EveLens.Common.SettingsObjects
{
    public sealed class LoadoutsProviderSettings
    {
        private static readonly Dictionary<string, LoadoutsProvider> s_loadoutsProviders = new Dictionary<string, LoadoutsProvider>();

        /// <summary>
        /// Initializes a new instance of the <see cref="LoadoutsProviderSettings"/> class.
        /// </summary>
        public LoadoutsProviderSettings()
        {
            foreach (LoadoutsProvider provider in LoadoutsProvider.Providers)
            {
                s_loadoutsProviders[provider.Name] = provider;
            }

            ProviderName = s_loadoutsProviders.FirstOrDefault().Key ?? string.Empty;
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
        public LoadoutsProvider Provider
        {
            get
            {
                if (s_loadoutsProviders.ContainsKey(ProviderName))
                    return s_loadoutsProviders[ProviderName];

                ProviderName = s_loadoutsProviders.FirstOrDefault().Key ?? string.Empty;

                return s_loadoutsProviders.FirstOrDefault().Value;
            }
        }
    }
}
