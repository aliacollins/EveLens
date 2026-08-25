// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace EveLens.Avalonia.Services
{
    /// <summary>
    /// Microsoft's Codicons icon font (CC-BY-4.0 — license ships beside the ttf in
    /// Assets/Fonts). One monochrome, theme-tinted icon language for the whole app:
    /// emoji rendered as uncontrollable colored pictographs and read as unfinished,
    /// and ad-hoc unicode shapes drift per platform font.
    /// </summary>
    public static class Codicon
    {
        /// <summary>The embedded codicon font family.</summary>
        public static readonly FontFamily Font =
            new("avares://EveLens/Assets/Fonts/codicon.ttf#codicon");

        // Codepoints from dist/codicon.csv, @vscode/codicons 0.0.46.
        public const string Trash = "\uEA81";
        public const string Target = "\uEBF8";
        public const string SettingsGear = "\uEB51";
        public const string Search = "\uEA6D";
        public const string Gripper = "\uEB04";
        public const string Graph = "\uEB03";
        public const string GraphLine = "\uEBE2";
        public const string DeviceCamera = "\uEADA";
        public const string DeviceCameraVideo = "\uEAD9";
        public const string Check = "\uEAB2";
        public const string Book = "\uEAA4";
        public const string ArrowUp = "\uEAA1";
        public const string ArrowDown = "\uEA9A";
        public const string Sync = "\uEA77";
        public const string Sparkle = "\uEC10";
        public const string Wand = "\uEBCF";
        public const string Warning = "\uEA6C";
        public const string Watch = "\uEB7C";
        public const string History = "\uEA82";

        /// <summary>An icon TextBlock, sized and tinted like the text beside it.</summary>
        public static TextBlock Icon(string glyph, double size, IBrush? brush = null)
        {
            var block = new TextBlock
            {
                Text = glyph,
                FontFamily = Font,
                FontSize = size,
                VerticalAlignment = VerticalAlignment.Center,
            };
            // Only set when given: assigning null OVERRIDES the inherited brush
            // with nothing, which rendered every un-tinted icon/label invisible
            // (the "empty pill" bug).
            if (brush != null)
                block.Foreground = brush;
            return block;
        }

        /// <summary>Icon + label in a row — the standard button content shape.</summary>
        public static Control IconText(string glyph, string text, double size,
            IBrush? brush = null)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6,
                VerticalAlignment = VerticalAlignment.Center,
            };
            panel.Children.Add(Icon(glyph, size, brush));
            var label = new TextBlock
            {
                Text = text,
                FontSize = size,
                VerticalAlignment = VerticalAlignment.Center,
            };
            if (brush != null)
                label.Foreground = brush;
            panel.Children.Add(label);
            return panel;
        }
    }
}
