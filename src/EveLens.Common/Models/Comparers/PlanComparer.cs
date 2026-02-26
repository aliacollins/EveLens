// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using EveLens.Common.Enumerations;

namespace EveLens.Common.Models.Comparers
{
    /// <summary>
    /// Implements a plan comparer.
    /// </summary>
    public sealed class PlanComparer : Comparer<Plan>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="sort"></param>
        public PlanComparer(PlanSort sort)
        {
            Sort = sort;
        }

        /// <summary>
        /// Sort order (ascending, descending).
        /// </summary>
        public SortOrder Order { get; set; }

        /// <summary>
        /// Sort criteria.
        /// </summary>
        public PlanSort Sort { get; set; }

        /// <summary>
        /// Comparison function.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public override int Compare(Plan? x, Plan? y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            // Swap variables for descending order
            if (Order == SortOrder.Descending)
            {
                Plan tmp = y;
                y = x;
                x = tmp;
            }

            // Compare plans
            switch (Sort)
            {
                case PlanSort.Description:
                    return string.Compare(x.Description, y.Description,
                        StringComparison.CurrentCulture);
                case PlanSort.Name:
                    return string.Compare(x.Name, y.Name, StringComparison.CurrentCulture);
                case PlanSort.Time:
                    {
                        TimeSpan xtime = x.TotalTrainingTime;
                        TimeSpan ytime = y.TotalTrainingTime;
                        return TimeSpan.Compare(xtime, ytime);
                    }
                case PlanSort.SkillsCount:
                    return x.UniqueSkillsCount - y.UniqueSkillsCount;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
