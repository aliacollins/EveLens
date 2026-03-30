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
using Avalonia.Media;
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
        private const double RowHeight = 36;

        // Drag state
        private bool _isDragging;
        private int _dragStartIndex = -1;
        private double _dragStartY;
        private int _currentInsertSlot = -1;
        private readonly List<Control> _rowControls = new();
        private Border? _dragGhost;

        /// <summary>
        /// Event raised when skills are reordered via drag.
        /// </summary>
        public event Action? Reordered;

        /// <summary>
        /// Event raised when a skill is double-clicked (for sidebar detail).
        /// </summary>
        public event Action<PlanQueueItem>? SkillDoubleClicked;

        public PlanQueueListControl()
        {
            InitializeComponent();
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

        private void BuildRows()
        {
            _rowControls.Clear();

            for (int i = 0; i < _viewModel!.Items.Count; i++)
            {
                var item = _viewModel.Items[i];
                var row = BuildRow(item, i);
                _rowControls.Add(row);
            }

            // CRITICAL: Must set to null first, then to a NEW list.
            // Avalonia's ItemsControl won't re-render if the same object
            // reference is assigned to ItemsSource (it short-circuits).
            QueueList.ItemsSource = null;
            QueueList.ItemsSource = _rowControls.ToList();
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
            };

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

            // Click for selection
            container.PointerPressed += (_, e) =>
            {
                if (e.Source is Control c && c.Tag?.ToString() == "grip")
                    return; // grip handles drag start

                HandleRowClick(index, e);
                e.Handled = true;
            };

            // Double-click for skill detail
            container.DoubleTapped += (_, _) => SkillDoubleClicked?.Invoke(item);

            // 8-column grid matching old design: grip | skill | time | R | PRI | SEC | SP/HR | LEVEL
            var grid = new Grid
            {
                ColumnDefinitions = ColumnDefinitions.Parse("24,*,100,40,75,75,55,60"),
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

            // ── Col 0: Grip handle ──
            var grip = new TextBlock
            {
                Text = "\u2807",
                FontSize = FontScaleService.Body,
                Foreground = selected
                    ? new SolidColorBrush(Color.Parse("#4A94F0"))
                    : new SolidColorBrush(Color.Parse("#323C4A")),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = new Cursor(StandardCursorType.SizeAll),
                Tag = "grip",
            };
            grip.PointerPressed += (_, e) =>
            {
                if (!_selected.Contains(index))
                {
                    _selected.Clear();
                    _selected.Add(index);
                    Rebuild();
                }
                StartDrag(index, e);
                e.Handled = true;
            };
            Grid.SetColumn(grip, 0);

            // ── Col 1: Skill name with omega badge ──
            var namePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5,
                VerticalAlignment = VerticalAlignment.Center,
            };
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
            namePanel.Children.Add(new TextBlock
            {
                Text = item.DisplayName,
                FontSize = FontScaleService.Body,
                FontWeight = item.IsGoal ? FontWeight.Medium : FontWeight.Normal,
                Foreground = chainBrush,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
            });
            Grid.SetColumn(namePanel, 1);

            // ── Col 2: Time (with color for long durations) ──
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
            Grid.SetColumn(timeText, 2);

            // ── Col 3: Rank ──
            var rankText = new TextBlock
            {
                Text = item.Rank.ToString(),
                FontSize = FontScaleService.Body,
                Foreground = (IBrush)Application.Current!.FindResource("EveTextSecondaryBrush")!,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(rankText, 3);

            // ── Col 4: Primary attribute (full name, colored pill) ──
            var priPill = BuildFullAttrPill(item.PrimaryAttribute);
            Grid.SetColumn(priPill, 4);

            // ── Col 5: Secondary attribute (full name, colored pill) ──
            var secPill = BuildFullAttrPill(item.SecondaryAttribute);
            Grid.SetColumn(secPill, 5);

            // ── Col 6: SP/HR ──
            var spText = new TextBlock
            {
                Text = item.SkillPointsPerHour.ToString("N0"),
                FontSize = FontScaleService.Body,
                Foreground = (IBrush)Application.Current!.FindResource("EveTextSecondaryBrush")!,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(spText, 6);

            // ── Col 7: Level pips (old style — wider blocks) ──
            var pipsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 2,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
            };
            for (int lvl = 1; lvl <= 5; lvl++)
            {
                bool trained = lvl <= item.TrainedLevel;
                bool queued = !trained && lvl <= item.Level;

                pipsPanel.Children.Add(new Border
                {
                    Width = 8, Height = 12,
                    CornerRadius = new CornerRadius(2),
                    Background = trained
                        ? new SolidColorBrush(Color.Parse("#D4A020"))
                        : queued
                            ? new SolidColorBrush(Color.FromArgb(56, 212, 160, 32))
                            : Brushes.Transparent,
                    BorderBrush = trained || queued
                        ? new SolidColorBrush(Color.Parse("#D4A020"))
                        : new SolidColorBrush(Color.Parse("#323C4A")),
                    BorderThickness = new Thickness(1),
                });
            }
            Grid.SetColumn(pipsPanel, 7);

            grid.Children.Add(ribbon);
            grid.Children.Add(grip);
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
            Rebuild();
        }

        #endregion

        #region Drag Reorder (Pointer Capture + RenderTransform)

        private void StartDrag(int index, PointerPressedEventArgs e)
        {
            _isDragging = true;
            _dragStartIndex = index;
            _dragStartY = e.GetPosition(QueueScroller).Y;
            _currentInsertSlot = -1;

            // Dim selected rows
            foreach (int si in _selected)
            {
                if (si < _rowControls.Count)
                    _rowControls[si].Opacity = 0.3;
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
            Canvas.SetLeft(_dragGhost, 40);
            Canvas.SetTop(_dragGhost, _dragStartY - 16);
            DragCanvas.Children.Add(_dragGhost);
            DragCanvas.IsHitTestVisible = false; // keep it as overlay only

            // Capture on the scroller so we get reliable move events
            e.Pointer.Capture(QueueScroller);
            QueueScroller.PointerMoved += OnDragPointerMoved;
            QueueScroller.PointerReleased += OnDragPointerReleased;

            InsertIndicator.IsVisible = false;
        }

        private void OnDragPointerMoved(object? sender, PointerEventArgs e)
        {
            if (!_isDragging || _viewModel == null) return;

            double currentY = e.GetPosition(QueueScroller).Y;
            int totalItems = _viewModel.Items.Count;

            // Move ghost badge
            if (_dragGhost != null)
                Canvas.SetTop(_dragGhost, currentY - 16);

            // Determine slot
            int slot = (int)Math.Round(currentY / RowHeight);
            slot = Math.Max(0, Math.Min(totalItems, slot));

            if (slot == _currentInsertSlot) return;
            _currentInsertSlot = slot;

            var indices = _selected.OrderBy(i => i).ToList();
            bool valid = _viewModel.CanMove(indices, slot);

            // Position indicator
            InsertIndicator.IsVisible = true;
            Canvas.SetLeft(InsertIndicator, 18);
            Canvas.SetTop(InsertIndicator, slot * RowHeight - 1);
            InsertIndicator.Width = Math.Max(100, QueueScroller.Bounds.Width - 26);
            InsertIndicator.Background = valid
                ? new SolidColorBrush(Color.Parse("#4A94F0"))
                : new SolidColorBrush(Color.Parse("#E84A4A"));
        }

        private void OnDragPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (!_isDragging || _viewModel == null) return;

            QueueScroller.PointerMoved -= OnDragPointerMoved;
            QueueScroller.PointerReleased -= OnDragPointerReleased;
            e.Pointer.Capture(null);

            // Remove ghost
            if (_dragGhost != null)
            {
                DragCanvas.Children.Remove(_dragGhost);
                _dragGhost = null;
            }

            // Restore opacity
            foreach (var row in _rowControls)
                row.Opacity = 1.0;

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
                    ShowToast("Queue reordered", false);
                }
                else
                {
                    ShowToast("Prerequisite constraint blocks this", true);
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
    }
}
