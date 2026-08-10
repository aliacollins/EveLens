// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using EveLens.Common.Constants;
using EveLens.Common.Models;
using EveLens.Common.Services.Planetary;
using SkiaSharp;

namespace EveLens.Avalonia.Controls
{
    /// <summary>
    /// Renders a colony's production chain as a left-to-right flow diagram.
    /// Columns: Extractors → Storage → Factories → Export
    /// Lines connect via routes, colored by material tier.
    /// </summary>
    public sealed class ColonyFlowCanvas : Control
    {
        private ColonyAnalysis? _analysis;
        private PlanetaryColony? _colony;
        private List<FlowNode> _nodes = new();
        private List<FlowEdge> _edges = new();
        private bool _needsRelayout;

        public ColonyFlowCanvas()
        {
            HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Stretch;
            VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Stretch;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            // Under infinite width (a vertical ScrollViewer offers unbounded width), claim 0 rather
            // than a magic 900px. The control is Stretch-aligned, so it takes the parent's real width
            // and draws against Bounds — claiming 900 left a gap to the right until a resize forced
            // a relayout. Height still grows with the tallest column. (Issue #66)
            double w = double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width;
            int nodeCount = Math.Max(_nodes.Count(n => n.Column == 0), Math.Max(_nodes.Count(n => n.Column == 1), _nodes.Count(n => n.Column == 2)));
            double h = Math.Max(280, 60 + nodeCount * 52);
            return new Size(w, h);
        }

        protected override void OnSizeChanged(SizeChangedEventArgs e)
        {
            base.OnSizeChanged(e);
            if (_nodes.Count > 0 && e.NewSize.Width > 100)
            {
                // Relayout NOW, synchronously with the size change, so node positions are
                // always computed from the same bounds the next frame draws with. Deferring
                // to Render (the old _needsRelayout pattern) drew one frame with stale node
                // positions during interactive resize — the transient right-side gap (#66).
                _needsRelayout = false;
                RelayoutWithWidth((float)e.NewSize.Width);
                InvalidateVisual();
            }
        }

        public void SetColony(PlanetaryColony colony)
        {
            _colony = colony;
            _analysis = ProductionChainAnalyzer.Analyze(colony);
            _needsRelayout = true;
            LayoutNodes();
            InvalidateVisual();
        }

        public void Clear()
        {
            _colony = null;
            _analysis = null;
            _nodes.Clear();
            _edges.Clear();
            InvalidateVisual();
        }

        private void LayoutNodes()
        {
            _nodes.Clear();
            _edges.Clear();

            if (_colony == null || _analysis == null) return;

            var pins = _colony.Pins.ToList();
            var routes = _colony.Routes.ToList();

            // Classify pins into columns
            var extractors = new List<PlanetaryPin>();
            var factories = new List<PlanetaryPin>();
            var storage = new List<PlanetaryPin>();

            var storageGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { "Command Centers", "Spaceports", "Storage Facilities" };

            foreach (var pin in pins)
            {
                if (DBConstants.EcuTypeIDs.Contains(pin.TypeID))
                    extractors.Add(pin);
                else if (pin.SchematicID > 0)
                    factories.Add(pin);
                else if (storageGroups.Contains(pin.GroupName ?? ""))
                    storage.Add(pin);
            }

            // Group factories by production tier for separate columns
            var factoryByTier = factories
                .Select(f => (pin: f, info: _analysis.Factories.FirstOrDefault(fi => fi.Pin == f)))
                .GroupBy(x => x.info?.Tier ?? ProductionTier.Basic)
                .OrderBy(g => g.Key)
                .ToList();

            // Dynamic columns: ECU(0) | Storage(1) | P1 Factories(2) | P2 Factories(3) | ...
            int numColumns = 2 + factoryByTier.Count;
            float availableWidth = Math.Max((float)Bounds.Width, 700f);
            float colWidth = (availableWidth - 60f) / numColumns;
            const float nodeHeight = 44f;
            const float nodeSpacing = 10f;
            const float startX = 30f;
            const float startY = 60f;

            // Column 0: Extractors
            float y = startY;
            foreach (var pin in extractors)
            {
                var ext = _analysis.Extractors.FirstOrDefault(e => e.Pin == pin);
                string label = ext != null && !string.IsNullOrEmpty(ext.OutputTypeName)
                    ? ext.OutputTypeName
                    : !string.IsNullOrEmpty(pin.ContentTypeName) ? pin.ContentTypeName : "Extractor";
                string detail = ext != null && ext.CurrentYieldPerHour > 0
                    ? $"{ext.CurrentYieldPerHour:F0}/hr"
                    : pin.State == Common.Enumerations.PlanetaryPinState.Extracting ? "Active" : "Idle";
                var state = pin.State == Common.Enumerations.PlanetaryPinState.Extracting
                    ? FlowNodeState.Active : FlowNodeState.Idle;

                _nodes.Add(new FlowNode(pin.ID, label, detail, "ECU", 0, startX, y, state));
                y += nodeHeight + nodeSpacing;
            }

            // Column 1: Storage
            y = startY;
            foreach (var pin in storage)
            {
                string label = pin.TypeName;
                string detail = pin.ContentQuantity > 0 ? $"{pin.ContentQuantity} units" : "Empty";
                _nodes.Add(new FlowNode(pin.ID, label, detail, "Store", 1, startX + colWidth, y, FlowNodeState.Neutral));
                y += nodeHeight + nodeSpacing;
            }

            // Columns 2+: Factories grouped by tier
            int colIndex = 2;
            foreach (var tierGroup in factoryByTier)
            {
                y = startY;
                string tierLabel = tierGroup.Key switch
                {
                    ProductionTier.Basic => "P1",
                    ProductionTier.Advanced => "P2",
                    ProductionTier.AdvancedP3 => "P3",
                    ProductionTier.HighTech => "P4",
                    _ => "Fac"
                };

                foreach (var (pin, info) in tierGroup)
                {
                    string label = info?.OutputName ?? pin.TypeName;
                    string detail = info != null ? $"{info.OutputPerHour}/hr" : "";
                    _nodes.Add(new FlowNode(pin.ID, label, detail, tierLabel, colIndex, startX + colWidth * colIndex, y, FlowNodeState.Active));
                    y += nodeHeight + nodeSpacing;
                }
                colIndex++;
            }

            // Build edges from routes
            var nodeMap = _nodes.ToDictionary(n => n.PinID);
            foreach (var route in routes)
            {
                if (nodeMap.TryGetValue(route.SourcePinID, out var from) &&
                    nodeMap.TryGetValue(route.DestinationPinID, out var to))
                {
                    int tier = route.ContentTypeID > 0
                        ? InferTierFromTypeId(route.ContentTypeID)
                        : 0;
                    _edges.Add(new FlowEdge(from, to, tier));
                }
            }
        }

        private static int InferTierFromTypeId(int typeId)
        {
            var schematic = PlanetarySchematicsProvider.GetSchematicByOutputType(typeId);
            if (schematic == null) return 0; // P0 raw
            return schematic.Tier switch
            {
                ProductionTier.Basic => 1,
                ProductionTier.Advanced => 2,
                ProductionTier.AdvancedP3 => 3,
                ProductionTier.HighTech => 4,
                _ => 0
            };
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            if (_needsRelayout && Bounds.Width > 100)
            {
                _needsRelayout = false;
                RelayoutWithWidth((float)Bounds.Width);
            }

            context.Custom(new FlowDrawOp(
                new Rect(0, 0, Bounds.Width, Bounds.Height),
                _nodes, _edges, _colony?.PlanetName ?? ""));
        }

        private void RelayoutWithWidth(float width)
        {
            if (_colony != null)
            {
                LayoutNodes();
                return;
            }

            // Determine how many columns are in use
            int maxCol = 0;
            foreach (var node in _nodes)
                if (node.Column > maxCol) maxCol = node.Column;

            int numCols = maxCol + 1;
            float colWidth = (width - 60f) / numCols;
            const float startX = 30f;

            foreach (var node in _nodes)
                node.X = startX + node.Column * colWidth;
        }

        private sealed class FlowDrawOp : ICustomDrawOperation
        {
            private readonly Rect _bounds;
            private readonly List<FlowNode> _nodes;
            private readonly List<FlowEdge> _edges;
            private readonly string _title;

            public FlowDrawOp(Rect bounds, List<FlowNode> nodes, List<FlowEdge> edges, string title)
            {
                _bounds = bounds;
                _nodes = nodes;
                _edges = edges;
                _title = title;
            }

            public Rect Bounds => _bounds;

            public void Dispose() { }

            public bool Equals(ICustomDrawOperation? other) => false;

            public bool HitTest(Point p) => _bounds.Contains(p);

            public void Render(ImmediateDrawingContext context)
            {
                var leaseFeature = context.TryGetFeature(typeof(ISkiaSharpApiLeaseFeature));
                if (leaseFeature is not ISkiaSharpApiLeaseFeature skiaFeature) return;

                using var lease = skiaFeature.Lease();
                var canvas = lease.SkCanvas;

                canvas.Clear(new SKColor(18, 22, 30)); // dark space background

                DrawColumnHeaders(canvas);
                DrawEdges(canvas);
                DrawNodes(canvas);
                DrawTitle(canvas);
            }

            private void DrawTitle(SKCanvas canvas)
            {
                if (string.IsNullOrEmpty(_title)) return;
                using var paint = new SKPaint
                {
                    Color = new SKColor(200, 170, 80),
                    TextSize = 14,
                    IsAntialias = true,
                    Typeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold)
                };
                canvas.DrawText(_title, 40, 30, paint);
            }

            private void DrawColumnHeaders(SKCanvas canvas)
            {
                using var paint = new SKPaint
                {
                    Color = new SKColor(120, 130, 150),
                    TextSize = 11,
                    IsAntialias = true
                };

                // Derive column headers from actual node data
                var columns = _nodes.Select(n => n.Column).Distinct().OrderBy(c => c).ToList();
                if (columns.Count == 0) return;

                int maxCol = columns.Max();
                float colWidth = ((float)_bounds.Width - 60f) / (maxCol + 1);

                foreach (int col in columns)
                {
                    string header = _nodes.FirstOrDefault(n => n.Column == col)?.Role ?? "";
                    header = col switch
                    {
                        0 => "Extractors",
                        1 => "Storage",
                        _ => header + " Factories"
                    };
                    canvas.DrawText(header, 30 + col * colWidth, 50, paint);
                }
            }

            private void DrawEdges(SKCanvas canvas)
            {
                float canvasW = (float)_bounds.Width;
                int maxCol = _nodes.Count > 0 ? _nodes.Max(n => n.Column) + 1 : 3;
                float nodeW = Math.Min(180, (canvasW - 60f) / maxCol - 40f);

                foreach (var edge in _edges)
                {
                    var color = GetTierColor(edge.Tier);
                    float strokeWidth = GetTierStrokeWidth(edge.Tier);

                    using var paint = new SKPaint
                    {
                        Color = color,
                        StrokeWidth = strokeWidth,
                        IsAntialias = true,
                        Style = SKPaintStyle.Stroke
                    };

                    // Add gap: start 8px past node right edge, end 8px before dest left edge
                    float fromX = edge.From.X + nodeW + 8;
                    float fromY = edge.From.Y + 22;
                    float toX = edge.To.X - 8;
                    float toY = edge.To.Y + 22;

                    using var path = new SKPath();
                    path.MoveTo(fromX, fromY);

                    float cpOffset = (toX - fromX) * 0.4f;
                    path.CubicTo(fromX + cpOffset, fromY, toX - cpOffset, toY, toX, toY);
                    canvas.DrawPath(path, paint);

                    // Arrow head (scaled by stroke width)
                    float arrowSize = 5 + strokeWidth;
                    using var arrowPaint = new SKPaint
                    {
                        Color = color,
                        Style = SKPaintStyle.Fill,
                        IsAntialias = true
                    };
                    using var arrow = new SKPath();
                    arrow.MoveTo(toX, toY);
                    arrow.LineTo(toX - arrowSize, toY - arrowSize * 0.6f);
                    arrow.LineTo(toX - arrowSize, toY + arrowSize * 0.6f);
                    arrow.Close();
                    canvas.DrawPath(arrow, arrowPaint);
                }
            }

            private void DrawNodes(SKCanvas canvas)
            {
                foreach (var node in _nodes)
                {
                    DrawSingleNode(canvas, node);
                }
            }

            private void DrawSingleNode(SKCanvas canvas, FlowNode node)
            {
                float canvasWidth = (float)_bounds.Width;
                int maxCol = _nodes.Count > 0 ? _nodes.Max(n => n.Column) + 1 : 3;
                float w = Math.Min(180, (canvasWidth - 60f) / maxCol - 40f);
                float h = 44;
                var rect = new SKRect(node.X, node.Y, node.X + w, node.Y + h);

                // Background
                var bgColor = node.State switch
                {
                    FlowNodeState.Active => new SKColor(30, 50, 40),
                    FlowNodeState.Idle => new SKColor(50, 30, 30),
                    _ => new SKColor(35, 38, 48)
                };
                using var bgPaint = new SKPaint { Color = bgColor, Style = SKPaintStyle.Fill, IsAntialias = true };
                canvas.DrawRoundRect(rect, 6, 6, bgPaint);

                // Border
                var borderColor = node.State switch
                {
                    FlowNodeState.Active => new SKColor(80, 200, 120),
                    FlowNodeState.Idle => new SKColor(200, 80, 80),
                    _ => new SKColor(80, 90, 110)
                };
                using var borderPaint = new SKPaint { Color = borderColor, StrokeWidth = 1.5f, Style = SKPaintStyle.Stroke, IsAntialias = true };
                canvas.DrawRoundRect(rect, 6, 6, borderPaint);

                // Role badge (small tag)
                using var badgePaint = new SKPaint { Color = borderColor.WithAlpha(60), Style = SKPaintStyle.Fill };
                var badgeRect = new SKRect(node.X + 4, node.Y + 4, node.X + 44, node.Y + 16);
                canvas.DrawRoundRect(badgeRect, 3, 3, badgePaint);

                using var badgeTextPaint = new SKPaint
                {
                    Color = borderColor,
                    TextSize = 9,
                    IsAntialias = true
                };
                canvas.DrawText(node.Role, node.X + 8, node.Y + 14, badgeTextPaint);

                // Main label
                using var labelPaint = new SKPaint
                {
                    Color = SKColors.White,
                    TextSize = 11,
                    IsAntialias = true,
                    Typeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold)
                };
                canvas.DrawText(TruncateText(node.Label, 22), node.X + 8, node.Y + 30, labelPaint);

                // Detail text (right-aligned)
                if (!string.IsNullOrEmpty(node.Detail))
                {
                    using var detailPaint = new SKPaint
                    {
                        Color = new SKColor(150, 160, 180),
                        TextSize = 9,
                        IsAntialias = true,
                        TextAlign = SKTextAlign.Right
                    };
                    canvas.DrawText(node.Detail, node.X + w - 8, node.Y + 40, detailPaint);
                }
            }

            private static SKColor GetTierColor(int tier) => tier switch
            {
                0 => new SKColor(130, 140, 150),  // P0 - gray
                1 => new SKColor(70, 180, 220),   // P1 - cyan/blue
                2 => new SKColor(200, 80, 220),   // P2 - vivid purple/magenta
                3 => new SKColor(240, 160, 40),   // P3 - bright orange
                4 => new SKColor(240, 210, 60),   // P4 - gold
                _ => new SKColor(100, 100, 100)
            };

            private static float GetTierStrokeWidth(int tier) => tier switch
            {
                0 => 1.5f,
                1 => 2f,
                2 => 2.5f,
                3 => 3f,
                4 => 3.5f,
                _ => 1.5f
            };

            private static string TruncateText(string text, int maxLen)
            {
                if (text.Length <= maxLen) return text;
                return text.Substring(0, maxLen - 1) + "...";
            }
        }
    }

    internal sealed class FlowNode
    {
        public long PinID { get; }
        public string Label { get; }
        public string Detail { get; }
        public string Role { get; }
        public int Column { get; }
        public float X { get; set; }
        public float Y { get; }
        public FlowNodeState State { get; }

        public FlowNode(long pinId, string label, string detail, string role, int col, float x, float y, FlowNodeState state)
        {
            PinID = pinId;
            Label = label;
            Detail = detail;
            Role = role;
            Column = col;
            X = x;
            Y = y;
            State = state;
        }
    }

    internal sealed class FlowEdge
    {
        public FlowNode From { get; }
        public FlowNode To { get; }
        public int Tier { get; }

        public FlowEdge(FlowNode from, FlowNode to, int tier)
        {
            From = from;
            To = to;
            Tier = tier;
        }
    }

    internal enum FlowNodeState
    {
        Active,
        Idle,
        Neutral
    }
}
