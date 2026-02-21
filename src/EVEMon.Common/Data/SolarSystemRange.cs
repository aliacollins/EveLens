// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using System.Linq;
using EVEMon.Common.Collections;

namespace EVEMon.Common.Data
{
    /// <summary>
    /// Represents the collections of solar systems at a given number of jumps of a certain solar system.
    /// </summary>
    public sealed class SolarSystemRange : ReadonlyCollection<SolarSystem>
    {
        # region Private constructor

        /// <summary>
        /// Private constructor.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="range">The range.</param>
        private SolarSystemRange(SolarSystem source, int range)
            : base(1)
        {
            Source = source;
            Range = range;
        }

        #endregion


        # region Public Properties

        /// <summary>
        /// Gets the source solar system.
        /// </summary>
        public SolarSystem Source { get; }

        /// <summary>
        /// The number of jumps those system are located from the source.
        /// </summary>
        public int Range { get; }

        #endregion


        /// <summary>
        /// Gets a list of solar systems within the specified range of the specified solar system.
        /// </summary>
        /// <param name="system">The system.</param>
        /// <param name="maxInclusiveNumberOfJumps">The maximum, inclusive, number of jumps.</param>
        /// <returns></returns>
        internal static IEnumerable<SolarSystemRange> GetSystemRangesFrom(SolarSystem system, int maxInclusiveNumberOfJumps)
        {
            Dictionary<long, SolarSystem> collectedSystems = new Dictionary<long, SolarSystem>();
            List<SolarSystemRange> ranges = new List<SolarSystemRange>();
            SolarSystemRange lastRange = new SolarSystemRange(system, 0);

            collectedSystems.Add(system.ID, system);
            lastRange.Items.Add(system);

            for (int i = 1; i <= maxInclusiveNumberOfJumps; i++)
            {
                lastRange = lastRange.GetNextRange(collectedSystems);
                ranges.Add(lastRange);
            }

            return ranges;
        }

        /// <summary>
        /// Gets the next solar system range.
        /// </summary>
        /// <returns></returns>
        private SolarSystemRange GetNextRange(IDictionary<long, SolarSystem> collectedSystems)
        {
            SolarSystemRange nextRange = new SolarSystemRange(Source, Range + 1);

            foreach (SolarSystem child in Items.SelectMany(system => system.Neighbors.Where(
                child => !collectedSystems.ContainsKey(child.ID))))
            {
                collectedSystems.Add(child.ID, child);
                nextRange.Items.Add(child);
            }

            return nextRange;
        }
    }
}