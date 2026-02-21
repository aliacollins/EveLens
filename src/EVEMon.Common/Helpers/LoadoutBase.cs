// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using EVEMon.Common.Data;

namespace EVEMon.Common.Helpers
{
    public abstract class LoadoutBase
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public long ID { get; internal set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name of the loadout.
        /// </value>
        public string Name { get; internal set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        /// <value>
        /// The loadout description.
        /// </value>
        public string Description { get; internal set; }

        /// <summary>
        /// Gets or sets the author.
        /// </summary>
        /// <value>
        /// The author.
        /// </value>
        public string Author { get; internal set; }

        /// <summary>
        /// Gets or sets the rating.
        /// </summary>
        /// <value>
        /// The rating.
        /// </value>
        public double Rating { get; internal set; }

        /// <summary>
        /// Gets or sets the submission date.
        /// </summary>
        /// <value>
        /// The submission date.
        /// </value>
        public DateTimeOffset SubmissionDate { get; internal set; }

        /// <summary>
        /// Gets the topic URL.
        /// </summary>
        /// <value>
        /// The topic URL.
        /// </value>
        public Uri TopicUrl { get; internal set; }

        /// <summary>
        /// Gets or sets the items.
        /// </summary>
        /// <value>
        /// The items.
        /// </value>
        public IEnumerable<Item> Items { get; protected internal set; }
    }
}