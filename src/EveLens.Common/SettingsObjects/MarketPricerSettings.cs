// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using EveLens.Common.MarketPricer;
using EveLens.Common.Services;

namespace EveLens.Common.SettingsObjects
{
    public sealed class MarketPricerSettings
    {
        private static readonly Dictionary<string, ItemPricer> s_pricer = new Dictionary<string, ItemPricer>();

        /// <summary>
        /// Initializes a new instance of the <see cref="MarketPricerSettings"/> class.
        /// </summary>
        public MarketPricerSettings()
        {
            try
            {
                foreach (ItemPricer pricer in ItemPricer.Providers)
                {
                    s_pricer[pricer.Name] = pricer;
                }

                // Default to zKillboard if available, otherwise first alphabetically
                ProviderName = s_pricer.ContainsKey("zKillboard")
                    ? "zKillboard"
                    : s_pricer.FirstOrDefault().Key ?? string.Empty;
            }
            catch (System.Reflection.ReflectionTypeLoadException e)
            {
                // Dump the loader exceptions for more debug information
                AppServices.TraceService?.Trace("Error loading market price providers:");
                foreach (var exception in e.LoaderExceptions)
                    if (exception != null)
                        AppServices.TraceService?.Trace(exception.ToString(), false);
            }
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
        /// Gets the pricer.
        /// </summary>
        /// <value>
        /// The pricer.
        /// </value>
        [XmlIgnore]
        public ItemPricer Pricer
        {
            get
            {
                if (s_pricer.ContainsKey(ProviderName))
                    return s_pricer[ProviderName];

                ProviderName = s_pricer.FirstOrDefault().Key ?? string.Empty;

                return s_pricer.FirstOrDefault().Value;
            }
        }
    }
}
