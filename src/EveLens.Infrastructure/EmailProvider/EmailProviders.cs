// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EveLens.Common.EmailProvider
{
    public static class EmailProviders
    {
        private static readonly Dictionary<string, IEmailProvider> s_emailProviders = new Dictionary<string, IEmailProvider>();

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        public static void Initialize()
        {
            foreach (IEmailProvider provider in Assembly.GetExecutingAssembly().GetTypes().Where(
                type => typeof(IEmailProvider).IsAssignableFrom(type) && type.GetConstructor(Type.EmptyTypes) != null).Select(
                    type => Activator.CreateInstance(type) as IEmailProvider).OrderBy(provider => provider.Name))
            {
                s_emailProviders[provider.Name] = provider;
            }
        }

        /// <summary>
        /// Gets the providers.
        /// </summary>
        /// <value>The providers.</value>
        public static IEnumerable<IEmailProvider> Providers => s_emailProviders.Values;

        /// <summary>
        /// Gets the value by the provided key.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public static IEmailProvider GetByKey(string name)
        {
            IEmailProvider provider;
            s_emailProviders.TryGetValue(name, out provider);
            return provider;
        }
    }
}
