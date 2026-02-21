// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using Avalonia.Media;
using EVEMon.Common.ViewModels;

namespace EVEMon.Avalonia.ViewModels
{
    /// <summary>
    /// Avalonia display wrapper for PlanSkillEntry with IBrush properties.
    /// Contains zero business logic per Law 16.
    /// </summary>
    internal sealed class PlanSkillBrowserDisplayItem
    {
        private static readonly IBrush TrainedBrush = new SolidColorBrush(Color.Parse("#FFE6A817"));
        private static readonly IBrush UntrainedBrush = new SolidColorBrush(Color.Parse("#FFF0F0F0"));
        private static readonly IBrush PlannedBrush = new SolidColorBrush(Color.Parse("#FFFFD54F"));
        private static readonly IBrush RankBrush = new SolidColorBrush(Color.Parse("#FF707070"));

        public PlanSkillEntry Entry { get; }

        public string Name => Entry.Name;
        public string RankText => Entry.RankText;
        public string LevelText => Entry.LevelText;
        public bool IsKnown => Entry.IsKnown;
        public bool IsPlanned => Entry.PlannedLevel > Entry.CharacterLevel;

        public IBrush NameBrush => Entry.IsKnown ? TrainedBrush
            : Entry.PlannedLevel > 0 ? PlannedBrush
            : UntrainedBrush;

        public PlanSkillBrowserDisplayItem(PlanSkillEntry entry)
        {
            Entry = entry;
        }
    }
}
