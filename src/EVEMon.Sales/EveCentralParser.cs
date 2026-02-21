// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Text.RegularExpressions;

namespace EVEMon.Sales
{
    public class EveCentralParser : IMineralParser
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name => "evecentral";

        /// <summary>
        /// Gets the title.
        /// </summary>
        /// <value>The title.</value>
        public string Title => "EVE-Central Mineral Feed";

        /// <summary>
        /// Gets the courtesy URL.
        /// </summary>
        /// <value>The courtesy URL.</value>
        public Uri CourtesyUrl => new Uri("http://eve-central.com");

        /// <summary>
        /// Gets the courtesy text.
        /// </summary>
        /// <value>The courtesy text.</value>
        public string CourtesyText => "Eve-Central";

        /// <summary>
        /// Gets the tokenizer.
        /// </summary>
        /// <value>The tokenizer.</value>
        public Regex Tokenizer => new Regex(@"<name>(?<name>.+?)</name>\s*<price>(?<price>.+?)</price>",
            RegexOptions.Compiled
            | RegexOptions.IgnorePatternWhitespace
            | RegexOptions.Singleline
            | RegexOptions.Multiline
            | RegexOptions.IgnoreCase);

        /// <summary>
        /// Gets the URL.
        /// </summary>
        /// <value>The URL.</value>
        public Uri URL => new Uri("http://api.eve-central.com/api/evemon");
    }
}