using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using SkiaSharp;

namespace EVEMon.Avalonia.Controls
{
    /// <summary>
    /// A skill node in the constellation.
    /// </summary>
    public sealed class SkillNode
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public int GroupIndex { get; set; }
        public int Level { get; set; }
        public int Rank { get; set; }
        public bool IsTraining { get; set; }
        public string TrainingTime { get; set; } = string.Empty;
        public List<string> PrereqIds { get; set; } = new();

        // Layout
        public float X { get; set; }
        public float Y { get; set; }
        public float Vx { get; set; }
        public float Vy { get; set; }
    }

    /// <summary>
    /// A prerequisite edge between two skill nodes.
    /// </summary>
    public sealed class SkillEdge
    {
        public SkillNode From { get; set; } = null!;
        public SkillNode To { get; set; } = null!;
    }

    /// <summary>
    /// A group of skills displayed as a constellation cluster.
    /// </summary>
    public sealed class SkillGroupInfo
    {
        public string Name { get; set; } = string.Empty;
        public SKColor Color { get; set; }
        public int Index { get; set; }
        public List<SkillNode> Nodes { get; set; } = new();
    }

    /// <summary>
    /// GPU-accelerated skill constellation visualization using SkiaSharp.
    /// Pan/zoom/select/hover with nebula clouds, glow, and training pulse animation.
    /// </summary>
    public sealed class ConstellationCanvas : Control
    {
        // Data
        private readonly List<SkillGroupInfo> _groups = new();
        private readonly List<SkillNode> _allNodes = new();
        private readonly List<SkillEdge> _allEdges = new();
        private readonly Dictionary<string, SkillNode> _nodeMap = new();

        // Background stars
        private readonly List<(float X, float Y, float R, float Opacity, float Speed, float Delay)> _bgStars = new();

        // View transform
        private float _viewX, _viewY;
        private float _viewW = 1400, _viewH = 1100;
        private float _zoom = 1f;

        // Interaction
        private bool _isPanning;
        private Point _panStart;
        private float _panStartVx, _panStartVy;
        private string? _hoveredId;
        private SkillNode? _selected;
        private string? _highlightGroup;
        private bool _showAllLabels;
        private HashSet<string> _searchMatchIds = new();

        // Animation
        private float _time;
        private DispatcherTimer? _animTimer;

        // Events
        public event Action<SkillNode?>? SelectionChanged;
        public event Action<string?>? HighlightGroupChanged;

        public SkillNode? Selected => _selected;
        public string? HighlightGroup => _highlightGroup;
        public bool ShowAllLabels { get => _showAllLabels; set { _showAllLabels = value; InvalidateVisual(); } }

        public string CharacterName { get; set; } = string.Empty;
        public string SkillSummary { get; set; } = string.Empty;

        public IReadOnlyList<SkillGroupInfo> Groups => _groups;

        public void SetData(List<SkillGroupInfo> groups)
        {
            _groups.Clear();
            _allNodes.Clear();
            _allEdges.Clear();
            _nodeMap.Clear();
            _groups.AddRange(groups);

            foreach (var g in _groups)
            {
                foreach (var n in g.Nodes)
                {
                    _allNodes.Add(n);
                    _nodeMap[n.Id] = n;
                }
            }

            // Build edges
            foreach (var n in _allNodes)
            {
                foreach (string pid in n.PrereqIds)
                {
                    if (_nodeMap.TryGetValue(pid, out var from))
                        _allEdges.Add(new SkillEdge { From = from, To = n });
                }
            }

            // Initial layout
            ComputeInitialLayout();
            GenerateBackgroundStars();

            // Center view
            if (_allNodes.Count > 0)
            {
                float minX = _allNodes.Min(n => n.X) - 200;
                float minY = _allNodes.Min(n => n.Y) - 200;
                float maxX = _allNodes.Max(n => n.X) + 200;
                float maxY = _allNodes.Max(n => n.Y) + 200;
                _viewX = minX;
                _viewY = minY;
                _viewW = maxX - minX;
                _viewH = maxY - minY;
            }

            InvalidateVisual();
        }

        private void ComputeInitialLayout()
        {
            // Arrange groups in a grid-like spiral, skills in concentric rings within each group.
            // This is stable (no physics) and scales to 400+ skills.
            int groupCount = _groups.Count;
            if (groupCount == 0) return;

            // Compute grid dimensions for groups
            int cols = (int)Math.Ceiling(Math.Sqrt(groupCount * 1.5));
            int rows = (int)Math.Ceiling((double)groupCount / cols);
            float groupSpacingX = 350;
            float groupSpacingY = 320;

            for (int gi = 0; gi < groupCount; gi++)
            {
                var group = _groups[gi];
                int col = gi % cols;
                int row = gi / cols;

                // Offset odd rows for a hex-like pattern
                float offsetX = (row % 2 == 1) ? groupSpacingX * 0.5f : 0;
                float gx = col * groupSpacingX + offsetX + 200;
                float gy = row * groupSpacingY + 200;

                int skillCount = group.Nodes.Count;

                // Place skills in expanding rings around group center
                // First skill at center, then ring of 6, ring of 12, etc.
                int placed = 0;
                int ring = 0;
                while (placed < skillCount)
                {
                    if (ring == 0)
                    {
                        // Center node
                        group.Nodes[placed].X = gx;
                        group.Nodes[placed].Y = gy;
                        placed++;
                        ring++;
                        continue;
                    }

                    int nodesInRing = ring * 6;
                    float ringRadius = ring * 42;
                    for (int ri = 0; ri < nodesInRing && placed < skillCount; ri++)
                    {
                        float angle = (float)(2 * Math.PI * ri / nodesInRing) + ring * 0.3f;
                        group.Nodes[placed].X = gx + (float)Math.Cos(angle) * ringRadius;
                        group.Nodes[placed].Y = gy + (float)Math.Sin(angle) * ringRadius;
                        placed++;
                    }
                    ring++;
                }

                // Zero out velocities
                foreach (var n in group.Nodes)
                {
                    n.Vx = 0;
                    n.Vy = 0;
                }
            }
        }

        private void GenerateBackgroundStars()
        {
            _bgStars.Clear();
            var rng = new Random(42);
            float minX = _allNodes.Count > 0 ? _allNodes.Min(n => n.X) - 300 : 0;
            float maxX = _allNodes.Count > 0 ? _allNodes.Max(n => n.X) + 300 : 1400;
            float minY = _allNodes.Count > 0 ? _allNodes.Min(n => n.Y) - 300 : 0;
            float maxY = _allNodes.Count > 0 ? _allNodes.Max(n => n.Y) + 300 : 1100;

            for (int i = 0; i < 400; i++)
            {
                _bgStars.Add((
                    X: minX + (float)rng.NextDouble() * (maxX - minX),
                    Y: minY + (float)rng.NextDouble() * (maxY - minY),
                    R: (float)rng.NextDouble() * 1.2f + 0.3f,
                    Opacity: (float)rng.NextDouble() * 0.4f + 0.1f,
                    Speed: (float)rng.NextDouble() * 3f + 2f,
                    Delay: (float)rng.NextDouble() * 5f
                ));
            }
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _animTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) }; // 20fps for animation
            _animTimer.Tick += (_, _) =>
            {
                _time += 0.05f;
                InvalidateVisual();
            };
            _animTimer.Start();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            _animTimer?.Stop();
            _animTimer = null;
            base.OnDetachedFromVisualTree(e);
        }

        // ── Input handling ──

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            var pos = e.GetPosition(this);

            // Hit test nodes
            var worldPos = ScreenToWorld((float)pos.X, (float)pos.Y);
            var hit = HitTestNode(worldPos.X, worldPos.Y);
            if (hit != null)
            {
                _selected = hit;
                SelectionChanged?.Invoke(_selected);
                InvalidateVisual();
                e.Handled = true;
                return;
            }

            // Start pan
            _isPanning = true;
            _panStart = pos;
            _panStartVx = _viewX;
            _panStartVy = _viewY;
            e.Handled = true;
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            var pos = e.GetPosition(this);

            if (_isPanning)
            {
                float dx = (float)(pos.X - _panStart.X) / (float)Bounds.Width * _viewW;
                float dy = (float)(pos.Y - _panStart.Y) / (float)Bounds.Height * _viewH;
                _viewX = _panStartVx - dx;
                _viewY = _panStartVy - dy;
                InvalidateVisual();
                return;
            }

            // Hover
            var worldPos = ScreenToWorld((float)pos.X, (float)pos.Y);
            var hit = HitTestNode(worldPos.X, worldPos.Y);
            string? newHover = hit?.Id;
            if (newHover != _hoveredId)
            {
                _hoveredId = newHover;
                Cursor = _hoveredId != null ? new Cursor(StandardCursorType.Hand) : new Cursor(StandardCursorType.Arrow);
                InvalidateVisual();
            }
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            _isPanning = false;
        }

        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            base.OnPointerWheelChanged(e);
            var pos = e.GetPosition(this);
            float mx = (float)pos.X / (float)Bounds.Width * _viewW + _viewX;
            float my = (float)pos.Y / (float)Bounds.Height * _viewH + _viewY;
            float factor = e.Delta.Y > 0 ? 0.9f : 1.1f;
            float nw = _viewW * factor;
            float nh = _viewH * factor;
            if (nw < 200 || nw > 6000) return;
            _viewX = mx - (mx - _viewX) * factor;
            _viewY = my - (my - _viewY) * factor;
            _viewW = nw;
            _viewH = nh;
            InvalidateVisual();
            e.Handled = true;
        }

        public void FocusGroup(int groupIndex)
        {
            var group = _groups.FirstOrDefault(g => g.Index == groupIndex);
            if (group == null || group.Nodes.Count == 0) return;
            float cx = group.Nodes.Average(n => n.X);
            float cy = group.Nodes.Average(n => n.Y);
            _viewX = cx - 300;
            _viewY = cy - 250;
            _viewW = 600;
            _viewH = 500;
            InvalidateVisual();
        }

        public void SetHighlightGroup(string? name)
        {
            _highlightGroup = name;
            HighlightGroupChanged?.Invoke(name);
            InvalidateVisual();
        }

        public void ClearSelection()
        {
            _selected = null;
            SelectionChanged?.Invoke(null);
            InvalidateVisual();
        }

        public void SetSearchResults(HashSet<string> matchIds)
        {
            _searchMatchIds = matchIds;
            InvalidateVisual();
        }

        public void FocusNode(string nodeId)
        {
            if (!_nodeMap.TryGetValue(nodeId, out var node)) return;
            _viewX = node.X - _viewW / 2;
            _viewY = node.Y - _viewH / 2;
            _selected = node;
            SelectionChanged?.Invoke(_selected);
            InvalidateVisual();
        }

        private (float X, float Y) ScreenToWorld(float sx, float sy)
        {
            return (
                sx / (float)Bounds.Width * _viewW + _viewX,
                sy / (float)Bounds.Height * _viewH + _viewY
            );
        }

        private SkillNode? HitTestNode(float wx, float wy)
        {
            float bestDist = float.MaxValue;
            SkillNode? best = null;
            foreach (var n in _allNodes)
            {
                float dx = n.X - wx;
                float dy = n.Y - wy;
                float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                float hitRadius = StarSize(n) + 8;
                if (dist < hitRadius && dist < bestDist)
                {
                    bestDist = dist;
                    best = n;
                }
            }
            return best;
        }

        private static float StarSize(SkillNode node)
        {
            float b = 3 + node.Rank * 0.4f;
            if (node.Level == 5) return b + 3;
            if (node.Level >= 3) return b + 1.5f;
            if (node.Level > 0) return b;
            return b - 1;
        }

        // ── Rendering ──

        public override void Render(DrawingContext context)
        {
            base.Render(context);
            context.Custom(new ConstellationDrawOp(
                new Rect(0, 0, Bounds.Width, Bounds.Height),
                _viewX, _viewY, _viewW, _viewH,
                _allNodes, _allEdges, _groups, _bgStars,
                _hoveredId, _selected, _highlightGroup, _showAllLabels, _time,
                _searchMatchIds, _nodeMap
            ));
        }

        /// <summary>
        /// Custom draw operation using SkiaSharp for GPU-accelerated rendering.
        /// Everything starts quiet. Interaction reveals: hover/select a node → it and its
        /// prereq chain glow up with labels and edges. Group chip hover → whole group lights up.
        /// </summary>
        private sealed class ConstellationDrawOp : ICustomDrawOperation
        {
            private readonly Rect _bounds;
            private readonly float _vx, _vy, _vw, _vh;
            private readonly List<SkillNode> _nodes;
            private readonly List<SkillEdge> _edges;
            private readonly List<SkillGroupInfo> _groups;
            private readonly List<(float X, float Y, float R, float Opacity, float Speed, float Delay)> _stars;
            private readonly string? _hoveredId;
            private readonly SkillNode? _selected;
            private readonly string? _highlightGroup;
            private readonly bool _showAllLabels;
            private readonly float _time;
            private readonly HashSet<string> _searchMatchIds;
            private readonly Dictionary<string, SkillNode> _nodeMap;

            // Computed per-frame: the set of "active" (lit up) node IDs
            private readonly HashSet<string> _activeIds = new();

            public ConstellationDrawOp(
                Rect bounds, float vx, float vy, float vw, float vh,
                List<SkillNode> nodes, List<SkillEdge> edges, List<SkillGroupInfo> groups,
                List<(float, float, float, float, float, float)> stars,
                string? hoveredId, SkillNode? selected, string? highlightGroup,
                bool showAllLabels, float time, HashSet<string> searchMatchIds,
                Dictionary<string, SkillNode> nodeMap)
            {
                _bounds = bounds;
                _vx = vx; _vy = vy; _vw = vw; _vh = vh;
                _nodes = nodes; _edges = edges; _groups = groups; _stars = stars;
                _hoveredId = hoveredId; _selected = selected;
                _highlightGroup = highlightGroup; _showAllLabels = showAllLabels; _time = time;
                _searchMatchIds = searchMatchIds; _nodeMap = nodeMap;

                // Build the active set: nodes that should be "lit up"
                ComputeActiveSet();
            }

            private void ComputeActiveSet()
            {
                _activeIds.Clear();

                // Hovered node + its full prereq chain
                if (_hoveredId != null)
                    AddWithPrereqs(_hoveredId);

                // Selected node + its full prereq chain
                if (_selected != null)
                    AddWithPrereqs(_selected.Id);

                // Group highlight: all nodes in that group
                if (_highlightGroup != null)
                {
                    foreach (var n in _nodes)
                    {
                        if (n.GroupName == _highlightGroup)
                            _activeIds.Add(n.Id);
                    }
                }

                // Search matches
                foreach (string id in _searchMatchIds)
                    _activeIds.Add(id);
            }

            private void AddWithPrereqs(string id)
            {
                if (!_activeIds.Add(id)) return; // already visited
                if (!_nodeMap.TryGetValue(id, out var node)) return;
                foreach (string pid in node.PrereqIds)
                    AddWithPrereqs(pid);
            }

            private bool IsActive(SkillNode node) => _activeIds.Contains(node.Id);
            private bool HasActiveContext => _activeIds.Count > 0;

            public Rect Bounds => _bounds;
            public bool HitTest(Point p) => true;
            public bool Equals(ICustomDrawOperation? other) => false;
            public void Dispose() { }

            public void Render(ImmediateDrawingContext context)
            {
                try
                {
                    var leaseFeature = context.TryGetFeature(typeof(ISkiaSharpApiLeaseFeature));
                    if (leaseFeature is not ISkiaSharpApiLeaseFeature skiaFeature) return;
                    using var lease = skiaFeature.Lease();
                    var canvas = lease.SkCanvas;
                    Render(canvas);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Constellation render error: {ex}");
                }
            }

            private void Render(SKCanvas canvas)
            {
                float w = (float)_bounds.Width;
                float h = (float)_bounds.Height;
                if (_vw < 1 || _vh < 1) return;
                float sx = w / _vw;
                float sy = h / _vh;

                canvas.Save();
                canvas.ClipRect(new SKRect(0, 0, w, h));
                canvas.Clear(new SKColor(0x06, 0x08, 0x0D));

                canvas.Scale(sx, sy);
                canvas.Translate(-_vx, -_vy);

                DrawNebulae(canvas);
                DrawBackgroundStars(canvas);
                DrawEdges(canvas);
                DrawNodes(canvas);
                DrawGroupLabels(canvas);

                canvas.Restore();
            }

            private void DrawNebulae(SKCanvas canvas)
            {
                foreach (var group in _groups)
                {
                    if (group.Nodes.Count == 0) continue;
                    float cx = group.Nodes.Average(n => n.X);
                    float cy = group.Nodes.Average(n => n.Y);

                    bool groupActive = _highlightGroup == group.Name;
                    float opacity = groupActive ? 0.12f
                        : HasActiveContext ? 0.015f : 0.04f;

                    using var paint = new SKPaint
                    {
                        IsAntialias = true,
                        Color = group.Color.WithAlpha((byte)(opacity * 255)),
                        MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 60)
                    };
                    canvas.DrawOval(cx, cy, 180, 140, paint);
                }
            }

            private void DrawBackgroundStars(SKCanvas canvas)
            {
                using var paint = new SKPaint { IsAntialias = true };
                foreach (var s in _stars)
                {
                    float twinkle = 0.5f + 0.5f * (float)Math.Sin(_time * s.Speed + s.Delay);
                    paint.Color = SKColors.White.WithAlpha((byte)(s.Opacity * twinkle * 255));
                    canvas.DrawCircle(s.X, s.Y, s.R, paint);
                }
            }

            private void DrawEdges(SKCanvas canvas)
            {
                using var paint = new SKPaint
                {
                    IsAntialias = true,
                    Style = SKPaintStyle.Stroke,
                    StrokeCap = SKStrokeCap.Round
                };

                foreach (var edge in _edges)
                {
                    // Only draw edges where BOTH nodes are active
                    bool fromActive = IsActive(edge.From);
                    bool toActive = IsActive(edge.To);
                    if (!fromActive || !toActive) continue;

                    bool isHoveredEdge = _hoveredId != null &&
                        (edge.From.Id == _hoveredId || edge.To.Id == _hoveredId);
                    bool isSelectedEdge = _selected != null &&
                        (edge.From.Id == _selected.Id || edge.To.Id == _selected.Id);

                    float op = isHoveredEdge ? 0.7f : isSelectedEdge ? 0.5f : 0.2f;

                    var edgeColor = edge.From.GroupIndex < _groups.Count
                        ? _groups[edge.From.GroupIndex].Color : new SKColor(0x80, 0x80, 0x80);
                    paint.Color = edgeColor.WithAlpha((byte)(op * 255));
                    paint.StrokeWidth = isHoveredEdge || isSelectedEdge ? 1.5f : 0.8f;

                    if (edge.To.Level == 0)
                        paint.PathEffect = SKPathEffect.CreateDash(new[] { 4f, 4f }, 0);
                    else
                        paint.PathEffect = null;

                    canvas.DrawLine(edge.From.X, edge.From.Y, edge.To.X, edge.To.Y, paint);
                }
                paint.PathEffect = null;
            }

            private void DrawNodes(SKCanvas canvas)
            {
                using var paint = new SKPaint { IsAntialias = true };
                using var textPaint = new SKPaint
                {
                    IsAntialias = true,
                    TextSize = 8,
                    Typeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Normal)
                };

                foreach (var node in _nodes)
                {
                    bool isHov = _hoveredId == node.Id;
                    bool isSel = _selected?.Id == node.Id;
                    bool active = IsActive(node);
                    bool isTraining = node.IsTraining;
                    bool isMatch = _searchMatchIds.Count > 0 && _searchMatchIds.Contains(node.Id);

                    var groupColor = node.GroupIndex < _groups.Count
                        ? _groups[node.GroupIndex].Color : new SKColor(0x80, 0x80, 0x80);

                    // ── Compute opacity ──
                    float opacity;
                    if (isHov || isSel) opacity = 1f;
                    else if (isMatch) opacity = 1f;
                    else if (active) opacity = 0.85f;
                    else if (HasActiveContext)
                    {
                        // Something is active but this node isn't — dim it way down
                        opacity = node.Level > 0 ? 0.1f : 0.04f;
                    }
                    else
                    {
                        // Idle state: trained visible, untrained subtle
                        if (isTraining) opacity = 1f;
                        else if (node.Level == 5) opacity = 0.7f;
                        else if (node.Level >= 3) opacity = 0.5f;
                        else if (node.Level > 0) opacity = 0.35f;
                        else opacity = 0.12f;
                    }

                    if (opacity < 0.02f) continue;

                    float pulse = isTraining ? 1 + 0.3f * (float)Math.Sin(_time * 3) : 1f;
                    float baseSize = StarSize(node);
                    float size = (isHov || isSel) ? baseSize * 1.6f : active ? baseSize * 1.2f : baseSize;

                    // ── Glow (active/trained nodes only) ──
                    if (active || isTraining || node.Level == 5)
                    {
                        float glowR = isTraining ? 20 : (isHov || isSel) ? 16 : active ? 10 : 8;
                        float glowOp = isTraining
                            ? 0.2f + 0.1f * (float)Math.Sin(_time * 2)
                            : (isHov || isSel) ? 0.15f : 0.06f;
                        paint.Color = groupColor.WithAlpha((byte)(glowOp * opacity * 255));
                        paint.MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, glowR * 0.6f);
                        canvas.DrawCircle(node.X, node.Y, glowR * pulse, paint);
                        paint.MaskFilter = null;
                    }

                    // ── Training pulse rings ──
                    if (isTraining)
                    {
                        paint.Style = SKPaintStyle.Stroke;
                        paint.StrokeWidth = 0.8f;
                        for (int r = 0; r < 2; r++)
                        {
                            float phase = (_time * 0.5f + r * 0.5f) % 1f;
                            paint.Color = new SKColor(0xE8, 0xA4, 0x4A, (byte)(0.4f * (1 - phase) * 255));
                            canvas.DrawCircle(node.X, node.Y, 12 + 15 * phase, paint);
                        }
                        paint.Style = SKPaintStyle.Fill;
                    }

                    // ── Star core ──
                    float coreR = size * pulse;
                    if (node.Level > 0)
                    {
                        // Filled star
                        paint.Color = groupColor.WithAlpha((byte)(opacity * 255));
                        canvas.DrawCircle(node.X, node.Y, coreR, paint);

                        // White center point
                        float centerOp = 0.5f + (node.Level / 5f) * 0.5f;
                        paint.Color = SKColors.White.WithAlpha((byte)(centerOp * opacity * 255));
                        canvas.DrawCircle(node.X, node.Y, coreR * 0.3f, paint);
                    }
                    else
                    {
                        // Untrained: tiny dot in idle, hollow ring when active
                        if (active || isHov || isSel)
                        {
                            paint.Style = SKPaintStyle.Stroke;
                            paint.StrokeWidth = 1.2f;
                            paint.Color = groupColor.WithAlpha((byte)(0.6f * opacity * 255));
                            canvas.DrawCircle(node.X, node.Y, coreR, paint);
                            paint.Style = SKPaintStyle.Fill;
                            paint.Color = groupColor.WithAlpha((byte)(0.1f * opacity * 255));
                            canvas.DrawCircle(node.X, node.Y, coreR - 0.5f, paint);
                        }
                        else
                        {
                            // Just a tiny dim dot
                            paint.Color = groupColor.WithAlpha((byte)(opacity * 255));
                            canvas.DrawCircle(node.X, node.Y, Math.Max(coreR * 0.6f, 1.5f), paint);
                        }
                    }

                    // ── Search match ring ──
                    if (isMatch)
                    {
                        float mp = 1f + 0.15f * (float)Math.Sin(_time * 4);
                        paint.Style = SKPaintStyle.Stroke;
                        paint.StrokeWidth = 2f;
                        paint.Color = new SKColor(0xFF, 0xFF, 0x00, 0xCC);
                        canvas.DrawCircle(node.X, node.Y, (size + 8) * mp, paint);
                        paint.Style = SKPaintStyle.Fill;
                    }

                    // ── Hover/selection ring ──
                    if (isHov || isSel)
                    {
                        paint.Style = SKPaintStyle.Stroke;
                        paint.StrokeWidth = 1f;
                        paint.Color = groupColor.WithAlpha(0x80);
                        paint.PathEffect = SKPathEffect.CreateDash(new[] { 3f, 3f }, 0);
                        canvas.DrawCircle(node.X, node.Y, size + 6, paint);
                        paint.PathEffect = null;
                        paint.Style = SKPaintStyle.Fill;
                    }

                    // ── Labels: only when active, hovered, selected, or global override ──
                    bool showLabel = _showAllLabels || isHov || isSel || isMatch
                        || (active && (HasActiveContext));

                    if (showLabel)
                    {
                        if (isHov || isSel)
                        {
                            textPaint.TextSize = 10;
                            textPaint.Typeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold);
                        }
                        else
                        {
                            textPaint.TextSize = 8;
                            textPaint.Typeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Normal);
                        }

                        float labelOp = (isHov || isSel || isMatch) ? 1f : 0.6f;
                        textPaint.Color = SKColors.White.WithAlpha((byte)(labelOp * opacity * 255));
                        textPaint.TextAlign = SKTextAlign.Center;
                        canvas.DrawText(node.Name, node.X, node.Y + size + 14, textPaint);

                        // Level pips for interactive nodes
                        if (isHov || isSel || isTraining)
                            DrawLevelPips(canvas, node.X - 17, node.Y + size + 20, node.Level, opacity);
                    }
                }
            }

            private static void DrawLevelPips(SKCanvas canvas, float x, float y, int level, float opacity)
            {
                using var paint = new SKPaint { IsAntialias = true };
                for (int i = 0; i < 5; i++)
                {
                    float px = x + i * 8;
                    paint.Color = i < level
                        ? new SKColor(0xE8, 0xA4, 0x4A, (byte)(opacity * 255))
                        : SKColors.White.WithAlpha((byte)(0.1f * opacity * 255));
                    canvas.DrawRoundRect(new SKRoundRect(new SKRect(px, y, px + 6, y + 6), 1), paint);
                }
            }

            private void DrawGroupLabels(SKCanvas canvas)
            {
                using var paint = new SKPaint
                {
                    IsAntialias = true,
                    TextSize = 11,
                    Typeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold),
                    TextAlign = SKTextAlign.Center
                };

                foreach (var group in _groups)
                {
                    if (group.Nodes.Count == 0) continue;
                    float cx = group.Nodes.Average(n => n.X);
                    float minY = group.Nodes.Min(n => n.Y);
                    float op = _highlightGroup == group.Name ? 1f
                        : HasActiveContext ? 0.12f : 0.4f;
                    paint.Color = group.Color.WithAlpha((byte)(op * 255));
                    canvas.DrawText(group.Name.ToUpperInvariant(), cx, minY - 35, paint);
                }
            }

            private static float StarSize(SkillNode node)
            {
                float b = 3 + node.Rank * 0.35f;
                if (node.Level == 5) return b + 2;
                if (node.Level >= 3) return b + 1;
                if (node.Level > 0) return b;
                return b - 1;
            }
        }
    }
}
