// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Linq;
using EVEMon.Common.Enumerations;
using EVEMon.Common.Interfaces;
using EVEMon.Common.Models;

namespace EVEMon.Common.Helpers
{
    /// <summary>
    /// Helper for plan operations.
    /// </summary>
    public static class PlanHelper
    {
        /// <summary>
        /// Checks whether the given operation absolutely requires a confirmation from the user.
        /// True when there are dependencies to remove.
        /// </summary>
        /// <param name="operation">The operation.</param>
        /// <returns></returns>
        public static bool RequiresWindow(IPlanOperation operation)
        {
            if (operation?.Type != PlanOperations.Suppression)
                return false;

            return operation.SkillsToRemove.Count() != operation.AllEntriesToRemove.Count();
        }
    }
}
