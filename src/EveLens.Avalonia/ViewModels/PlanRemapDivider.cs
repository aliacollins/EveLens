// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using Avalonia.Media;

namespace EveLens.Avalonia.ViewModels
{
    /// <summary>
    /// A single attribute delta for display in a remap divider.
    /// </summary>
    internal sealed class AttributeDelta
    {
        public string Name { get; init; } = "";
        public int NewValue { get; init; }
        public int Delta { get; init; }
        public IBrush ValueBrush { get; init; } = Brushes.Gray;
        public IBrush DeltaBrush { get; init; } = Brushes.Gray;
    }

    /// <summary>
    /// Display item for a gold remap divider row in the segmented plan list.
    /// Shows the attribute values at the remap point with delta arrows showing
    /// changes from current attributes. Contains zero business logic per Law 16.
    /// </summary>
    internal sealed class PlanRemapDivider : IPlanDisplayItem
    {
        private static readonly IBrush GoldBrush = new SolidColorBrush(Color.Parse("#FFE6A817"));
        private static readonly IBrush DimGoldBrush = new SolidColorBrush(Color.Parse("#99E6A817"));
        private static readonly IBrush GoldBg = new SolidColorBrush(Color.Parse("#25E6A817"));

        public PlanDisplayItemKind Kind => PlanDisplayItemKind.RemapDivider;

        /// <summary>
        /// Formatted attribute string, e.g., "INT 32  MEM 28  PER 17  WIL 17  CHA 17".
        /// </summary>
        public string AttributeSummary { get; init; } = "";

        /// <summary>
        /// Individual attribute values for rendering.
        /// </summary>
        public int Intelligence { get; init; }
        public int Perception { get; init; }
        public int Willpower { get; init; }
        public int Charisma { get; init; }
        public int Memory { get; init; }

        /// <summary>
        /// Whether this remap has been computed by the optimizer.
        /// </summary>
        public bool IsComputed { get; init; }

        /// <summary>
        /// Time savings text, e.g., "Save 14d 3h".
        /// </summary>
        public string TimeSavingsText { get; init; } = "";

        /// <summary>
        /// Remap availability text, e.g., "Remap available" or "Remap in 147 days".
        /// </summary>
        public string AvailabilityText { get; init; } = "";

        /// <summary>
        /// Brush for the availability text (green/yellow/red).
        /// </summary>
        public IBrush? AvailabilityBrush { get; init; }

        /// <summary>
        /// Attribute deltas for computed remaps (only attributes that change).
        /// </summary>
        public List<AttributeDelta> Deltas { get; init; } = new();

        /// <summary>
        /// The gold accent brush for the remap indicator.
        /// </summary>
        public IBrush AccentBrush => IsComputed ? GoldBrush : DimGoldBrush;

        /// <summary>
        /// Background brush (dark gold tint).
        /// </summary>
        public IBrush BackgroundBrush => GoldBg;
    }
}
