// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Drawing;
using System.Windows.Forms;
using EVEMon.Common.Models;

namespace EVEMon.Common.CustomEventArgs
{
    /// <summary>
    /// Arguments for the skill clicked event args
    /// </summary>
    public sealed class SkillClickedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SkillClickedEventArgs"/> class.
        /// </summary>
        /// <param name="skill">The skill.</param>
        /// <param name="button">The button.</param>
        /// <param name="location">The location.</param>
        public SkillClickedEventArgs(Skill skill, MouseButtons button, Point location)
        {
            Skill = skill;
            Button = button;
            Location = location;
        }

        /// <summary>
        /// Gets the skill.
        /// </summary>
        /// <value>The skill.</value>
        public Skill Skill { get; }

        /// <summary>
        /// Gets the button.
        /// </summary>
        /// <value>The button.</value>
        public MouseButtons Button { get; }

        /// <summary>
        /// Gets the location.
        /// </summary>
        /// <value>The location.</value>
        public Point Location { get; }
    }
}