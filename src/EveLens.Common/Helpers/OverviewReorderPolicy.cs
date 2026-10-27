// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Enumerations.UISettings;

namespace EveLens.Common.Helpers
{
    /// <summary>
    /// How the overview's Reorder mode relates to the sort mode. Manual reordering
    /// only makes sense in Custom order, so instead of disabling Reorder under the
    /// other sorts (which hid the grouping gestures from players), entering Reorder
    /// switches the sort to Custom. The switch is non-destructive: the saved custom
    /// order is simply shown again, never overwritten.
    /// </summary>
    public static class OverviewReorderPolicy
    {
        /// <summary>
        /// True when toggling Reorder must first switch the sort to Custom: only on
        /// entering reorder mode (leaving via Done never touches the sort), and only
        /// when the current sort isn't Custom already.
        /// </summary>
        public static bool ShouldSwitchToCustom(OverviewSortMode current, bool enteringReorder)
            => enteringReorder && current != OverviewSortMode.Custom;

        /// <summary>
        /// Whether reorder mode survives the user picking <paramref name="newSort"/>
        /// from the sort dropdown. Dragging while cards are auto-sorted would edit an
        /// order the user can't see, so any non-Custom sort ends reorder mode.
        /// </summary>
        public static bool StaysInReorderMode(bool inReorderMode, OverviewSortMode newSort)
            => inReorderMode && newSort == OverviewSortMode.Custom;
    }
}
