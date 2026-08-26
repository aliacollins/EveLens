// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Animation;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Threading;
using EveLens.Common.Enumerations;
using EveLens.Common.Models;
using EveLens.Avalonia.Services;
using EveLens.Common.Services;
using EveLens.Common.ViewModels;

namespace EveLens.Avalonia.Views.PlanEditor
{
    public partial class PlanQueueListControl : UserControl
    {
        private PlanQueueManager? _viewModel;
        private readonly HashSet<int> _selected = new();
        private int _lastClickIndex = -1;
        // 28, not 36: the schedule design is information-dense — at 36 a 40-skill
        // plan needed a second screen the mock fits on one.
        private const double RowHeight = 28;
        private bool _groupByAttribute;

        // Drag state
        private bool _isDragging;
        private int _dragStartIndex = -1;
        private double _dragStartY;
        private int _currentInsertSlot = -1;
        private readonly List<Control> _rowControls = new();
        private Border? _dragGhost;

        // Threshold drag: press captures position, promotes to drag after 5px movement
        private bool _dragPending;
        private int _dragPendingIndex = -1;
        private double _dragPendingY;
        private PointerPressedEventArgs? _dragPendingEvent;
        private const double DragThreshold = 5.0;

        /// <summary>
        /// Event raised when skills are reordered via drag.
        /// </summary>
        public event Action? Reordered;

        /// <summary>Raised when the selection settles on exactly one entry (that
        /// entry), or on none/many (null) — feeds the sidebar's Selected Skill card.</summary>
        public event Action<PlanQueueItem?>? SelectionChanged;

        /// <summary>Raised when a level pip is clicked: the user wants this skill's
        /// TARGET level changed — the editor half of "editable level cells".</summary>
        public event Action<PlanQueueItem, int>? LevelChangeRequested;

        /// <summary>Scrolls the entry into view and selects it — the inspector's
        /// "Locate in plan" for selections that scrolled out of the viewport.</summary>
        public void ScrollToItem(PlanQueueItem item)
        {
            int index = _viewModel?.Items.IndexOf(item) ?? -1;
            if (index < 0 || index >= _itemTops.Count - 1) return;
            double target = Math.Max(0,
                _itemTops[index] - QueueScroller.Bounds.Height / 3);
            QueueScroller.Offset = new Vector(0, target);
            _selected.Clear();
            _selected.Add(index);
            _lastClickIndex = index;
            UpdateSelectionVisuals();
        }

        /// <summary>The queue in display order, for consumers positioning an entry
        /// within the plan (the sidebar's completes-on / starts-after math).</summary>
        public System.Collections.Generic.IReadOnlyList<PlanQueueItem> QueueItems =>
            (System.Collections.Generic.IReadOnlyList<PlanQueueItem>?)_viewModel?.Items
            ?? System.Array.Empty<PlanQueueItem>();

        /// <summary>
        /// Event raised when a skill is double-clicked (for sidebar detail).
        /// </summary>
        public event Action<PlanQueueItem>? SkillDoubleClicked;

        /// <summary>
        /// Supplies the right-click context menu for a queue row (Plan to level, move,
        /// priority, Insert Remap Point...). Owned by the parent view, which has the
        /// plan/viewmodel context this control deliberately lacks. Without this the rows
        /// had NO context menu at all — the menu only existed on the legacy row builder
        /// that no longer renders (Issue #71).
        /// </summary>
        public Func<PlanQueueItem, ContextMenu>? ContextMenuFactory { get; set; }


        public PlanQueueListControl()
        {
            InitializeComponent();
            LocalizeHeaders();
        }

        private void LocalizeHeaders()
        {
            HeaderSkill.Text = Loc.Get("Plan.Skill");
            HeaderTime.Text = Loc.Get("Plan.Time");
            HeaderRank.Text = Loc.Get("Plan.Rank");
            HeaderPri.Text = Loc.Get("Plan.Primary");
            HeaderSec.Text = Loc.Get("Plan.Secondary");
            HeaderSphr.Text = Loc.Get("Plan.SPPerHour");
            HeaderLevel.Text = Loc.Get("Plan.Level");
        }

        public bool GroupByAttribute
        {
            get => _groupByAttribute;
            set { _groupByAttribute = value; Rebuild(); }
        }

        public void SetViewModel(PlanQueueManager viewModel)
        {
            _viewModel = viewModel;
            _selected.Clear();
            Rebuild();
        }

        public void Rebuild()
        {
            if (_viewModel == null) return;

            BuildRows();
            BuildMinimap();
            BuildLegend();
        }

        #region Row Building

        // Header rows are REAL rows now. The old 1:1 rowControls↔Items invariant is
        // replaced by two structures: _rowControls stays item-indexed (selection and
        // drag visuals keep their index math), while _visualRows is what actually
        // renders (headers + rows), and _itemTops maps item index → Y so the drag
        // slot math works over non-uniform row heights.
        private const double HeaderRowHeight = 26;
        private readonly List<Control> _visualRows = new();
        private readonly List<double> _itemTops = new();

        private void BuildRows()
        {
            _rowControls.Clear();
            _visualRows.Clear();
            _itemTops.Clear();
            double y = 0;
            EveAttribute lastAttr = EveAttribute.None;
            bool anyRemap = _viewModel!.Items.Any(it => it.Entry.Remapping != null);
            var character = _viewModel.Plan?.Character;

            for (int i = 0; i < _viewModel.Items.Count; i++)
            {
                var item = _viewModel.Items[i];

                // ── Segment headers (schedule design) ──
                if (item.Entry.Remapping != null &&
                    item.Entry.Remapping.Status == RemappingPointStatus.UpToDate)
                {
                    var remap = item.Entry.Remapping;
                    var spread = new Dictionary<EveAttribute, long>
                    {
                        [EveAttribute.Perception] = remap[EveAttribute.Perception],
                        [EveAttribute.Willpower] = remap[EveAttribute.Willpower],
                        [EveAttribute.Intelligence] = remap[EveAttribute.Intelligence],
                        [EveAttribute.Memory] = remap[EveAttribute.Memory],
                        [EveAttribute.Charisma] = remap[EveAttribute.Charisma],
                    };
                    // The reorder-staleness contract: a remap is a promise about
                    // what FOLLOWS it. When manual moves change the block so its
                    // dominant pair no longer matches the spread's top two, the
                    // divider says so in amber instead of silently presiding over
                    // the wrong skills — and the fix is one click away in the
                    // stats-band verdict.
                    var top2 = spread.OrderByDescending(kv => kv.Value)
                        .Take(2).Select(kv => kv.Key).ToHashSet();
                    var dominant = DominantPair(i);
                    bool stale = dominant.Item1 != EveAttribute.None &&
                        !(top2.Contains(dominant.Item1) && top2.Contains(dominant.Item2));
                    Color accent = stale
                        ? Color.Parse("#FFE07B39") : Color.Parse("#FFE6A817");
                    string label = "◆ " + string.Format(
                        Loc.Get("PlanEditor.RemapBeforeBanner"),
                        item.DisplayName.ToUpperInvariant());
                    if (stale)
                        label += " — " + Loc.Get("PlanEditor.RemapStale");
                    _visualRows.Add(BuildSegmentDivider(label, spread, accent));
                    y += HeaderRowHeight;
                    _visualRows.Add(BuildSegmentHeader(
                        DominantPairLabel(i), SegmentStats(i), spread, accent));
                    y += HeaderRowHeight;
                }
                else if (i == 0 && anyRemap)
                {
                    // The mirror header: the leading block trains on CURRENT attributes.
                    Dictionary<EveAttribute, long>? current = null;
                    if (character != null)
                    {
                        current = new Dictionary<EveAttribute, long>();
                        foreach (var attr in new[] { EveAttribute.Perception,
                            EveAttribute.Willpower, EveAttribute.Intelligence,
                            EveAttribute.Memory, EveAttribute.Charisma })
                            current[attr] = character[attr].EffectiveValue;
                    }
                    _visualRows.Add(BuildSegmentHeader(
                        Loc.Get("PlanEditor.CurrentAttrBanner"), SegmentStats(0),
                        current, Color.Parse("#FF2BB5AD")));
                    y += HeaderRowHeight;
                }
                else if (_groupByAttribute && item.PrimaryAttribute != lastAttr)
                {
                    _visualRows.Add(BuildAttributeGroupHeader(item.PrimaryAttribute));
                    y += HeaderRowHeight;
                }
                lastAttr = item.PrimaryAttribute;

                var row = BuildRow(item, i);
                _rowControls.Add(row);
                _visualRows.Add(row);
                _itemTops.Add(y);
                y += RowHeight;
            }
            _itemTops.Add(y);   // sentinel: the slot after the last item

            // CRITICAL: Must set to null first, then to a NEW list.
            // Avalonia's ItemsControl won't re-render if the same object
            // reference is assigned to ItemsSource (it short-circuits).
            QueueList.ItemsSource = null;
            QueueList.ItemsSource = _visualRows.ToList();
        }

        /// <summary>The dominant primary/secondary pair over the segment starting at
        /// <paramref name="startIndex"/>, by training time.</summary>
        private (EveAttribute, EveAttribute) DominantPair(int startIndex)
        {
            var totals = new Dictionary<(EveAttribute, EveAttribute), long>();
            for (int i = startIndex; i < _viewModel!.Items.Count; i++)
            {
                if (i > startIndex && _viewModel.Items[i].Entry.Remapping != null)
                    break;
                var key = (_viewModel.Items[i].PrimaryAttribute,
                    _viewModel.Items[i].SecondaryAttribute);
                totals[key] = totals.GetValueOrDefault(key)
                    + _viewModel.Items[i].TrainingTime.Ticks;
            }
            if (totals.Count == 0) return (EveAttribute.None, EveAttribute.None);
            return totals.OrderByDescending(kv => kv.Value).First().Key;
        }

        private string DominantPairLabel(int startIndex)
        {
            var top = DominantPair(startIndex);
            return top.Item1 == EveAttribute.None
                ? string.Empty
                : $"{top.Item1} / {top.Item2}".ToUpperInvariant();
        }

        /// <summary>The gold "◆ REMAP BEFORE X" divider row with the spread's top
        /// two attributes as chips on the right.</summary>
        private Control BuildSegmentDivider(string label,
            Dictionary<EveAttribute, long> spread, Color accent)
        {
            var dock = new DockPanel();
            var chips = BuildAttrChips(spread, 2);
            DockPanel.SetDock(chips, Dock.Right);
            dock.Children.Add(chips);
            dock.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = FontScaleService.Small,
                FontWeight = FontWeight.SemiBold,
                Foreground = new SolidColorBrush(accent),
                VerticalAlignment = VerticalAlignment.Center,
            });
            return new Border
            {
                Height = HeaderRowHeight,
                Background = new SolidColorBrush(Color.FromArgb(26, accent.R, accent.G, accent.B)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(150, accent.R, accent.G, accent.B)),
                BorderThickness = new Thickness(1, 1, 1, 1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(12, 0),
                Margin = new Thickness(2, 3, 8, 0),
                Child = dock,
            };
        }

        /// <summary>A segment header row: pair label + "n skills · dur" left, top-two
        /// attribute chips right — teal for keep-current, gold for remapped blocks.</summary>
        private Control BuildSegmentHeader(string title, string stats,
            Dictionary<EveAttribute, long>? attrs, Color accent)
        {
            var dock = new DockPanel();
            if (attrs != null)
            {
                var chips = BuildAttrChips(attrs, 2);
                DockPanel.SetDock(chips, Dock.Right);
                dock.Children.Add(chips);
            }
            var left = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                VerticalAlignment = VerticalAlignment.Center,
            };
            left.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = FontScaleService.Small,
                FontWeight = FontWeight.SemiBold,
                Foreground = new SolidColorBrush(accent),
                VerticalAlignment = VerticalAlignment.Center,
            });
            left.Children.Add(new TextBlock
            {
                Text = stats.TrimStart(' ', '·', ' '),
                FontSize = FontScaleService.Caption,
                Foreground = new SolidColorBrush(
                    Color.FromArgb(190, accent.R, accent.G, accent.B)),
                VerticalAlignment = VerticalAlignment.Center,
            });
            dock.Children.Add(left);
            return new Border
            {
                Height = HeaderRowHeight,
                Padding = new Thickness(18, 0, 8, 0),
                Child = dock,
            };
        }

        /// <summary>The top-<paramref name="take"/> attributes as "MEM 30"-style chips.</summary>
        private Control BuildAttrChips(Dictionary<EveAttribute, long> attrs, int take)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6,
                VerticalAlignment = VerticalAlignment.Center,
            };
            foreach (var kv in attrs.OrderByDescending(kv => kv.Value).Take(take))
            {
                var (bg, fg) = GetAttrColors(kv.Key);
                panel.Children.Add(new Border
                {
                    Background = new SolidColorBrush(bg),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(8, 2),
                    Child = new TextBlock
                    {
                        Text = $"{AttrAbbrev(kv.Key)} {kv.Value}",
                        FontSize = FontScaleService.Caption,
                        FontWeight = FontWeight.Bold,
                        Foreground = new SolidColorBrush(fg),
                    },
                });
            }
            return panel;
        }

        private static string AttrAbbrev(EveAttribute attr) => attr switch
        {
            EveAttribute.Perception => "PER",
            EveAttribute.Willpower => "WIL",
            EveAttribute.Intelligence => "INT",
            EveAttribute.Memory => "MEM",
            EveAttribute.Charisma => "CHA",
            _ => "?",
        };

        /// <summary>Insertion slot for an absolute Y — replaces the uniform
        /// <c>Y / RowHeight</c> math, which real header rows made wrong.</summary>
        private int SlotFromY(double absoluteY)
        {
            for (int i = 0; i < _itemTops.Count - 1; i++)
            {
                if (absoluteY < _itemTops[i] + RowHeight / 2)
                    return i;
            }
            return _itemTops.Count - 1;
        }

        /// <summary>The Y of an insertion slot's indicator line.</summary>
        private double TopOfSlot(int slot) =>
            slot < _itemTops.Count ? _itemTops[slot] : _itemTops[^1];

        /// <summary>"· n skills · duration" for the segment starting at
        /// <paramref name="startIndex"/> and running to the next remap point.</summary>
        private string SegmentStats(int startIndex)
        {
            int count = 0;
            TimeSpan total = TimeSpan.Zero;
            for (int i = startIndex; i < _viewModel!.Items.Count; i++)
            {
                if (i > startIndex && _viewModel.Items[i].Entry.Remapping != null)
                    break;
                count++;
                total += _viewModel.Items[i].TrainingTime;
            }
            string dur = total.TotalDays >= 1
                ? $"{(int)total.TotalDays}d {total.Hours}h"
                : $"{(int)total.TotalHours}h {total.Minutes}m";
            return $" · {count} {Loc.Get("PlanEditor.Skills")} · {dur}";
        }

        private Control BuildRow(PlanQueueItem item, int index)
        {
            var chainColor = Color.Parse(item.ChainColor);
            var chainBrush = new SolidColorBrush(chainColor);
            bool selected = _selected.Contains(index);

            var normalBg = Brushes.Transparent;
            var hoverBg = new SolidColorBrush(Color.FromArgb(20, 74, 148, 240));
            var selectedBg = new SolidColorBrush(Color.FromArgb(45, 74, 148, 240));

            var container = new Border
            {
                Height = RowHeight,
                Background = selected ? selectedBg : normalBg,
                BorderBrush = selected ? new SolidColorBrush(Color.FromArgb(56, 74, 148, 240)) : Brushes.Transparent,
                BorderThickness = new Thickness(1.5),
                ClipToBounds = false,
                Cursor = new Cursor(StandardCursorType.Hand),
                RenderTransform = TransformOperations.Parse("scale(1,1)"),
                RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                Transitions = new global::Avalonia.Animation.Transitions
                {
                    new global::Avalonia.Animation.TransformOperationsTransition
                    {
                        Property = Border.RenderTransformProperty,
                        Duration = TimeSpan.FromMilliseconds(100),
                    },
                    new global::Avalonia.Animation.BrushTransition
                    {
                        Property = Border.BackgroundProperty,
                        Duration = TimeSpan.FromMilliseconds(150),
                    }
                },
            };

            ToolTip.SetTip(container, Loc.Get("Plan.DragToReorder"));
            ToolTip.SetShowDelay(container, 800);

            container.PointerEntered += (_, _) =>
            {
                if (!_selected.Contains(index))
                    container.Background = hoverBg;
            };
            container.PointerExited += (_, _) =>
            {
                if (!_selected.Contains(index))
                    container.Background = normalBg;
            };

            // Press: start tracking for threshold drag (whole row is draggable)
            container.PointerPressed += (_, e) =>
            {
                var props = e.GetCurrentPoint(this).Properties;
                if (!props.IsLeftButtonPressed) return;

                _dragPending = true;
                _dragPendingIndex = index;
                _dragPendingY = e.GetPosition(QueueScroller).Y;
                _dragPendingEvent = e;

                container.RenderTransform = TransformOperations.Parse("scale(0.98, 0.98)");

                e.Pointer.Capture(QueueScroller);
                QueueScroller.PointerMoved += OnPendingDragPointerMoved;
                QueueScroller.PointerReleased += OnPendingDragPointerReleased;
            };

            // Double-click for skill detail
            var capturedItem = item;
            container.DoubleTapped += (_, _) => SkillDoubleClicked?.Invoke(capturedItem);

            // Right-click context menu. Assigned as a property (not opened manually from
            // ContextRequested) — Avalonia's native open/close/click handling only works
            // reliably through the ContextMenu property. Rows are rebuilt on every refresh,
            // so menu state (remap flags, planned levels) stays current.
            if (ContextMenuFactory != null)
                container.ContextMenu = ContextMenuFactory(capturedItem);

            // 7-column grid: skill | time | R | PRI | SEC | SP/HR | LEVEL
            var grid = new Grid
            {
                ColumnDefinitions = ColumnDefinitions.Parse("*,100,40,75,75,55,60"),
                Margin = new Thickness(18, 0, 8, 0),
            };

            // ── Chain ribbon (left edge) ──
            var ribbon = new Border
            {
                Width = 3,
                Background = chainBrush,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Stretch,
                Margin = new Thickness(-18, 0, 0, 0),
            };
            switch (item.ChainPosition)
            {
                case ChainPosition.First:
                    ribbon.CornerRadius = new CornerRadius(2, 2, 0, 0);
                    ribbon.Margin = new Thickness(-18, RowHeight / 2, 0, 0);
                    break;
                case ChainPosition.Last:
                    ribbon.CornerRadius = new CornerRadius(0, 0, 2, 2);
                    ribbon.Margin = new Thickness(-18, 0, 0, RowHeight / 2);
                    break;
                case ChainPosition.Solo:
                    ribbon.CornerRadius = new CornerRadius(2);
                    ribbon.Margin = new Thickness(-18, RowHeight * 0.2, 0, RowHeight * 0.2);
                    break;
                default:
                    ribbon.Margin = new Thickness(-18, 0, 0, 0);
                    break;
            }

            // ── Col 0: Skill name with omega badge ──
            var namePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5,
                VerticalAlignment = VerticalAlignment.Center,
            };
            // Editor affordance: the drag handle appears on hover — the whole row
            // was always draggable, but nothing SAID so.
            var dragHandle = Services.Codicon.Icon(Services.Codicon.Gripper,
                FontScaleService.Small, new SolidColorBrush(Color.Parse("#FF707088")));
            dragHandle.Opacity = 0;
            dragHandle.Width = 14;
            namePanel.Children.Add(dragHandle);
            container.PointerEntered += (_, _) => dragHandle.Opacity = 0.8;
            container.PointerExited += (_, _) => dragHandle.Opacity = 0;
            if (item.OmegaRequired)
            {
                namePanel.Children.Add(new TextBlock
                {
                    Text = "\u03A9",
                    FontSize = FontScaleService.Caption,
                    Foreground = chainBrush,
                    VerticalAlignment = VerticalAlignment.Center,
                });
            }
            // White typography (the chain color lives on the ribbon and the omega
            // badge): whole rows of magenta/green text read as a syntax error, not
            // a plan.
            namePanel.Children.Add(new TextBlock
            {
                Text = item.DisplayName,
                FontSize = FontScaleService.Body,
                FontWeight = item.IsGoal ? FontWeight.Medium : FontWeight.Normal,
                Foreground = (IBrush)Application.Current!.FindResource("EveTextPrimaryBrush")!,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
            });
            Grid.SetColumn(namePanel, 0);

            // ── Col 1: Time (with color for long durations) ──
            var timeColor = item.TimeSeverity switch
            {
                TimeSeverity.Massive => Color.Parse("#E84A4A"),
                TimeSeverity.Long => Color.Parse("#E6A632"),
                _ => Color.Parse("#F0F0F0"),
            };
            var timeText = new TextBlock
            {
                Text = item.TimeText,
                FontSize = FontScaleService.Body,
                Foreground = new SolidColorBrush(timeColor),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(timeText, 1);

            // ── Col 2: Rank ──
            var rankText = new TextBlock
            {
                Text = item.Rank.ToString(),
                FontSize = FontScaleService.Body,
                Foreground = (IBrush)Application.Current!.FindResource("EveTextSecondaryBrush")!,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(rankText, 2);

            // ── Col 3: Primary attribute (full name, colored pill) ──
            var priPill = BuildFullAttrPill(item.PrimaryAttribute);
            Grid.SetColumn(priPill, 3);

            // ── Col 4: Secondary attribute (full name, colored pill) ──
            var secPill = BuildFullAttrPill(item.SecondaryAttribute);
            Grid.SetColumn(secPill, 4);

            // ── Col 5: SP/HR ──
            var spText = new TextBlock
            {
                Text = item.SkillPointsPerHour.ToString("N0"),
                FontSize = FontScaleService.Body,
                Foreground = (IBrush)Application.Current!.FindResource("EveTextSecondaryBrush")!,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(spText, 5);

            // ── Col 6: Level blocks (standard 10x12, matches Skills tab) ──
            var accentColor = Color.Parse("#FFE6A817");
            var trainedBrush = new SolidColorBrush(accentColor);
            var plannedBrush = new SolidColorBrush(new Color(80, accentColor.R, accentColor.G, accentColor.B));
            var emptyBrush = new SolidColorBrush(new Color(30, accentColor.R, accentColor.G, accentColor.B));

            var pipsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 2,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
            };
            for (int lvl = 1; lvl <= 5; lvl++)
            {
                IBrush fill;
                if (lvl <= item.TrainedLevel)
                    fill = trainedBrush;
                else if (lvl <= item.Level)
                    fill = plannedBrush;
                else
                    fill = emptyBrush;

                var pip = new Border
                {
                    Width = 9,
                    Height = 10,
                    CornerRadius = new CornerRadius(2),
                    Background = fill,
                    Cursor = new Cursor(StandardCursorType.Hand),
                };
                // Editable, not just readable: clicking pip N retargets the plan to
                // level N (up plans prerequisites in, down cascades dependents out).
                int targetLevel = lvl;
                var pipItem = item;
                pip.PointerPressed += (_, pe) =>
                {
                    if (!pe.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
                    pe.Handled = true;   // a pip click edits; it must not start a drag
                    LevelChangeRequested?.Invoke(pipItem, targetLevel);
                };
                ToolTip.SetTip(pip, string.Format(
                    Loc.Get("PlanEditor.SetTargetLevelFmt"), targetLevel));
                pipsPanel.Children.Add(pip);
            }
            Grid.SetColumn(pipsPanel, 6);

            grid.Children.Add(ribbon);
            grid.Children.Add(namePanel);
            grid.Children.Add(timeText);
            grid.Children.Add(rankText);
            grid.Children.Add(priPill);
            grid.Children.Add(secPill);
            grid.Children.Add(spText);
            grid.Children.Add(pipsPanel);

            container.Child = grid;
            return container;
        }

        private static (Color bg, Color fg) GetAttrColors(EveAttribute attr) => attr switch
        {
            EveAttribute.Memory => (Color.FromArgb(30, 74, 148, 240), Color.Parse("#6AAEF0")),
            EveAttribute.Perception => (Color.FromArgb(30, 68, 195, 106), Color.Parse("#54D878")),
            EveAttribute.Charisma => (Color.FromArgb(30, 230, 166, 50), Color.Parse("#E6A632")),
            EveAttribute.Willpower => (Color.FromArgb(30, 170, 136, 250), Color.Parse("#AA88FA")),
            EveAttribute.Intelligence => (Color.FromArgb(30, 240, 240, 240), Color.Parse("#DCDCDC")),
            _ => (Color.FromArgb(30, 128, 128, 128), Color.Parse("#808080")),
        };

        private static Control BuildAttributeGroupHeader(EveAttribute attr)
        {
            var (bg, fg) = attr switch
            {
                EveAttribute.Memory => (Color.FromArgb(40, 74, 148, 240), Color.Parse("#6AAEF0")),
                EveAttribute.Perception => (Color.FromArgb(40, 68, 195, 106), Color.Parse("#54D878")),
                EveAttribute.Charisma => (Color.FromArgb(40, 230, 166, 50), Color.Parse("#E6A632")),
                EveAttribute.Willpower => (Color.FromArgb(40, 170, 136, 250), Color.Parse("#AA88FA")),
                EveAttribute.Intelligence => (Color.FromArgb(40, 240, 240, 240), Color.Parse("#DCDCDC")),
                _ => (Color.FromArgb(40, 128, 128, 128), Color.Parse("#808080")),
            };

            return new Border
            {
                Height = 28,
                Background = new SolidColorBrush(bg),
                Padding = new Thickness(18, 4),
                Margin = new Thickness(0, 4, 0, 0),
                Child = new TextBlock
                {
                    Text = Loc.Get($"Eve.{attr}"),
                    FontSize = FontScaleService.Body,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = new SolidColorBrush(fg),
                    VerticalAlignment = VerticalAlignment.Center,
                },
            };
        }

        private static Border BuildFullAttrPill(EveAttribute attr)
        {
            string text = attr.ToString();
            var (bg, fg) = attr switch
            {
                EveAttribute.Memory => (Color.FromArgb(30, 74, 148, 240), Color.Parse("#6AAEF0")),
                EveAttribute.Perception => (Color.FromArgb(30, 68, 195, 106), Color.Parse("#54D878")),
                EveAttribute.Charisma => (Color.FromArgb(30, 230, 166, 50), Color.Parse("#E6A632")),
                EveAttribute.Willpower => (Color.FromArgb(30, 170, 136, 250), Color.Parse("#AA88FA")),
                EveAttribute.Intelligence => (Color.FromArgb(30, 240, 240, 240), Color.Parse("#DCDCDC")),
                _ => (Color.FromArgb(30, 128, 128, 128), Color.Parse("#808080")),
            };

            return new Border
            {
                Background = new SolidColorBrush(bg),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 2),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = text,
                    FontSize = FontScaleService.Caption,
                    Foreground = new SolidColorBrush(fg),
                }
            };
        }

        #endregion

        #region Selection

        private void HandleRowClick(int index, PointerPressedEventArgs e)
        {
            var props = e.GetCurrentPoint(this).Properties;
            if (!props.IsLeftButtonPressed) return;

            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift) && _lastClickIndex >= 0)
            {
                int lo = Math.Min(_lastClickIndex, index);
                int hi = Math.Max(_lastClickIndex, index);
                for (int i = lo; i <= hi; i++)
                    _selected.Add(i);
            }
            else if (e.KeyModifiers.HasFlag(KeyModifiers.Control) ||
                     e.KeyModifiers.HasFlag(KeyModifiers.Meta))
            {
                if (_selected.Contains(index))
                    _selected.Remove(index);
                else
                    _selected.Add(index);
            }
            else
            {
                _selected.Clear();
                _selected.Add(index);
            }

            _lastClickIndex = index;
            UpdateSelectionVisuals();
            SelectionChanged?.Invoke(_selected.Count == 1 && _viewModel != null
                ? _viewModel.Items[_selected.First()] : null);
        }

        /// <summary>
        /// Updates row visuals to reflect selection state without rebuilding.
        /// This avoids destroying controls, which would break DoubleTapped events.
        /// </summary>
        private void UpdateSelectionVisuals()
        {
            var selectedBg = new SolidColorBrush(Color.FromArgb(45, 74, 148, 240));
            var selectedBorder = new SolidColorBrush(Color.FromArgb(56, 74, 148, 240));
            var normalBorder = Brushes.Transparent;

            for (int i = 0; i < _rowControls.Count; i++)
            {
                if (_rowControls[i] is Border b)
                {
                    bool sel = _selected.Contains(i);
                    b.Background = sel ? selectedBg : Brushes.Transparent;
                    b.BorderBrush = sel ? selectedBorder : normalBorder;
                }
            }
        }

        #endregion

        #region Drag Reorder (Pointer Capture + RenderTransform)

        // Threshold drag handlers: track press, promote to drag after 5px movement
        private void OnPendingDragPointerMoved(object? sender, PointerEventArgs e)
        {
            if (!_dragPending) return;

            double currentY = e.GetPosition(QueueScroller).Y;
            double delta = Math.Abs(currentY - _dragPendingY);

            if (delta < DragThreshold) return;

            // Promote to real drag
            _dragPending = false;
            QueueScroller.PointerMoved -= OnPendingDragPointerMoved;
            QueueScroller.PointerReleased -= OnPendingDragPointerReleased;

            // Ensure pressed row is selected
            if (!_selected.Contains(_dragPendingIndex))
            {
                _selected.Clear();
                _selected.Add(_dragPendingIndex);
                UpdateSelectionVisuals();
            }

            PromoteToDrag(_dragPendingIndex, _dragPendingY, e);
        }

        private void OnPendingDragPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            // Did not exceed threshold — treat as a click
            _dragPending = false;
            QueueScroller.PointerMoved -= OnPendingDragPointerMoved;
            QueueScroller.PointerReleased -= OnPendingDragPointerReleased;
            e.Pointer.Capture(null);

            // Restore scale on the row
            if (_dragPendingIndex >= 0 && _dragPendingIndex < _rowControls.Count)
                _rowControls[_dragPendingIndex].RenderTransform = TransformOperations.Parse("scale(1, 1)");

            // Process as selection click
            if (_dragPendingEvent != null)
                HandleRowClick(_dragPendingIndex, _dragPendingEvent);

            _dragPendingEvent = null;
            _dragPendingIndex = -1;
        }

        private void PromoteToDrag(int index, double startY, PointerEventArgs e)
        {
            _isDragging = true;
            _dragStartIndex = index;
            _dragStartY = startY + QueueScroller.Offset.Y;
            _currentInsertSlot = -1;

            // Ghost the selected rows
            foreach (int si in _selected)
            {
                if (si < _rowControls.Count)
                {
                    _rowControls[si].Opacity = 0.15;
                    if (_rowControls[si] is Border b)
                    {
                        b.BorderBrush = new SolidColorBrush(Color.Parse("#323C4A"));
                        b.BorderThickness = new Thickness(1);
                        b.Background = new SolidColorBrush(Color.FromArgb(8, 255, 255, 255));
                    }
                }
            }

            // Create ghost badge
            int count = _selected.Count;
            string label = count == 1
                ? _viewModel!.Items[index].DisplayName
                : $"{count} skills";
            _dragGhost = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#CC1A1F2E")),
                BorderBrush = new SolidColorBrush(Color.Parse("#4A94F0")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 6),
                IsHitTestVisible = false,
                Child = new TextBlock
                {
                    Text = label,
                    FontSize = FontScaleService.Small,
                    Foreground = new SolidColorBrush(Color.Parse("#4A94F0")),
                    FontWeight = FontWeight.SemiBold,
                },
            };
            double viewportY = e.GetPosition(QueueScroller).Y;
            Canvas.SetLeft(_dragGhost, 40);
            Canvas.SetTop(_dragGhost, viewportY - 16);
            DragCanvas.Children.Add(_dragGhost);
            DragCanvas.IsHitTestVisible = false;

            e.Pointer.Capture(QueueScroller);
            QueueScroller.PointerMoved += OnDragPointerMoved;
            QueueScroller.PointerReleased += OnDragPointerReleased;

            InsertIndicator.IsVisible = false;
        }

        private void OnDragPointerMoved(object? sender, PointerEventArgs e)
        {
            if (!_isDragging || _viewModel == null) return;

            double viewportY = e.GetPosition(QueueScroller).Y;
            double absoluteY = viewportY + QueueScroller.Offset.Y;
            int totalItems = _viewModel.Items.Count;

            if (_dragGhost != null)
                Canvas.SetTop(_dragGhost, viewportY - 16);

            // Auto-scroll when dragging near edges
            double scrollZone = 40;
            double viewportHeight = QueueScroller.Bounds.Height;
            if (viewportY < scrollZone)
            {
                double speed = (scrollZone - viewportY) / scrollZone * 8;
                QueueScroller.Offset = new Vector(0, Math.Max(0, QueueScroller.Offset.Y - speed));
            }
            else if (viewportY > viewportHeight - scrollZone)
            {
                double speed = (viewportY - (viewportHeight - scrollZone)) / scrollZone * 8;
                double maxScroll = QueueScroller.Extent.Height - viewportHeight;
                QueueScroller.Offset = new Vector(0, Math.Min(maxScroll, QueueScroller.Offset.Y + speed));
            }

            int slot = SlotFromY(absoluteY);
            slot = Math.Max(0, Math.Min(totalItems, slot));

            if (slot == _currentInsertSlot) return;
            _currentInsertSlot = slot;

            var indices = _selected.OrderBy(i => i).ToList();
            bool valid = _viewModel.CanMove(indices, slot);

            double indicatorViewportY = TopOfSlot(slot) - QueueScroller.Offset.Y - 1;
            InsertIndicator.IsVisible = true;
            Canvas.SetLeft(InsertIndicator, 18);
            Canvas.SetTop(InsertIndicator, indicatorViewportY);
            InsertIndicator.Width = Math.Max(100, QueueScroller.Bounds.Width - 26);
            InsertIndicator.Background = valid
                ? new SolidColorBrush(Color.Parse("#4A94F0"))
                : new SolidColorBrush(Color.Parse("#E84A4A"));
        }

        private void CancelDrag()
        {
            if (!_isDragging) return;

            QueueScroller.PointerMoved -= OnDragPointerMoved;
            QueueScroller.PointerReleased -= OnDragPointerReleased;

            if (_dragGhost != null)
            {
                DragCanvas.Children.Remove(_dragGhost);
                _dragGhost = null;
            }

            foreach (var row in _rowControls)
            {
                row.Opacity = 1.0;
                row.RenderTransform = null;
            }

            InsertIndicator.IsVisible = false;
            _isDragging = false;
            _dragStartIndex = -1;
            _currentInsertSlot = -1;

            ShowToast(Loc.Get("Plan.DragCancelled"), false);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.Escape && _isDragging)
            {
                CancelDrag();
                e.Handled = true;
            }
        }

        private void OnDragPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (!_isDragging || _viewModel == null) return;

            // Right-click cancels drag
            if (e.InitialPressMouseButton == MouseButton.Right)
            {
                e.Pointer.Capture(null);
                CancelDrag();
                return;
            }

            QueueScroller.PointerMoved -= OnDragPointerMoved;
            QueueScroller.PointerReleased -= OnDragPointerReleased;
            e.Pointer.Capture(null);

            if (_dragGhost != null)
            {
                DragCanvas.Children.Remove(_dragGhost);
                _dragGhost = null;
            }

            foreach (var row in _rowControls)
            {
                row.Opacity = 1.0;
                row.RenderTransform = null;
            }

            InsertIndicator.IsVisible = false;

            if (_currentInsertSlot >= 0)
            {
                var indices = _selected.OrderBy(i => i).ToList();
                if (_viewModel.CanMove(indices, _currentInsertSlot))
                {
                    var newIndices = _viewModel.PerformMove(indices, _currentInsertSlot);
                    _selected.Clear();
                    foreach (int ni in newIndices)
                        _selected.Add(ni);

                    Rebuild();
                    Reordered?.Invoke();
                    ShowToast(Loc.Get("Plan.QueueReordered"), false);
                }
                else
                {
                    string reason = _viewModel.GetBlockingReason(indices, _currentInsertSlot)
                        ?? "Prerequisite constraints block this move";
                    ShowToast(reason, true);
                }
            }

            _isDragging = false;
            _dragStartIndex = -1;
            _currentInsertSlot = -1;
        }

        #endregion

        #region Minimap

        private void BuildMinimap()
        {
            MinimapItems.Children.Clear();
            if (_viewModel == null || _viewModel.Items.Count == 0) return;

            double totalHours = _viewModel.Items.Sum(i => i.TrainingTime.TotalHours);
            if (totalHours <= 0) return;

            foreach (var item in _viewModel.Items)
            {
                double pct = Math.Max(0.2, (item.TrainingTime.TotalHours / totalHours) * 100);
                var seg = new Border
                {
                    Background = new SolidColorBrush(Color.Parse(item.ChainColor)),
                    // Percentage width via binding would be cleaner but this works for code-behind
                };

                // We need to set width after the minimap has a known width
                seg.Tag = pct;
                MinimapItems.Children.Add(seg);
            }

            // Defer width calculation until layout
            Dispatcher.UIThread.Post(() =>
            {
                double barWidth = MinimapBar.Bounds.Width;
                if (barWidth <= 0) barWidth = 600;

                foreach (var child in MinimapItems.Children)
                {
                    if (child is Border b && b.Tag is double pct)
                    {
                        b.Width = Math.Max(1, barWidth * pct / 100);
                    }
                }
            }, DispatcherPriority.Render);
        }

        #endregion

        #region Legend

        private void BuildLegend()
        {
            LegendPanel.Children.Clear();
            if (_viewModel == null) return;

            foreach (var chain in _viewModel.GetOrderedChains())
            {
                var chip = new Border
                {
                    Background = new SolidColorBrush(Color.Parse("#181E28")),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(10, 4),
                    Margin = new Thickness(0, 0, 4, 0),
                    BorderBrush = Brushes.Transparent,
                    BorderThickness = new Thickness(0.5),
                };

                var stack = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 5,
                };
                stack.Children.Add(new Border
                {
                    Width = 8, Height = 8,
                    CornerRadius = new CornerRadius(2),
                    Background = new SolidColorBrush(Color.Parse(chain.Color)),
                });
                stack.Children.Add(new TextBlock
                {
                    Text = chain.GoalSkillName,
                    FontSize = FontScaleService.Small,
                    Foreground = (IBrush)Application.Current!.FindResource("EveTextSecondaryBrush")!,
                });

                chip.Child = stack;
                LegendPanel.Children.Add(chip);
            }
        }

        #endregion

        #region Toast

        private System.Threading.CancellationTokenSource? _toastCts;

        private async void ShowToast(string message, bool isError)
        {
            _toastCts?.Cancel();
            _toastCts = new System.Threading.CancellationTokenSource();
            var token = _toastCts.Token;

            ToastText.Text = message;
            ToastBorder.Background = isError
                ? new SolidColorBrush(Color.Parse("#E84A4A"))
                : new SolidColorBrush(Color.Parse("#4A94F0"));
            ToastBorder.IsVisible = true;

            try
            {
                await System.Threading.Tasks.Task.Delay(2000, token);
                if (!token.IsCancellationRequested)
                    ToastBorder.IsVisible = false;
            }
            catch (System.Threading.Tasks.TaskCanceledException) { }
        }

        #endregion

        public IReadOnlySet<int> SelectedIndices => _selected;

        public List<int> GetSelectedIndicesSorted() => _selected.OrderBy(i => i).ToList();

        public void SetSelection(IEnumerable<int> indices)
        {
            _selected.Clear();
            foreach (int i in indices)
                _selected.Add(i);
            UpdateSelectionVisuals();
        }
    }
}
