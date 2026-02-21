// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EVEMon.Common.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.XPath;

namespace EVEMon.Common.MarketPricer
{
    public abstract class ItemPricer
    {
        protected static readonly Dictionary<int, double> PriceByItemID = new Dictionary<int, double>();

        protected static string SelectedProviderName;
        protected static bool Loaded;

        /// <summary>
        /// Gets the name.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="ItemPricer"/> is enabled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if enabled; otherwise, <c>false</c>.
        /// </value>
        protected abstract bool Enabled { get; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="ItemPricer"/> is queried.
        /// </summary>
        /// <value>
        ///   <c>true</c> if queried; otherwise, <c>false</c>.
        /// </value>
        public bool Queried => Loaded;

        /// <summary>
        /// Gets the providers.
        /// </summary>
        /// <value>
        /// The providers.
        /// </value>
        public static IEnumerable<ItemPricer> Providers => Assembly.GetExecutingAssembly().
            GetTypes().Where(type => typeof(ItemPricer).IsAssignableFrom(type) && type.
            GetConstructor(Type.EmptyTypes) != null).Select(type => Activator.CreateInstance(
            type) as ItemPricer).Where(provider => !string.IsNullOrWhiteSpace(provider.Name) &&
            provider.Enabled).OrderBy(pricer => pricer.Name);

        /// <summary>
        /// Gets the price by type ID.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns></returns>
        public abstract double GetPriceByTypeID(int id);
 
        /// <summary>
        /// Saves the xml document to the specified filename.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="xdoc">The xdoc.</param>
        protected static Task SaveAsync(string filename, IXPathNavigable xdoc)
            => LocalXmlCache.SaveAsync(filename, xdoc);
    }
}
