// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using System.Linq;
using EVEMon.Common.Data;
using EVEMon.Common.Models;

namespace EVEMon.Common.Extensions
{
    public static class SkillEnumerableExtensions
    {
        /// <summary>
        /// Rreturns an enumeration of static equivalent of those character skills.
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static IEnumerable<StaticSkill> ToStatic(this IEnumerable<Skill> src) => src.Select(item => item.StaticData);

        /// <summary>
        /// Gets all the prerequisites. I.e, for eidetic memory, it will return <c>{ instant recall IV }</c>. 
        /// The order matches the hirerarchy but skills are not duplicated and are systematically trained to the highest required level.
        /// For example, if some skill is required to lv3 and, later, to lv4, this first time it is encountered, lv4 is returned.
        /// </summary>
        /// <remarks>Please note they may be redundancies.</remarks>
        public static IEnumerable<SkillLevel> GetAllPrerequisites(this IEnumerable<Skill> src)
        {
            var enumerable = src as IList<Skill> ?? src.ToList();
            Skill first = enumerable.FirstOrDefault();
            return first == null
                ? Enumerable.Empty<SkillLevel>()
                : enumerable.ToStatic().GetAllPrerequisites().ToCharacter(first.Character);
        }
    }
}