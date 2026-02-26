// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using EveLens.Common.Data;
using EveLens.Common.Enumerations;

namespace EveLens.Common.Helpers
{
    public sealed class PathFinder
    {
        private readonly PathFinder m_parent;
        private readonly SolarSystem m_system;

        /// <summary>
        /// Constructor for a starting point
        /// </summary>
        /// <param name="system"></param>
        private PathFinder(SolarSystem system)
        {
            m_parent = null;
            m_system = system;
        }

        /// <summary>
        /// Constructor for a child.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="system"></param>
        private PathFinder(PathFinder parent, SolarSystem system)
        {
            m_parent = parent;
            m_system = system;
        }

        /// <summary>
        /// Gets the best possible path (approximate solution) using a A* alogrithm.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="target"></param>
        /// <param name="criteria"></param>
        /// <param name="minSecurityLevel">The minimum, inclusive, security level.</param>
        /// <param name="maxSecurityLevel">The maximum, inclusive, security level.</param>
        /// <returns></returns>
        public static IEnumerable<SolarSystem> FindBestPath(SolarSystem start, SolarSystem target, PathSearchCriteria criteria,
            float minSecurityLevel, float maxSecurityLevel)
        {
            PathFinder result = FindBestPathCore(start, target, criteria, minSecurityLevel, maxSecurityLevel);

            // Transforms the result into a list
            List<SolarSystem> list = new List<SolarSystem>();
            for (PathFinder item = result; item != null; item = item.m_parent)
            {
                list.Insert(0, item.m_system);
            }

            return list;
        }

        /// <summary>
        /// Gets the best possible path (approximate solution) using a A* alogrithm.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="target"></param>
        /// <param name="criteria"></param>
        /// <param name="minSecurityLevel">The minimum, inclusive, security level.</param>
        /// <param name="maxSecurityLevel">The maximum, inclusive, security level.</param>
        /// <returns></returns>
        private static PathFinder FindBestPathCore(SolarSystem start, SolarSystem target, PathSearchCriteria criteria,
            float minSecurityLevel, float maxSecurityLevel)
        {
            Dictionary<SolarSystem, int> bestDepths = new Dictionary<SolarSystem, int>();
            SortedList<int, PathFinder> paths = new SortedList<int, PathFinder>();
            PathFinder root = new PathFinder(start);
            bestDepths.Add(start, -1);
            paths.Add(0, root);

            while (true)
            {
                if (paths.Count == 0)
                    return null;

                PathFinder best;
                int depth;

                // Pick the best candidate path, but ensures it matches the best depth found so far
                while (true)
                {
                    // Picks the best one, returns if we found our target
                    best = paths.Values[0];

                    if (best.m_system == target)
                        return best;

                    // Removes it from the list
                    paths.RemoveAt(0);

                    int bestDepth;
                    depth = best.Depth;
                    bestDepths.TryGetValue(best.m_system, out bestDepth);

                    if (bestDepth == depth || best.m_system == start)
                        break;
                }

                // Gets the subpaths for the best path so far
                depth++;
                foreach (PathFinder child in best.GetChildren(depth, bestDepths))
                {
                    // Gets the heuristic key based on path search criteria
                    int criteriaValue = criteria == PathSearchCriteria.ShortestDistance
                        ? child.m_system.GetSquareDistanceWith(target)
                        : 1;
                    int key = criteriaValue * depth * depth;
                    if (child.m_system.SecurityLevel < minSecurityLevel || child.m_system.SecurityLevel > maxSecurityLevel)
                        key *= 100;

                    while (paths.ContainsKey(key))
                    {
                        key++;
                    }

                    // Stores it in the sorted list
                    paths.Add(key, child);
                }
            }
        }

        /// <summary>
        /// Gets all the immediate children paths.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<PathFinder> GetChildren(int depth, IDictionary<SolarSystem, int> bestDepths)
        {
            // Check this system is not already present with a lesser range
            foreach (SolarSystem neighbor in m_system.Neighbors)
            {
                // Checks the best depth so far
                int bestDepth;
                bestDepths.TryGetValue(neighbor, out bestDepth);

                // Enumerates it
                if (bestDepth != 0 && depth >= bestDepth)
                    continue;

                bestDepths[neighbor] = depth;
                yield return new PathFinder(this, neighbor);
            }
        }

        /// <summary>
        /// Gets the depth of this node.
        /// </summary>
        private int Depth
        {
            get
            {
                int count = 0;
                for (PathFinder parent = m_parent; parent != null; parent = parent.m_parent)
                {
                    count++;
                }
                return count;
            }
        }

        /// <summary>
        /// Gets the name of the solar system.
        /// </summary>
        public override string ToString() => m_system.ToString();
    }
}