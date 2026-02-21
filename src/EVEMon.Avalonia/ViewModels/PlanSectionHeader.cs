// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using Avalonia.Media;

namespace EVEMon.Avalonia.ViewModels
{
    /// <summary>
    /// Display item for a segment header row in the segmented plan list.
    /// Shows the dominant attribute focus (e.g., "Perception / Willpower Focus"),
    /// skill count, and total training time for the segment.
    /// Contains zero business logic per Law 16.
    /// </summary>
    internal sealed class PlanSectionHeader : IPlanDisplayItem
    {
        // Segment focus colors (tinted backgrounds)
        private static readonly IBrush IntFocusBg = new SolidColorBrush(Color.Parse("#304FC3F7"));
        private static readonly IBrush PerFocusBg = new SolidColorBrush(Color.Parse("#30EF5350"));
        private static readonly IBrush ChaFocusBg = new SolidColorBrush(Color.Parse("#3066BB6A"));
        private static readonly IBrush WilFocusBg = new SolidColorBrush(Color.Parse("#30AB47BC"));
        private static readonly IBrush MemFocusBg = new SolidColorBrush(Color.Parse("#30FFA726"));
        private static readonly IBrush DefaultFocusBg = new SolidColorBrush(Color.Parse("#30808080"));

        // Segment accent colors
        private static readonly IBrush IntAccent = new SolidColorBrush(Color.Parse("#FF4FC3F7"));
        private static readonly IBrush PerAccent = new SolidColorBrush(Color.Parse("#FFEF5350"));
        private static readonly IBrush ChaAccent = new SolidColorBrush(Color.Parse("#FF66BB6A"));
        private static readonly IBrush WilAccent = new SolidColorBrush(Color.Parse("#FFAB47BC"));
        private static readonly IBrush MemAccent = new SolidColorBrush(Color.Parse("#FFFFA726"));
        private static readonly IBrush DefaultAccent = new SolidColorBrush(Color.Parse("#FF808080"));

        public PlanDisplayItemKind Kind => PlanDisplayItemKind.SectionHeader;

        /// <summary>
        /// The segment index (0-based).
        /// </summary>
        public int SegmentIndex { get; init; }

        /// <summary>
        /// Display label, e.g., "Perception / Willpower Focus".
        /// </summary>
        public string FocusLabel { get; init; } = "";

        /// <summary>
        /// Short primary attribute name, e.g., "PER".
        /// </summary>
        public string PrimaryShortName { get; init; } = "";

        /// <summary>
        /// Short secondary attribute name, e.g., "WIL".
        /// </summary>
        public string SecondaryShortName { get; init; } = "";

        /// <summary>
        /// Number of skills in this segment.
        /// </summary>
        public int SkillCount { get; init; }

        /// <summary>
        /// Formatted training time for the segment, e.g., "28d 7h".
        /// </summary>
        public string TrainingTimeText { get; init; } = "";

        /// <summary>
        /// Average SP/hr for the segment.
        /// </summary>
        public string AvgSpPerHourText { get; init; } = "";

        /// <summary>
        /// Background brush tinted to the primary attribute color.
        /// </summary>
        public IBrush BackgroundBrush { get; init; } = DefaultFocusBg;

        /// <summary>
        /// Accent brush for the segment indicator strip.
        /// </summary>
        public IBrush AccentBrush { get; init; } = DefaultAccent;

        public static IBrush GetFocusBackground(string primaryAttrShort) => primaryAttrShort switch
        {
            "INT" => IntFocusBg,
            "PER" => PerFocusBg,
            "CHA" => ChaFocusBg,
            "WIL" => WilFocusBg,
            "MEM" => MemFocusBg,
            _ => DefaultFocusBg
        };

        public static IBrush GetAccentBrush(string primaryAttrShort) => primaryAttrShort switch
        {
            "INT" => IntAccent,
            "PER" => PerAccent,
            "CHA" => ChaAccent,
            "WIL" => WilAccent,
            "MEM" => MemAccent,
            _ => DefaultAccent
        };
    }
}
