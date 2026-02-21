// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using Avalonia.Media;
using EVEMon.Common.Enumerations;
using EVEMon.Common.Models;

namespace EVEMon.Avalonia.ViewModels
{
    /// <summary>
    /// Avalonia display wrapper for PlanEntry with IBrush properties.
    /// Contains zero business logic per Law 16.
    /// </summary>
    internal sealed class PlanEntryDisplayItem : IPlanDisplayItem
    {
        public PlanDisplayItemKind Kind => PlanDisplayItemKind.SkillEntry;
        private static readonly IBrush TrainingColor = new SolidColorBrush(Color.Parse("#FFE6A817"));
        private static readonly IBrush MissingColor = new SolidColorBrush(Color.Parse("#FFF0F0F0"));
        private static readonly IBrush TrainedColor = new SolidColorBrush(Color.Parse("#FF707070"));
        private static readonly IBrush GreenBrush = new SolidColorBrush(Color.Parse("#FF81C784"));
        private static readonly IBrush YellowBrush = new SolidColorBrush(Color.Parse("#FFFFD54F"));
        private static readonly IBrush AlternateBg = new SolidColorBrush(Color.Parse("#FF151525"));
        private static readonly IBrush NormalBg = new SolidColorBrush(Color.Parse("#FF1A1A2E"));

        // Attribute text color brushes
        private static readonly IBrush AttrIntBrush = new SolidColorBrush(Color.Parse("#FF4FC3F7"));
        private static readonly IBrush AttrPerBrush = new SolidColorBrush(Color.Parse("#FFEF5350"));
        private static readonly IBrush AttrChaBrush = new SolidColorBrush(Color.Parse("#FF66BB6A"));
        private static readonly IBrush AttrWilBrush = new SolidColorBrush(Color.Parse("#FFAB47BC"));
        private static readonly IBrush AttrMemBrush = new SolidColorBrush(Color.Parse("#FFFFA726"));
        private static readonly IBrush AttrDefaultBrush = new SolidColorBrush(Color.Parse("#FFAAAAAA"));

        // Attribute pill background brushes (dark tinted)
        private static readonly IBrush AttrIntPillBg = new SolidColorBrush(Color.Parse("#254FC3F7"));
        private static readonly IBrush AttrPerPillBg = new SolidColorBrush(Color.Parse("#25EF5350"));
        private static readonly IBrush AttrChaPillBg = new SolidColorBrush(Color.Parse("#2566BB6A"));
        private static readonly IBrush AttrWilPillBg = new SolidColorBrush(Color.Parse("#25AB47BC"));
        private static readonly IBrush AttrMemPillBg = new SolidColorBrush(Color.Parse("#25FFA726"));
        private static readonly IBrush AttrDefaultPillBg = new SolidColorBrush(Color.Parse("#15AAAAAA"));

        // SP/hr color coding brushes
        private static readonly IBrush SpHrGreenBrush = new SolidColorBrush(Color.Parse("#FF81C784"));
        private static readonly IBrush SpHrYellowBrush = new SolidColorBrush(Color.Parse("#FFFFD54F"));
        private static readonly IBrush SpHrRedBrush = new SolidColorBrush(Color.Parse("#FFEF5350"));

        // Priority color brushes
        private static readonly IBrush PriorityHighBrush = new SolidColorBrush(Color.Parse("#FFCF6679"));
        private static readonly IBrush PriorityAboveBrush = new SolidColorBrush(Color.Parse("#FFFFD54F"));
        private static readonly IBrush PriorityNormalBrush = new SolidColorBrush(Color.Parse("#FFAAAAAA"));
        private static readonly IBrush PriorityBelowBrush = new SolidColorBrush(Color.Parse("#FF707070"));
        private static readonly IBrush PriorityLowBrush = new SolidColorBrush(Color.Parse("#FF4A4A5A"));

        public PlanEntry Entry { get; }
        public PlanEntryStatus Status { get; }

        public string SkillName => $"{Entry.Skill.Name} {Skill.GetRomanFromInt(Entry.Level)}";
        public long Level => Entry.Level;
        public string TrainingTimeText => FormatTime(Entry.TrainingTime);
        public string SpPerHourText => $"{Entry.SpPerHour:N0}";
        public string SkillPointsText => $"{Entry.SkillPointsRequired:N0} SP";
        public float ProgressFraction => Entry.FractionCompleted;
        public string ProgressText => $"{(Entry.FractionCompleted * 100):F0}%";
        public bool IsOmegaRequired => Entry.OmegaRequired;
        public string PriorityText => $"Priority {Entry.Priority}";
        public int Priority => Entry.Priority;
        public string GroupName => Entry.Skill.Group.Name;

        // Enriched properties
        public string LevelBadge => Skill.GetRomanFromInt(Entry.Level);
        public string RankText => $"{Entry.Skill.Rank}";
        public string PrimaryAttr => FormatAttributeFull(Entry.Skill.PrimaryAttribute);
        public string SecondaryAttr => FormatAttributeFull(Entry.Skill.SecondaryAttribute);
        public string PriorityBadge => Entry.Priority switch
        {
            1 => "1",
            2 => "2",
            3 => "3",
            4 => "4",
            5 => "5",
            _ => "3"
        };

        public IBrush PriorityBrush => Entry.Priority switch
        {
            1 => PriorityHighBrush,
            2 => PriorityAboveBrush,
            3 => PriorityNormalBrush,
            4 => PriorityBelowBrush,
            5 => PriorityLowBrush,
            _ => PriorityNormalBrush
        };

        public bool IsAlternate { get; set; }
        public bool IsNewlyAdded { get; set; }
        public bool IsFirstItem { get; set; }
        public bool IsLastItem { get; set; }
        public bool IsRecentlyMoved { get; set; }
        public bool CanMoveUp { get; set; }
        public bool CanMoveDown { get; set; }

        /// <summary>
        /// Segment average SP/hr, set externally during segmented list building.
        /// Used to determine SP/hr color coding.
        /// </summary>
        public double SegmentAverageSpPerHour { get; set; }

        /// <summary>
        /// SP/hr color brush: green if at/above segment average, red if significantly below.
        /// </summary>
        public IBrush SpPerHourBrush
        {
            get
            {
                if (SegmentAverageSpPerHour <= 0 || Entry.SpPerHour <= 0)
                    return AttrDefaultBrush;
                double ratio = Entry.SpPerHour / SegmentAverageSpPerHour;
                if (ratio >= 0.95) return SpHrGreenBrush;
                if (ratio >= 0.70) return SpHrYellowBrush;
                return SpHrRedBrush;
            }
        }

        /// <summary>
        /// Character's current trained level for the skill (0-5).
        /// </summary>
        public int TrainedLevel { get; set; }

        /// <summary>
        /// Target level for the plan entry (1-5).
        /// </summary>
        public int TargetLevel => (int)Entry.Level;

        /// <summary>
        /// Whether the character is currently training this skill.
        /// </summary>
        public bool IsCurrentlyTraining => Status == PlanEntryStatus.Training;
        public string ItemRowClass => IsAlternate ? "item-row-alt" : "item-row";
        public IBrush RowBackground => IsAlternate ? AlternateBg : NormalBg;

        public IBrush PrimaryAttrBrush => MapAttributeBrush(Entry.Skill.PrimaryAttribute);
        public IBrush SecondaryAttrBrush => MapAttributeBrush(Entry.Skill.SecondaryAttribute);
        public IBrush PrimaryAttrPillBg => MapAttributePillBg(Entry.Skill.PrimaryAttribute);
        public IBrush SecondaryAttrPillBg => MapAttributePillBg(Entry.Skill.SecondaryAttribute);

        public IBrush NameBrush => Status switch
        {
            PlanEntryStatus.Training => TrainingColor,
            PlanEntryStatus.Trained => TrainedColor,
            _ => MissingColor
        };

        public IBrush TimeBrush => GetTimeBrush(Entry.TrainingTime);

        public string FinishDateText => Entry.EndTime > DateTime.MinValue
            ? Entry.EndTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm")
            : string.Empty;

        public bool ShowProgress => Status == PlanEntryStatus.Training;

        public PlanEntryDisplayItem(PlanEntry entry, PlanEntryStatus status)
        {
            Entry = entry;
            Status = status;
        }

        private static string FormatTime(TimeSpan time)
        {
            if (time <= TimeSpan.Zero) return "Done";
            if (time.TotalDays >= 1) return $"{(int)time.TotalDays}d {time.Hours}h {time.Minutes}m";
            if (time.TotalHours >= 1) return $"{(int)time.TotalHours}h {time.Minutes}m";
            return $"{(int)time.TotalMinutes}m";
        }

        private static IBrush MapAttributeBrush(EveAttribute attr)
        {
            return attr switch
            {
                EveAttribute.Intelligence => AttrIntBrush,
                EveAttribute.Perception => AttrPerBrush,
                EveAttribute.Charisma => AttrChaBrush,
                EveAttribute.Willpower => AttrWilBrush,
                EveAttribute.Memory => AttrMemBrush,
                _ => AttrDefaultBrush
            };
        }

        private static IBrush GetTimeBrush(TimeSpan time)
        {
            if (time <= TimeSpan.Zero) return TrainedColor;
            if (time.TotalDays >= 7) return new SolidColorBrush(Color.Parse("#FFEF5350"));   // red — weeks
            if (time.TotalDays >= 1) return new SolidColorBrush(Color.Parse("#FFFFA726"));   // orange — days
            if (time.TotalHours >= 1) return YellowBrush;                                     // yellow — hours
            return MissingColor;                                                               // white — minutes
        }

        private static IBrush MapAttributePillBg(EveAttribute attr)
        {
            return attr switch
            {
                EveAttribute.Intelligence => AttrIntPillBg,
                EveAttribute.Perception => AttrPerPillBg,
                EveAttribute.Charisma => AttrChaPillBg,
                EveAttribute.Willpower => AttrWilPillBg,
                EveAttribute.Memory => AttrMemPillBg,
                _ => AttrDefaultPillBg
            };
        }

        private static string FormatAttributeFull(EveAttribute attr)
        {
            return attr switch
            {
                EveAttribute.Intelligence => "Intelligence",
                EveAttribute.Perception => "Perception",
                EveAttribute.Charisma => "Charisma",
                EveAttribute.Willpower => "Willpower",
                EveAttribute.Memory => "Memory",
                _ => "—"
            };
        }
    }

    internal enum PlanEntryStatus { Training, Missing, Trained }
}
