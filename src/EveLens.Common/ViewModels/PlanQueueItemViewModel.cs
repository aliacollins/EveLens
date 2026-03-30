// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EveLens.Common.Data;
using EveLens.Common.Enumerations;
using EveLens.Common.Models;
using EveLens.Common.Services;

namespace EveLens.Common.ViewModels
{
    /// <summary>
    /// ViewModel for a single skill in the plan queue list.
    /// Wraps a PlanEntry with chain assignment, time normalization, and display state.
    /// </summary>
    public sealed class PlanQueueItem
    {
        public PlanEntry Entry { get; }
        public StaticSkill Skill => Entry.Skill;
        public string Name => Skill.Name;
        public string DisplayName => $"{Skill.Name} {Models.Skill.GetRomanFromInt(Entry.Level)}";
        public long Level => Entry.Level;
        public long Rank => Skill.Rank;
        public EveAttribute PrimaryAttribute => Skill.PrimaryAttribute;
        public EveAttribute SecondaryAttribute => Skill.SecondaryAttribute;
        public TimeSpan TrainingTime => Entry.TrainingTime;
        public long SkillPointsPerHour => Entry.SpPerHour;
        public bool OmegaRequired => Entry.OmegaRequired;

        // Chain assignment (set by RecomputeChains)
        public int ChainGoalId { get; set; }
        public string ChainGoalName { get; set; } = string.Empty;
        public string ChainColor { get; set; } = "#4E5C6E";
        public ChainPosition ChainPosition { get; set; } = ChainPosition.Solo;
        public bool IsGoal { get; set; }

        // Time bar normalization (set by RecomputeTimeBars)
        public double TimeBarPercent { get; set; }
        public TimeSeverity TimeSeverity { get; set; }

        // Level pips
        public int TrainedLevel { get; set; }
        public int QueuedAbove { get; set; }

        // Selection state
        public bool IsSelected { get; set; }

        public PlanQueueItem(PlanEntry entry, Character? character)
        {
            Entry = entry;

            if (character != null)
            {
                long charLevel = character.GetSkillLevel(Skill);
                TrainedLevel = (int)Math.Min(charLevel, 5);
            }

            // Queued levels above trained
            QueuedAbove = (int)(Level - TrainedLevel);
        }

        /// <summary>
        /// Formatted training time for display.
        /// </summary>
        public string TimeText
        {
            get
            {
                var ts = TrainingTime;
                if (ts.TotalDays >= 1)
                    return $"{(int)ts.TotalDays}d {ts.Hours}h";
                if (ts.TotalHours >= 1)
                    return $"{(int)ts.TotalHours}h {ts.Minutes}m";
                return $"{ts.Minutes}m {ts.Seconds}s";
            }
        }
    }

    /// <summary>
    /// How severe/long a skill's training time is — drives the time bar color.
    /// </summary>
    public enum TimeSeverity
    {
        Quick,    // < 1 day — green
        Medium,   // 1-7 days — gold
        Long,     // 7-30 days — orange
        Massive   // 30+ days — red
    }
}
