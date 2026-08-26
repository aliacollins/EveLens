// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Threading;

namespace EveLens.Avalonia.Controls
{
    /// <summary>
    /// A WrapPanel whose children glide to their new positions when layout changes
    /// (FLIP animation: First-Last-Invert-Play). Reordering, sorting, resizing and
    /// group changes on the character overview animate instead of teleporting
    /// (Issue #72 overview rework). Extends WrapPanel so existing drag machinery
    /// that types against WrapPanel keeps working; horizontal orientation only.
    /// </summary>
    public class AnimatedWrapPanel : WrapPanel
    {
        private static readonly TimeSpan MoveDuration = TimeSpan.FromMilliseconds(260);
        private static readonly Easing MoveEasing = new CubicEaseOut();

        // Last arranged top-left per child, so the next arrange knows where each
        // child came from. Children not seen before fade/scale in instead.
        private readonly Dictionary<Control, Point> _lastArranged = new();

        /// <summary>Set false to arrange instantly (e.g. while a drag is in progress).</summary>
        public bool AnimationsEnabled { get; set; } = true;

        protected override Size MeasureOverride(Size availableSize)
        {
            double lineWidth = 0, lineHeight = 0, totalWidth = 0, totalHeight = 0;

            foreach (var child in Children)
            {
                child.Measure(availableSize);
                var size = child.DesiredSize;

                if (lineWidth + size.Width > availableSize.Width && lineWidth > 0)
                {
                    totalWidth = Math.Max(totalWidth, lineWidth);
                    totalHeight += lineHeight;
                    lineWidth = 0;
                    lineHeight = 0;
                }

                lineWidth += size.Width;
                lineHeight = Math.Max(lineHeight, size.Height);
            }

            totalWidth = Math.Max(totalWidth, lineWidth);
            totalHeight += lineHeight;
            return new Size(totalWidth, totalHeight);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            double x = 0, y = 0, lineHeight = 0;
            var seen = new HashSet<Control>();

            foreach (var child in Children)
            {
                var size = child.DesiredSize;

                if (x + size.Width > finalSize.Width && x > 0)
                {
                    x = 0;
                    y += lineHeight;
                    lineHeight = 0;
                }

                var target = new Point(x, y);
                child.Arrange(new Rect(target, size));
                seen.Add(child);

                if (AnimationsEnabled)
                {
                    if (_lastArranged.TryGetValue(child, out var previous))
                    {
                        var dx = previous.X - target.X;
                        var dy = previous.Y - target.Y;
                        if (Math.Abs(dx) > 0.5 || Math.Abs(dy) > 0.5)
                            RunMove(child, dx, dy);
                    }
                    else
                    {
                        RunEnter(child);
                    }
                }
                _lastArranged[child] = target;

                x += size.Width;
                lineHeight = Math.Max(lineHeight, size.Height);
            }

            // Forget children that were removed so a control re-added later enters
            // fresh instead of flying in from a stale position.
            var stale = new List<Control>();
            foreach (var key in _lastArranged.Keys)
                if (!seen.Contains(key)) stale.Add(key);
            foreach (var key in stale)
                _lastArranged.Remove(key);

            return finalSize;
        }

        /// <summary>
        /// FLIP: the child is already AT its new position; start it visually offset
        /// at its old position and let a transition glide it to identity.
        /// </summary>
        private static void RunMove(Control child, double dx, double dy)
        {
            // Skip if a drag interaction owns this child's transform right now
            if (child.Tag is string s && s == "drag-owned") return;

            child.Transitions = null;
            child.RenderTransform = TransformOperations.Parse(
                FormattableString.Invariant($"translate({dx}px, {dy}px)"));

            Dispatcher.UIThread.Post(() =>
            {
                child.Transitions = new Transitions
                {
                    new TransformOperationsTransition
                    {
                        Property = RenderTransformProperty,
                        Duration = MoveDuration,
                        Easing = MoveEasing
                    }
                };
                child.RenderTransform = TransformOperations.Parse("translate(0px, 0px)");
            }, DispatcherPriority.Render);
        }

        /// <summary>New children pop in with a quick fade + scale.</summary>
        private static void RunEnter(Control child)
        {
            if (child.Tag is string s && s == "drag-owned") return;

            child.Transitions = null;
            child.Opacity = 0;
            child.RenderTransform = TransformOperations.Parse("scale(0.92, 0.92)");

            Dispatcher.UIThread.Post(() =>
            {
                child.Transitions = new Transitions
                {
                    new DoubleTransition
                    {
                        Property = OpacityProperty,
                        Duration = MoveDuration,
                        Easing = MoveEasing
                    },
                    new TransformOperationsTransition
                    {
                        Property = RenderTransformProperty,
                        Duration = MoveDuration,
                        Easing = MoveEasing
                    }
                };
                child.Opacity = 1;
                child.RenderTransform = TransformOperations.Parse("scale(1, 1)");
            }, DispatcherPriority.Render);
        }
    }
}
