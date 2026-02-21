// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EVEMon.Avalonia.ViewModels
{
    /// <summary>
    /// Discriminator for display items in the segmented plan list.
    /// Used to select the correct DataTemplate in the ItemsControl.
    /// </summary>
    internal enum PlanDisplayItemKind
    {
        SkillEntry,
        SectionHeader,
        RemapDivider
    }

    /// <summary>
    /// Marker interface for all display items in the plan list
    /// (PlanEntryDisplayItem, PlanSectionHeader, PlanRemapDivider).
    /// </summary>
    internal interface IPlanDisplayItem
    {
        PlanDisplayItemKind Kind { get; }
    }
}
