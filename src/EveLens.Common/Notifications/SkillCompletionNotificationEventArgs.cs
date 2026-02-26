// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using EveLens.Common.Extensions;
using EveLens.Common.Models;

namespace EveLens.Common.Notifications
{
    public sealed class SkillCompletionNotificationEventArgs : NotificationEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SkillCompletionNotificationEventArgs"/> class.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="skills">The skills.</param>
        /// <exception cref="System.ArgumentNullException">skills</exception>
        public SkillCompletionNotificationEventArgs(object sender, IEnumerable<QueuedSkill> skills)
            : base(sender, NotificationCategory.SkillCompletion)
        {
            skills.ThrowIfNull(nameof(skills));

            Skills = new Collection<QueuedSkill>();
            foreach (QueuedSkill skill in skills)
                Skills.Add(skill);
            UpdateDescription();
        }

        /// <summary>
        /// Gets the associated API result.
        /// </summary>
        public Collection<QueuedSkill> Skills { get; }

        /// <summary>
        /// Gets true if the notification has details.
        /// </summary>
        public override bool HasDetails => Skills.Count != 1;

        /// <summary>
        /// Enqueue the skills from the given notification at the end of this notification.
        /// </summary>
        /// <param name="other"></param>
        public override void Append(NotificationEventArgs other)
        {
            var skills = ((SkillCompletionNotificationEventArgs)other).Skills;
            foreach (QueuedSkill skill in skills)
                if (!Skills.Contains(skill))
                    Skills.Add(skill);
            UpdateDescription();
        }

        /// <summary>
        /// Updates the description.
        /// </summary>
        private void UpdateDescription()
        {
            Description = Skills.Count == 1 ? $"{Skills.First().SkillName} {Skill.GetRomanFromInt(Skills.First().Level)} completed." :
                $"{Skills.Count} skills completed.";
        }
    }
}
