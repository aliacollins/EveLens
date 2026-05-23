// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using EveLens.Avalonia.Converters;
using EveLens.Common;
using EveLens.Common.Enumerations;
using EveLens.Common.Events;
using EveLens.Common.Helpers;
using EveLens.Common.Models;
using EveLens.Common.Service;
using EveLens.Common.Services;
using EveLens.Common.SettingsObjects;
using EveLens.Common.ViewModels;
using EveLens.Avalonia.Views.Dialogs;
using EveLens.Core.Events;
using EveLens.Avalonia.Services;
namespace EveLens.Avalonia.Views.CharacterMonitor
{
    public partial class CharacterOverviewView : UserControl
    {
        private IDisposable? _fetchCompletedSub;
        private IDisposable? _secondTickSub;
        private IDisposable? _collectionChangedSub;
        private IDisposable? _privacyModeSub;
        private IDisposable? _settingsChangedSub;
        private IDisposable? _fontScaleSub;

        // Debug: when true, cards cycle through all queue health states for visual testing
        private bool _debugQueueTints;

        // Track previous ISK/SP values per character for flash-on-change
        private readonly Dictionary<long, decimal> _prevBalances = new();
        private readonly Dictionary<long, long> _prevSkillPoints = new();

        // Track animation timers so we can stop them on detach/rebuild
        private readonly List<DispatcherTimer> _animationTimers = new();

        // Toggle for fetching dot pulse (alternates each second tick)
        private bool _fetchingDotBright;

        // Reorder mode — when active, cards are draggable instead of clickable
        private bool _reorderMode;
        private Button? _reorderToggleBtn;

        // Only animate the staggered fade-in on first application load
        private bool _initialLoadDone;

        // Track collapsed groups for expand/collapse persistence
        private const string OverviewCollapseKey = "OverviewGroups";
        private HashSet<string> _collapsedGroups = new(StringComparer.Ordinal);

        // Drag-to-reorder state
        private bool _isDragging;
        private bool _dragPending;
        private double _dragStartX, _dragStartY;
        private int _dragStartIndex = -1;
        private WrapPanel? _dragSourcePanel;
        private Button? _draggedCard;
        private Border? _dragGhost;
        private Border? _insertIndicator;
        private int _currentInsertIndex = -1;
        private string? _dragGroupName;
        private const double DragThreshold = 8.0;
        private const double CardWidth = 308.0; // 300 + 8 margin


        public CharacterOverviewView()
        {
            InitializeComponent();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            _fetchCompletedSub ??= AppServices.EventAggregator?.Subscribe<MonitorFetchCompletedEvent>(
                evt => Dispatcher.UIThread.Post(() => OnFetchCompleted(evt)));

            _secondTickSub ??= AppServices.EventAggregator?.Subscribe<SecondTickEvent>(
                _ => Dispatcher.UIThread.Post(OnSecondTick));

            _collectionChangedSub ??= AppServices.EventAggregator?.Subscribe<MonitoredCharacterCollectionChangedEvent>(
                _ => Dispatcher.UIThread.Post(OnCollectionChanged));

            _privacyModeSub ??= AppServices.EventAggregator?.Subscribe<PrivacyModeChangedEvent>(
                _ => Dispatcher.UIThread.Post(LoadData));

            _settingsChangedSub ??= AppServices.EventAggregator?.Subscribe<Common.Events.SettingsChangedEvent>(
                _ => Dispatcher.UIThread.Post(LoadData));

            _fontScaleSub ??= AppServices.EventAggregator?.Subscribe<Common.Events.FontScaleChangedEvent>(
                _ => Dispatcher.UIThread.Post(LoadData));

            LoadData();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            foreach (var t in _animationTimers)
                t.Stop();
            _animationTimers.Clear();

            _fetchCompletedSub?.Dispose();
            _fetchCompletedSub = null;
            _secondTickSub?.Dispose();
            _secondTickSub = null;
            _collectionChangedSub?.Dispose();
            _collectionChangedSub = null;
            _privacyModeSub?.Dispose();
            _privacyModeSub = null;
            _settingsChangedSub?.Dispose();
            _settingsChangedSub = null;
            _fontScaleSub?.Dispose();
            _fontScaleSub = null;

            base.OnDetachedFromVisualTree(e);
        }

        /// <summary>
        /// Public method to refresh the view (called after group changes).
        /// </summary>
        public void RefreshView()
        {
            LoadData();
        }

        #region Initial Load

        private void LoadData()
        {
            try
            {
                foreach (var t in _animationTimers)
                    t.Stop();
                _animationTimers.Clear();

                OverviewPanel.Children.Clear();

                // Reorder mode toolbar
                BuildReorderToolbar();

                // Load persisted collapse state — we store collapsed group names
                _collapsedGroups = CollapseStateHelper.LoadExpandState(0, OverviewCollapseKey);
                _prevBalances.Clear();
                _prevSkillPoints.Clear();

                var characters = AppServices.Characters.Where(c => c.Monitored).ToList();

                if (characters.Count == 0)
                {
                    BuildWelcomeState();
                    _initialLoadDone = true;
                    return;
                }

                var groups = Settings.CharacterGroups;
                int cardIndex = 0;

                if (groups.Count > 0)
                {
                    var assignedGuids = new HashSet<Guid>();

                    foreach (var group in groups)
                    {
                        // Iterate CharacterGuids in order to respect user-defined ordering
                        var groupChars = new List<Character>();
                        foreach (var guid in group.CharacterGuids)
                        {
                            var ch = characters.FirstOrDefault(c => c.Guid == guid);
                            if (ch != null) groupChars.Add(ch);
                        }

                        if (groupChars.Count == 0) continue;

                        foreach (var guid in group.CharacterGuids)
                            assignedGuids.Add(guid);

                        BuildGroupSection(group.Name, groupChars, ref cardIndex);
                    }

                    var ungrouped = characters.Where(c => !assignedGuids.Contains(c.Guid)).ToList();
                    if (ungrouped.Count > 0)
                    {
                        ungrouped = SortByUngroupedOrder(ungrouped);
                        BuildGroupSection("Ungrouped", ungrouped, ref cardIndex);
                    }

                    // Ghost card after the last group
                    var ghostWrap = new WrapPanel { Orientation = Orientation.Horizontal };
                    ghostWrap.Children.Add(BuildGhostCard());
                    OverviewPanel.Children.Add(ghostWrap);
                }
                else
                {
                    characters = SortByUngroupedOrder(characters);
                    var wrap = BuildCardWrapPanel(characters, ref cardIndex, includeGhostCard: true, groupName: "Ungrouped");
                    OverviewPanel.Children.Add(wrap);
                }

                // Seed balance/SP tracking
                foreach (var c in characters)
                {
                    _prevBalances[c.CharacterID] = c.Balance;
                    _prevSkillPoints[c.CharacterID] = c.SkillPoints;
                }

                // Load portraits after items are rendered
                Dispatcher.UIThread.Post(() => LoadPortraitsAndTraining(),
                    DispatcherPriority.Background);

                _initialLoadDone = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Overview load error: {ex}");
            }
        }

        private void BuildGroupSection(string groupName, List<Character> characters, ref int cardIndex)
        {
            bool isCollapsed = _collapsedGroups.Contains(groupName);

            var chevron = new TextBlock
            {
                Text = isCollapsed ? "\u25B6" : "\u25BC",  // ▶ or ▼
                FontSize = FontScaleService.Small,
                Foreground = FindBrush("EveAccentPrimaryBrush", Brushes.Gold),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0),
                [DockPanel.DockProperty] = Dock.Left
            };

            var label = new TextBlock
            {
                Text = groupName,
                FontSize = FontScaleService.Body,
                FontWeight = FontWeight.SemiBold,
                Foreground = FindBrush("EveAccentPrimaryBrush", Brushes.Gold),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0),
                [DockPanel.DockProperty] = Dock.Left
            };

            var count = new TextBlock
            {
                Text = $"{characters.Count}",
                FontSize = FontScaleService.Small,
                Foreground = FindBrush("EveTextDisabledBrush", Brushes.Gray),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0),
                [DockPanel.DockProperty] = Dock.Left
            };

            var line = new Border
            {
                Height = 1,
                Background = FindBrush("EveAccentPrimaryBrush", Brushes.Gold),
                Opacity = 0.3,
                VerticalAlignment = VerticalAlignment.Center
            };

            var divider = new DockPanel
            {
                Margin = new Thickness(0, 6, 0, 2),
                MinHeight = 24,
                Cursor = new global::Avalonia.Input.Cursor(global::Avalonia.Input.StandardCursorType.Hand)
            };
            divider.Children.Add(chevron);
            divider.Children.Add(label);
            divider.Children.Add(count);
            divider.Children.Add(line);

            var wrap = BuildCardWrapPanel(characters, ref cardIndex, groupName: groupName);
            wrap.IsVisible = !isCollapsed;

            // Click header to toggle collapse
            divider.PointerPressed += (_, _) =>
            {
                wrap.IsVisible = !wrap.IsVisible;
                chevron.Text = wrap.IsVisible ? "\u25BC" : "\u25B6";

                if (wrap.IsVisible)
                    _collapsedGroups.Remove(groupName);
                else
                    _collapsedGroups.Add(groupName);

                CollapseStateHelper.SaveExpandState(0, OverviewCollapseKey, _collapsedGroups);
            };

            OverviewPanel.Children.Add(divider);
            OverviewPanel.Children.Add(wrap);
        }

        private WrapPanel BuildCardWrapPanel(List<Character> characters, ref int cardIndex,
            bool includeGhostCard = false, string? groupName = null)
        {
            var wrap = new WrapPanel { Orientation = Orientation.Horizontal, Tag = groupName };

            foreach (var character in characters)
            {
                var card = BuildCharacterCard(character, cardIndex);
                card.AddHandler(PointerPressedEvent, OnCardPointerPressed, global::Avalonia.Interactivity.RoutingStrategies.Tunnel);
                wrap.Children.Add(card);
                cardIndex++;
            }

            if (includeGhostCard)
                wrap.Children.Add(BuildGhostCard());

            return wrap;
        }

        private Button BuildGhostCard()
        {
            var plusText = new TextBlock
            {
                Text = "+",
                FontSize = FontScaleService.Title,
                FontWeight = FontWeight.Light,
                Foreground = FindBrush("EveAccentPrimaryBrush", Brushes.Gold),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var labelText = new TextBlock
            {
                Text = "Add New Character",
                FontSize = FontScaleService.Body,
                Foreground = FindBrush("EveTextDisabledBrush", Brushes.Gray),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center
            };

            var subtitleText = new TextBlock
            {
                Text = "Sign in with EVE to get started",
                FontSize = FontScaleService.Small,
                Foreground = FindBrush("EveTextDisabledBrush", Brushes.Gray),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Opacity = 0.6
            };

            var content = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Spacing = 4,
                Children = { plusText, labelText, subtitleText }
            };

            // Match real card dimensions — MinHeight same as character cards
            var cardBorder = new Border
            {
                Padding = new Thickness(12, 14),
                Width = 300,
                MinHeight = 90,
                Child = content,
                BorderThickness = new Thickness(1),
                BorderBrush = FindBrush("EveTextDisabledBrush", Brushes.Gray),
                CornerRadius = new CornerRadius(6),
                Background = Brushes.Transparent
            };

            var btn = new Button
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                Margin = new Thickness(4),
                Cursor = new global::Avalonia.Input.Cursor(global::Avalonia.Input.StandardCursorType.Hand),
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Content = cardBorder,
                Tag = "GhostCard"
            };
            btn.Click += OnAddCharacterFromWelcome;

            return btn;
        }

        private static int FindGhostCardIndex(WrapPanel wrap)
        {
            for (int i = 0; i < wrap.Children.Count; i++)
            {
                if (wrap.Children[i] is Button btn && btn.Tag as string == "GhostCard")
                    return i;
            }
            return -1;
        }

        private void BuildWelcomeState()
        {
            var container = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Spacing = 12,
                Margin = new Thickness(0, 80, 0, 0)
            };

            container.Children.Add(new TextBlock
            {
                Text = "Welcome to EveLens",
                FontSize = FontScaleService.Title,
                FontWeight = FontWeight.Bold,
                Foreground = FindBrush("EveAccentPrimaryBrush", Brushes.Gold),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center
            });

            container.Children.Add(new TextBlock
            {
                Text = "Add your first character to start monitoring\nskills, wallet, assets, and more.",
                FontSize = FontScaleService.Body,
                Foreground = FindBrush("EveTextSecondaryBrush", Brushes.Gray),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 360
            });

            var addButton = new Button
            {
                Content = "Add Character",
                FontSize = FontScaleService.Body,
                Padding = new Thickness(16, 6),
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = FindBrush("EveAccentPrimaryBrush", Brushes.Gold),
                Background = Brushes.Transparent,
                BorderBrush = FindBrush("EveAccentPrimaryBrush", Brushes.Gold),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Cursor = new global::Avalonia.Input.Cursor(global::Avalonia.Input.StandardCursorType.Hand)
            };
            addButton.Click += OnAddCharacterFromWelcome;
            container.Children.Add(addButton);

            container.Children.Add(new TextBlock
            {
                Text = "You can also add characters from the File menu.",
                FontSize = FontScaleService.Small,
                Foreground = FindBrush("EveTextDisabledBrush", Brushes.Gray),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 4, 0, 0)
            });

            OverviewPanel.Children.Add(container);
        }

        private async void OnAddCharacterFromWelcome(object? sender, RoutedEventArgs e)
        {
            try
            {
                var mainWindow = this.FindAncestorOfType<MainWindow>();
                if (mainWindow == null) return;

                var dialog = new AddCharacterWindow();
                await dialog.ShowDialog(mainWindow);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Welcome add character error: {ex}");
            }
        }

        #endregion

        #region Card Builder

        private Button BuildCharacterCard(Character character, int staggerIndex)
        {
            // Portrait with ESI status badge overlay (Teams-style)
            var portraitImage = new Image { Width = 56, Height = 56, Stretch = Stretch.UniformToFill };
            portraitImage.Tag = character.CharacterID;

            var portraitBorder = new Border
            {
                Width = 56, Height = 56,
                Background = FindBrush("EveBackgroundDarkestBrush", Brushes.Black),
                BorderBrush = FindBrush("EveBorderBrush", Brushes.Gray),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Child = portraitImage
            };

            var (dotColor, isFetching) = GetEsiStatus(character);
            var esiDot = new Border
            {
                Width = 12, Height = 12,
                CornerRadius = new CornerRadius(6),
                Background = dotColor,
                BorderBrush = FindBrush("EveBackgroundDarkBrush", Brushes.Black),
                BorderThickness = new Thickness(2),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Tag = "EsiDot"
            };

            // Grid overlays the dot on the portrait
            var portraitGrid = new Grid
            {
                Width = 56, Height = 56,
                Children = { portraitBorder, esiDot }
            };

            var portraitContainer = new StackPanel
            {
                Spacing = 0,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 0, 12, 0),
                Children = { portraitGrid }
            };

            // Info panel
            var infoPanel = new StackPanel { Spacing = 1 };

            infoPanel.Children.Add(new TextBlock
            {
                Text = PrivacyHelper.IsNameHidden ? PrivacyHelper.Mask : character.Name,
                FontSize = FontScaleService.Heading, FontWeight = FontWeight.Bold,
                Foreground = FindBrush("EveAccentPrimaryBrush", Brushes.Gold),
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            var iskText = new TextBlock
            {
                Text = PrivacyHelper.IsBalanceHidden ? $"{PrivacyHelper.Mask} ISK" : $"{character.Balance:N2} ISK",
                FontSize = FontScaleService.Body,
                Foreground = FindBrush("EveSuccessGreenBrush", Brushes.LimeGreen),
                Tag = "IskText",
                Transitions = new global::Avalonia.Animation.Transitions
                {
                    new global::Avalonia.Animation.DoubleTransition
                    {
                        Property = OpacityProperty,
                        Duration = TimeSpan.FromMilliseconds(300),
                        Easing = new QuadraticEaseOut()
                    }
                }
            };
            infoPanel.Children.Add(iskText);

            var spText = new TextBlock
            {
                Text = PrivacyHelper.IsSkillPointsHidden ? $"{PrivacyHelper.Mask} SP" : $"{character.SkillPoints:N0} SP",
                FontSize = FontScaleService.Body,
                Foreground = FindBrush("EveTextSecondaryBrush", Brushes.Gray),
                Tag = "SpText",
                Transitions = new global::Avalonia.Animation.Transitions
                {
                    new global::Avalonia.Animation.DoubleTransition
                    {
                        Property = OpacityProperty,
                        Duration = TimeSpan.FromMilliseconds(300),
                        Easing = new QuadraticEaseOut()
                    }
                }
            };
            infoPanel.Children.Add(spText);

            // Location + Ship line
            string locationName = character.LastKnownSolarSystem?.Name ?? "";
            string shipType = character.ShipTypeName ?? "";
            if (!string.IsNullOrEmpty(locationName) || !string.IsNullOrEmpty(shipType))
            {
                string locShipText = !string.IsNullOrEmpty(locationName) && !string.IsNullOrEmpty(shipType)
                    ? $"{locationName} \u2022 {shipType}"
                    : !string.IsNullOrEmpty(locationName) ? locationName : shipType;

                infoPanel.Children.Add(new TextBlock
                {
                    Text = locShipText,
                    FontSize = FontScaleService.Small,
                    Foreground = FindBrush("EveTextDisabledBrush", Brushes.Gray),
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    Tag = "LocationShipText"
                });
            }

            // Account status badge \u2014 built here, added below portrait later
            bool isOmega = character.EffectiveCharacterStatus == AccountStatus.Omega;
            var badgeText = new TextBlock
            {
                Text = isOmega ? $"\u03A9 {Loc.Get("Eve.Omega")}" : $"\u03B1 {Loc.Get("Eve.Alpha")}",
                FontSize = FontScaleService.Caption, FontWeight = FontWeight.SemiBold,
                Foreground = isOmega
                    ? new SolidColorBrush(Color.Parse("#FF00C853"))
                    : new SolidColorBrush(Color.Parse("#FFFF6D00")),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            var badge = new Border
            {
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(6, 1),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 3, 0, 0),
                Background = isOmega
                    ? new SolidColorBrush(Color.Parse("#2200C853"))
                    : new SolidColorBrush(Color.Parse("#22FF6D00")),
                Child = badgeText
            };

            // Training status
            var trainingText = new TextBlock
            {
                FontSize = FontScaleService.Small,
                Foreground = FindBrush("EveWarningYellowBrush", Brushes.Yellow),
                TextTrimming = TextTrimming.CharacterEllipsis,
                Tag = "TrainingText"
            };
            UpdateTrainingText(trainingText, character);
            infoPanel.Children.Add(trainingText);

            // Add Omega/Alpha badge below portrait
            portraitContainer.Children.Add(badge);

            var cardGrid = new Grid { ColumnDefinitions = ColumnDefinitions.Parse("68,*") };
            Grid.SetColumn(portraitContainer, 0);
            Grid.SetColumn(infoPanel, 1);
            cardGrid.Children.Add(portraitContainer);
            cardGrid.Children.Add(infoPanel);

            // Queue health state determines card tint and left border
            var (queueTint, queueBorderBrush) = _debugQueueTints
                ? GetDebugQueueHealthBrushes(staggerIndex)
                : GetQueueHealthBrushes(character);

            var cardContent = new Grid();
            // Tint overlay on the card background
            if (queueTint != null)
            {
                cardContent.Children.Add(new Border
                {
                    Background = queueTint,
                    CornerRadius = new CornerRadius(6),
                    IsHitTestVisible = false,
                });
            }
            // Left border accent strip
            if (queueBorderBrush != null)
            {
                cardContent.Children.Add(new Border
                {
                    Width = 3,
                    Background = queueBorderBrush,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    CornerRadius = new CornerRadius(6, 0, 0, 6),
                    IsHitTestVisible = false,
                });
            }
            cardContent.Children.Add(new Border
            {
                Padding = new Thickness(12, 10),
                Child = cardGrid
            });

            // Status dot + label in top-right corner — click to navigate to Queue tab
            var (statusBrush, statusTooltip) = _debugQueueTints
                ? GetDebugStatusDot(staggerIndex)
                : GetQueueStatusDot(character);
            if (statusBrush != null)
            {
                var statusPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 4,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 8, 10, 0),
                    Cursor = new global::Avalonia.Input.Cursor(global::Avalonia.Input.StandardCursorType.Hand),
                };

                statusPanel.Children.Add(new Ellipse
                {
                    Width = 8, Height = 8,
                    Fill = statusBrush,
                    VerticalAlignment = VerticalAlignment.Center,
                });

                statusPanel.Children.Add(new TextBlock
                {
                    Text = statusTooltip,
                    FontSize = FontScaleService.Caption,
                    Foreground = statusBrush,
                    VerticalAlignment = VerticalAlignment.Center,
                });

                ToolTip.SetTip(statusPanel, $"Queue: {statusTooltip}\nClick to open Queue tab");

                // Click navigates to this character's Queue tab
                var capturedChar = character;
                statusPanel.PointerPressed += (_, e) =>
                {
                    e.Handled = true;
                    var mainWindow = this.FindAncestorOfType<MainWindow>();
                    if (mainWindow != null)
                    {
                        mainWindow.NavigateToCharacter(capturedChar);
                        // TODO: auto-switch to Queue sub-tab
                    }
                };

                cardContent.Children.Add(statusPanel);
            }

            var cardBorder = new Border
            {
                Width = 300,
                MinHeight = 90,
                Child = cardContent
            };
            cardBorder.Classes.Add("card");

            bool animate = !_initialLoadDone;

            var btn = new Button
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                Margin = new Thickness(4),
                Cursor = new global::Avalonia.Input.Cursor(global::Avalonia.Input.StandardCursorType.Hand),
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Content = cardBorder,
                DataContext = character,
                Opacity = animate ? 0 : 1
            };

            if (animate)
            {
                btn.Transitions = new global::Avalonia.Animation.Transitions
                {
                    new global::Avalonia.Animation.DoubleTransition
                    {
                        Property = OpacityProperty,
                        Duration = TimeSpan.FromMilliseconds(300),
                        Easing = new QuadraticEaseOut()
                    }
                };

                int delayMs = staggerIndex * 50;
                if (delayMs > 0)
                {
                    var timer = new DispatcherTimer(DispatcherPriority.Render)
                    {
                        Interval = TimeSpan.FromMilliseconds(delayMs)
                    };
                    timer.Tick += (_, _) =>
                    {
                        timer.Stop();
                        _animationTimers.Remove(timer);
                        btn.Opacity = 1;
                    };
                    _animationTimers.Add(timer);
                    timer.Start();
                }
                else
                {
                    Dispatcher.UIThread.Post(() => btn.Opacity = 1, DispatcherPriority.Render);
                }
            }

            // Context menu
            var deleteItem = new MenuItem { Header = "Delete Character...", Tag = character };
            deleteItem.Click += OnDeleteCharacter;
            btn.ContextMenu = new ContextMenu { Items = { deleteItem } };

            btn.Click += OnCharacterCardClick;

            return btn;
        }

        private (IBrush dotColor, bool isFetching) GetEsiStatus(Character character)
        {
            var yellowBrush = FindBrush("EveWarningYellowBrush", Brushes.Yellow);
            var greenBrush = FindBrush("EveSuccessGreenBrush", Brushes.LimeGreen);
            var redBrush = FindBrush("EveErrorRedBrush", Brushes.Red);
            var grayBrush = FindBrush("EveTextDisabledBrush", Brushes.Gray);

            if (character is not CCPCharacter ccp)
                return (grayBrush, false);

            bool anyUpdating = false;
            bool anyNoKey = false;

            try
            {
                foreach (var monitor in ccp.QueryMonitors)
                {
                    if (monitor.IsUpdating)
                        anyUpdating = true;
                    if (monitor.Status == QueryStatus.NoESIKey)
                        anyNoKey = true;
                }
            }
            catch
            {
                return (grayBrush, false);
            }

            // Health state from the EndpointHealthTracker state machine
            var summary = AppServices.HealthTracker?.GetCharacterHealth(ccp.CharacterID);

            if (summary?.OverallHealth == Infrastructure.Scheduling.Health.CharacterHealth.Suspended)
                return (redBrush, false);

            if (anyNoKey)
                return (redBrush, false);

            if (summary?.OverallHealth == Infrastructure.Scheduling.Health.CharacterHealth.Failing)
                return (redBrush, false);

            if (anyUpdating)
                return (yellowBrush, true);

            if (summary?.OverallHealth == Infrastructure.Scheduling.Health.CharacterHealth.Degraded)
                return (yellowBrush, false);

            if (ccp.HasCompletedFirstUpdate)
                return (greenBrush, false);

            return (grayBrush, false);
        }

        #endregion

        #region Event Handlers — Incremental Updates

        private void OnFetchCompleted(MonitorFetchCompletedEvent evt)
        {
            try
            {
                var card = FindCardByCharacterId(evt.CharacterId);
                if (card == null) return;

                var character = card.DataContext as Character;
                if (character == null) return;

                // Update ISK
                var iskBlock = FindTaggedDescendant<TextBlock>(card, "IskText");
                if (iskBlock != null)
                {
                    string newIsk = PrivacyHelper.IsBalanceHidden
                        ? $"{PrivacyHelper.Mask} ISK"
                        : $"{character.Balance:N2} ISK";
                    if (iskBlock.Text != newIsk)
                    {
                        iskBlock.Text = newIsk;
                        if (!PrivacyHelper.IsBalanceHidden) FlashTextBlock(iskBlock);
                    }
                    _prevBalances[character.CharacterID] = character.Balance;
                }

                // Update SP
                var spBlock = FindTaggedDescendant<TextBlock>(card, "SpText");
                if (spBlock != null)
                {
                    string newSp = PrivacyHelper.IsSkillPointsHidden
                        ? $"{PrivacyHelper.Mask} SP"
                        : $"{character.SkillPoints:N0} SP";
                    if (spBlock.Text != newSp)
                    {
                        spBlock.Text = newSp;
                        if (!PrivacyHelper.IsSkillPointsHidden) FlashTextBlock(spBlock);
                    }
                    _prevSkillPoints[character.CharacterID] = character.SkillPoints;
                }

                // Update location + ship
                var locShipBlock = FindTaggedDescendant<TextBlock>(card, "LocationShipText");
                if (locShipBlock != null)
                {
                    string locationName = character.LastKnownSolarSystem?.Name ?? "";
                    string shipType = character.ShipTypeName ?? "";
                    string locShipText = !string.IsNullOrEmpty(locationName) && !string.IsNullOrEmpty(shipType)
                        ? $"{locationName} • {shipType}"
                        : !string.IsNullOrEmpty(locationName) ? locationName : shipType;
                    if (locShipBlock.Text != locShipText)
                        locShipBlock.Text = locShipText;
                }

                // Update training text
                var trainingBlock = FindTaggedDescendant<TextBlock>(card, "TrainingText");
                if (trainingBlock != null)
                    UpdateTrainingText(trainingBlock, character);

                // Update ESI status
                UpdateEsiStatus(card, character);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Overview fetch update error: {ex}");
            }
        }

        private void OnSecondTick()
        {
            try
            {
                _fetchingDotBright = !_fetchingDotBright;

                foreach (var card in GetAllCardButtons())
                {
                    var character = card.DataContext as Character;
                    if (character == null) continue;

                    // Tick training countdown every second
                    var trainingBlock = FindTaggedDescendant<TextBlock>(card, "TrainingText");
                    if (trainingBlock != null)
                        UpdateTrainingText(trainingBlock, character);

                    // Pulse the dot for actively fetching characters
                    var dot = FindTaggedDescendant<Border>(card, "EsiDot");
                    if (dot != null && character is CCPCharacter ccp)
                    {
                        bool anyUpdating = false;
                        try
                        {
                            foreach (var monitor in ccp.QueryMonitors)
                            {
                                if (monitor.IsUpdating) { anyUpdating = true; break; }
                            }
                        }
                        catch { /* ignore */ }

                        if (anyUpdating)
                            dot.Opacity = _fetchingDotBright ? 1.0 : 0.4;
                        else if (dot.Opacity < 1.0)
                            dot.Opacity = 1.0; // restore after fetching ends
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Overview tick error: {ex}");
            }
        }

        private void OnCollectionChanged()
        {
            try
            {
                // If groups are active, fall back to full rebuild (group membership may have changed)
                if (Settings.CharacterGroups.Count > 0)
                {
                    LoadData();
                    return;
                }

                var currentChars = AppServices.Characters.Where(c => c.Monitored).ToList();
                var existingCards = GetAllCardButtons().ToList();
                var existingIds = new HashSet<long>(
                    existingCards
                        .Select(b => (b.DataContext as Character)?.CharacterID ?? 0)
                        .Where(id => id != 0));

                var currentIds = new HashSet<long>(currentChars.Select(c => c.CharacterID));

                // Find the WrapPanel (should be the only/first one in ungrouped mode)
                var wrap = OverviewPanel.Children.OfType<WrapPanel>().FirstOrDefault();
                if (wrap == null)
                {
                    // No wrap panel yet — full rebuild
                    LoadData();
                    return;
                }

                // Remove deleted characters
                var toRemove = existingCards
                    .Where(b => b.DataContext is Character c && !currentIds.Contains(c.CharacterID))
                    .ToList();

                foreach (var card in toRemove)
                {
                    // Fade out then remove
                    card.Opacity = 0;
                    var timer = new DispatcherTimer(DispatcherPriority.Render)
                    {
                        Interval = TimeSpan.FromMilliseconds(350)
                    };
                    var capturedCard = card;
                    var capturedWrap = wrap;
                    timer.Tick += (_, _) =>
                    {
                        timer.Stop();
                        _animationTimers.Remove(timer);
                        capturedWrap.Children.Remove(capturedCard);
                    };
                    _animationTimers.Add(timer);
                    timer.Start();

                    if (card.DataContext is Character removed)
                    {
                        _prevBalances.Remove(removed.CharacterID);
                        _prevSkillPoints.Remove(removed.CharacterID);
                    }
                }

                // Add new characters (insert before ghost card if present)
                var newChars = currentChars.Where(c => !existingIds.Contains(c.CharacterID)).ToList();
                int ghostIndex = FindGhostCardIndex(wrap);
                int staggerBase = wrap.Children.Count;
                foreach (var character in newChars)
                {
                    var card = BuildCharacterCard(character, staggerBase);
                    if (ghostIndex >= 0)
                    {
                        wrap.Children.Insert(ghostIndex, card);
                        ghostIndex++;
                    }
                    else
                    {
                        wrap.Children.Add(card);
                    }
                    staggerBase++;

                    _prevBalances[character.CharacterID] = character.Balance;
                    _prevSkillPoints[character.CharacterID] = character.SkillPoints;
                }

                // Load portraits for any new cards
                if (newChars.Count > 0)
                {
                    Dispatcher.UIThread.Post(() => LoadPortraitsForCharacters(
                        newChars.Select(c => c.CharacterID).ToHashSet()),
                        DispatcherPriority.Background);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Overview collection change error: {ex}");
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Returns (dotBrush, label) for queue health status on card.
        /// Thresholds: >5d green, less than 5d yellow, less than 24h red, empty dark red, paused gray.
        /// </summary>
        private static (IBrush? brush, string label) GetQueueStatusDot(Character character)
        {
            if (character is not CCPCharacter ccp)
                return (null, "");

            if (!ccp.IsTraining || ccp.CurrentlyTrainingSkill == null)
                return (new SolidColorBrush(Color.Parse("#707080")), Loc.Get("Status.Paused"));

            var remaining = ccp.SkillQueue.EndTime - DateTime.UtcNow;

            if (remaining.TotalHours <= 0)
                return (new SolidColorBrush(Color.Parse("#B71C1C")), "Empty");

            if (remaining.TotalHours < 24)
                return (Application.Current?.FindResource("EveErrorRedBrush") as IBrush, $"{(int)remaining.TotalHours}h left");

            if (remaining.TotalDays < 5)
                return (Application.Current?.FindResource("EveWarningYellowBrush") as IBrush, $"{(int)remaining.TotalDays}d left");

            return (Application.Current?.FindResource("EveSuccessGreenBrush") as IBrush, $"{(int)remaining.TotalDays}d left");
        }

        /// <summary>
        /// Debug: cycles through all status dots.
        /// </summary>
        private static (IBrush? brush, string label) GetDebugStatusDot(int index)
        {
            return (index % 5) switch
            {
                0 => (Application.Current?.FindResource("EveSuccessGreenBrush") as IBrush, "14d left"),
                1 => (Application.Current?.FindResource("EveWarningYellowBrush") as IBrush, "3d left"),
                2 => (Application.Current?.FindResource("EveErrorRedBrush") as IBrush, "8h left"),
                3 => (new SolidColorBrush(Color.Parse("#B71C1C")), "Empty"),
                _ => (new SolidColorBrush(Color.Parse("#707080")), "Paused"),
            };
        }

        /// <summary>
        /// Debug: returns tint/border cycling through all states based on card index.
        /// Call ToggleDebugQueueTints() to enable from the Debug menu.
        /// </summary>
        private static (IBrush? tint, IBrush? border) GetDebugQueueHealthBrushes(int index)
        {
            string[] tintKeys = { "EveQueueHealthyTint", "EveQueueWarningTint", "EveQueueCriticalTint", "EveQueueEmptyTint", "EveQueuePausedTint" };
            string[] borderKeys = { "EveQueueHealthyBorder", "EveQueueWarningBorder", "EveQueueCriticalBorder", "EveQueueEmptyBorder", "EveQueuePausedBorder" };
            int state = index % 5;
            return (
                Application.Current?.FindResource(tintKeys[state]) as IBrush,
                Application.Current?.FindResource(borderKeys[state]) as IBrush);
        }

        /// <summary>
        /// Toggles debug queue tint mode — cycles cards through all health states.
        /// </summary>
        public void ToggleDebugQueueTints()
        {
            _debugQueueTints = !_debugQueueTints;
            LoadData();
        }

        /// <summary>
        /// Returns (backgroundTint, leftBorderBrush) based on queue health state.
        /// Uses theme-aware resources defined in each palette.
        /// </summary>
        private static (IBrush? tint, IBrush? border) GetQueueHealthBrushes(Character character)
        {
            if (character is not CCPCharacter ccp)
                return (null, null);

            // Not training at all
            if (!ccp.IsTraining || ccp.CurrentlyTrainingSkill == null)
            {
                return (
                    Application.Current?.FindResource("EveQueuePausedTint") as IBrush,
                    Application.Current?.FindResource("EveQueuePausedBorder") as IBrush);
            }

            var queueEnd = ccp.SkillQueue.EndTime;
            var remaining = queueEnd - DateTime.UtcNow;

            if (remaining.TotalHours <= 0)
            {
                // Queue empty
                return (
                    Application.Current?.FindResource("EveQueueEmptyTint") as IBrush,
                    Application.Current?.FindResource("EveQueueEmptyBorder") as IBrush);
            }

            if (remaining.TotalHours < 24)
            {
                // Critical — less than 24 hours
                return (
                    Application.Current?.FindResource("EveQueueCriticalTint") as IBrush,
                    Application.Current?.FindResource("EveQueueCriticalBorder") as IBrush);
            }

            if (remaining.TotalDays < 5)
            {
                // Warning — less than 5 days
                return (
                    Application.Current?.FindResource("EveQueueWarningTint") as IBrush,
                    Application.Current?.FindResource("EveQueueWarningBorder") as IBrush);
            }

            // Healthy — subtle green tint (>5 days)
            return (
                Application.Current?.FindResource("EveQueueHealthyTint") as IBrush,
                Application.Current?.FindResource("EveQueueHealthyBorder") as IBrush);
        }

        private static void UpdateTrainingText(TextBlock textBlock, Character character)
        {
            if (PrivacyHelper.IsTrainingHidden)
            {
                textBlock.Text = $"{Loc.Get("Status.Training")}: {PrivacyHelper.Mask}";
                textBlock.Foreground = (IBrush?)Application.Current?.FindResource("EveWarningYellowBrush") ?? Brushes.Yellow;
                return;
            }

            if (character is CCPCharacter ccp && ccp.IsTraining && ccp.CurrentlyTrainingSkill != null)
            {
                var skill = ccp.CurrentlyTrainingSkill;
                var remaining = skill.RemainingTime;
                string timeStr = remaining.TotalHours >= 24
                    ? $"{(int)remaining.TotalDays}d {remaining.Hours}h"
                    : remaining.TotalHours >= 1
                        ? $"{(int)remaining.TotalHours}h {remaining.Minutes}m"
                        : $"{remaining.Minutes}m {remaining.Seconds}s";
                textBlock.Text = $"{Loc.Get("Status.Training")}: {skill.SkillName} {skill.Level} ({timeStr})";
                textBlock.Foreground = (IBrush?)Application.Current?.FindResource("EveWarningYellowBrush") ?? Brushes.Yellow;
            }
            else
            {
                textBlock.Text = Loc.Get("Status.Paused");
                textBlock.Foreground = Brushes.Gray;
            }
        }

        private void UpdateEsiStatus(Button card, Character character)
        {
            var (dotColor, isFetching) = GetEsiStatus(character);

            var dot = FindTaggedDescendant<Border>(card, "EsiDot");
            if (dot != null)
            {
                dot.Background = dotColor;
                if (isFetching)
                    dot.Opacity = _fetchingDotBright ? 1.0 : 0.4;
                else
                    dot.Opacity = 1.0;
            }
        }

        private static void FlashTextBlock(TextBlock tb)
        {
            tb.Opacity = 0.5;
            // The DoubleTransition on Opacity handles the smooth fade back to 1.0
            Dispatcher.UIThread.Post(() => tb.Opacity = 1.0, DispatcherPriority.Render);
        }

        private Button? FindCardByCharacterId(long characterId)
        {
            foreach (var card in GetAllCardButtons())
            {
                if (card.DataContext is Character c && c.CharacterID == characterId)
                    return card;
            }
            return null;
        }

        private IEnumerable<Button> GetAllCardButtons()
        {
            foreach (var child in OverviewPanel.Children)
            {
                if (child is WrapPanel wrap)
                {
                    foreach (var item in wrap.Children)
                    {
                        if (item is Button btn && btn.DataContext is Character)
                            yield return btn;
                    }
                }
            }
        }

        private static T? FindTaggedDescendant<T>(Control root, string tag) where T : Control
        {
            foreach (var descendant in root.GetVisualDescendants())
            {
                if (descendant is T typed && typed.Tag is string s && s == tag)
                    return typed;
            }
            return null;
        }

        private static IBrush FindBrush(string resourceKey, IBrush fallback)
        {
            return (IBrush?)Application.Current?.FindResource(resourceKey) ?? fallback;
        }

        #endregion

        #region Portrait Loading

        private async void LoadPortraitsAndTraining()
        {
            try
            {
                // Use logical tree (not visual) so collapsed group cards are included
                var images = OverviewPanel.GetLogicalDescendants().OfType<Image>()
                    .Where(img => img.Tag is long).ToList();

                foreach (var img in images)
                {
                    long charId = (long)img.Tag!;
                    try
                    {
                        var drawingImage = await ImageService.GetCharacterImageAsync(charId);
                        if (drawingImage != null)
                        {
                            var converted = DrawingImageToAvaloniaConverter.Instance.Convert(
                                drawingImage, typeof(Bitmap), null, CultureInfo.InvariantCulture);
                            if (converted is Bitmap bitmap)
                            {
                                (img.Source as IDisposable)?.Dispose();
                                img.Source = bitmap;
                            }
                        }
                    }
                    catch { /* portrait load failure is non-fatal */ }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Portrait load error: {ex}");
            }
        }

        private async void LoadPortraitsForCharacters(HashSet<long> characterIds)
        {
            try
            {
                // Use logical tree (not visual) so collapsed group cards are included
                var images = OverviewPanel.GetLogicalDescendants().OfType<Image>()
                    .Where(img => img.Tag is long id && characterIds.Contains(id)).ToList();

                foreach (var img in images)
                {
                    long charId = (long)img.Tag!;
                    try
                    {
                        var drawingImage = await ImageService.GetCharacterImageAsync(charId);
                        if (drawingImage != null)
                        {
                            var converted = DrawingImageToAvaloniaConverter.Instance.Convert(
                                drawingImage, typeof(Bitmap), null, CultureInfo.InvariantCulture);
                            if (converted is Bitmap bitmap)
                            {
                                (img.Source as IDisposable)?.Dispose();
                                img.Source = bitmap;
                            }
                        }
                    }
                    catch { /* portrait load failure is non-fatal */ }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Portrait load error: {ex}");
            }
        }

        #endregion

        #region Click Handlers

        private void OnDeleteCharacter(object? sender, RoutedEventArgs e)
        {
            try
            {
                Character? character = null;
                if (sender is MenuItem { Tag: Character c })
                    character = c;

                if (character == null) return;

                var mainWindow = this.FindAncestorOfType<MainWindow>();
                mainWindow?.DeleteCharacterWithConfirmation(character);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error: {ex}");
            }
        }

        private void OnCharacterCardClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (_reorderMode) return;

                if (sender is Button btn && btn.DataContext is Character character)
                {
                    var mainWindow = this.FindAncestorOfType<MainWindow>();
                    mainWindow?.NavigateToCharacter(character);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Card click error: {ex}");
            }
        }

        #endregion

        #region Drag-to-Reorder

        private void BuildReorderToolbar()
        {
            var characters = AppServices.Characters.Where(c => c.Monitored).ToList();
            if (characters.Count < 2) return;

            _reorderToggleBtn = new Button
            {
                Content = _reorderMode ? "Done" : "Reorder",
                FontSize = FontScaleService.Small,
                Padding = new Thickness(10, 3),
                CornerRadius = new CornerRadius(12),
                Background = _reorderMode
                    ? FindBrush("EveAccentPrimaryBrush", Brushes.Gold)
                    : Brushes.Transparent,
                Foreground = _reorderMode
                    ? Brushes.Black
                    : FindBrush("EveTextSecondaryBrush", Brushes.Gray),
                HorizontalAlignment = HorizontalAlignment.Right,
                Cursor = new global::Avalonia.Input.Cursor(global::Avalonia.Input.StandardCursorType.Hand)
            };
            _reorderToggleBtn.Click += OnReorderToggle;

            var manageGroupsBtn = new Button
            {
                Content = "Manage Groups",
                FontSize = FontScaleService.Small,
                Padding = new Thickness(10, 3),
                CornerRadius = new CornerRadius(12),
                Background = Brushes.Transparent,
                Foreground = FindBrush("EveTextSecondaryBrush", Brushes.Gray),
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 6, 0),
                Cursor = new global::Avalonia.Input.Cursor(global::Avalonia.Input.StandardCursorType.Hand)
            };
            manageGroupsBtn.Click += OnManageGroupsClick;

            var toolbar = new DockPanel { Margin = new Thickness(0, 0, 0, 4) };
            if (_reorderMode)
            {
                toolbar.Children.Add(new TextBlock
                {
                    Text = "Drag cards to reorder, then click Done",
                    FontSize = FontScaleService.Small,
                    Foreground = FindBrush("EveAccentPrimaryBrush", Brushes.Gold),
                    VerticalAlignment = VerticalAlignment.Center,
                    FontStyle = FontStyle.Italic
                });
            }

            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            btnPanel.Children.Add(manageGroupsBtn);
            btnPanel.Children.Add(_reorderToggleBtn);
            DockPanel.SetDock(btnPanel, Dock.Right);
            toolbar.Children.Insert(0, btnPanel);

            OverviewPanel.Children.Add(toolbar);
        }

        private void OnManageGroupsClick(object? sender, RoutedEventArgs e)
        {
            var mainWindow = this.FindAncestorOfType<MainWindow>();
            if (mainWindow == null) return;

            var dialog = new ManageGroupsWindow { };
            dialog.ShowDialog(mainWindow);
        }

        private void OnReorderToggle(object? sender, RoutedEventArgs e)
        {
            _reorderMode = !_reorderMode;
            LoadData();
        }

        private static List<Character> SortByUngroupedOrder(List<Character> characters)
        {
            var order = Settings.UngroupedCharacterOrder;
            if (order == null || order.Count == 0)
                return characters;

            var orderMap = new Dictionary<Guid, int>();
            for (int i = 0; i < order.Count; i++)
                orderMap[order[i]] = i;

            return characters
                .OrderBy(c => orderMap.TryGetValue(c.Guid, out int idx) ? idx : int.MaxValue)
                .ThenBy(c => c.Name)
                .ToList();
        }

        private void OnCardPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            try
            {
                if (!_reorderMode) return;

                if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed == false)
                    return;

                if (sender is not Button card || card.DataContext is not Character)
                    return;

                var wrap = card.Parent as WrapPanel;
                if (wrap == null) return;

                var pos = e.GetPosition(OverviewScroller);
                _dragPending = true;
                _dragStartX = pos.X;
                _dragStartY = pos.Y;
                _draggedCard = card;
                _dragSourcePanel = wrap;
                _dragGroupName = wrap.Tag as string;
                _dragStartIndex = GetCardIndex(wrap, card);

                e.Pointer.Capture(OverviewScroller);
                e.Handled = true;

                OverviewScroller.PointerMoved += OnPendingDragPointerMoved;
                OverviewScroller.PointerReleased += OnPendingDragPointerReleased;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Drag press error: {ex}");
            }
        }

        private void OnPendingDragPointerMoved(object? sender, PointerEventArgs e)
        {
            if (!_dragPending) return;

            var pos = e.GetPosition(OverviewScroller);
            double dx = Math.Abs(pos.X - _dragStartX);
            double dy = Math.Abs(pos.Y - _dragStartY);

            if (dx < DragThreshold && dy < DragThreshold) return;

            // Promote to real drag
            _dragPending = false;
            OverviewScroller.PointerMoved -= OnPendingDragPointerMoved;
            OverviewScroller.PointerReleased -= OnPendingDragPointerReleased;

            PromoteToDrag(e);
        }

        private void OnPendingDragPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            // Did not exceed threshold — not a drag, allow click to proceed
            _dragPending = false;
            OverviewScroller.PointerMoved -= OnPendingDragPointerMoved;
            OverviewScroller.PointerReleased -= OnPendingDragPointerReleased;
            e.Pointer.Capture(null);
            _draggedCard = null;
            _dragSourcePanel = null;
            _dragStartIndex = -1;
        }

        private void PromoteToDrag(PointerEventArgs e)
        {
            _isDragging = true;

            // Ghost the dragged card — smooth fade out
            if (_draggedCard != null)
            {
                _draggedCard.Opacity = 0.2;
                _draggedCard.RenderTransform = global::Avalonia.Media.Transformation.TransformOperations.Parse("scale(0.95)");
            }

            // Create ghost badge with character name
            string label = (_draggedCard?.DataContext as Character)?.Name ?? "Character";
            _dragGhost = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#E81A1F2E")),
                BorderBrush = FindBrush("EveAccentPrimaryBrush", Brushes.Gold),
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14, 8),
                IsHitTestVisible = false,
                BoxShadow = new BoxShadows(new BoxShadow { Blur = 12, Color = Color.Parse("#60000000") }),
                Child = new TextBlock
                {
                    Text = label,
                    FontSize = FontScaleService.Body,
                    Foreground = FindBrush("EveAccentPrimaryBrush", Brushes.Gold),
                    FontWeight = FontWeight.SemiBold,
                },
            };

            var viewPos = e.GetPosition(OverviewScroller);
            Canvas.SetLeft(_dragGhost, viewPos.X + 16);
            Canvas.SetTop(_dragGhost, viewPos.Y - 12);
            DragCanvas.Children.Add(_dragGhost);

            // Create insert indicator (vertical line between cards)
            _insertIndicator = new Border
            {
                Width = 3,
                Height = 90,
                Background = new SolidColorBrush(Color.Parse("#4A94F0")),
                CornerRadius = new CornerRadius(2),
                IsHitTestVisible = false,
                IsVisible = false
            };
            DragCanvas.Children.Add(_insertIndicator);

            // Capture pointer on the scroller
            e.Pointer.Capture(OverviewScroller);
            OverviewScroller.PointerMoved += OnDragPointerMoved;
            OverviewScroller.PointerReleased += OnDragPointerReleased;
        }

        private void OnDragPointerMoved(object? sender, PointerEventArgs e)
        {
            if (!_isDragging || _dragSourcePanel == null) return;

            try
            {
                var viewPos = e.GetPosition(OverviewScroller);

                // Move ghost
                if (_dragGhost != null)
                {
                    Canvas.SetLeft(_dragGhost, viewPos.X + 16);
                    Canvas.SetTop(_dragGhost, viewPos.Y - 12);
                }

                // Auto-scroll near edges
                double scrollZone = 40;
                double viewportHeight = OverviewScroller.Bounds.Height;
                if (viewPos.Y < scrollZone)
                {
                    double speed = (scrollZone - viewPos.Y) / scrollZone * 6;
                    OverviewScroller.Offset = new Vector(0, Math.Max(0, OverviewScroller.Offset.Y - speed));
                }
                else if (viewPos.Y > viewportHeight - scrollZone)
                {
                    double speed = (viewPos.Y - (viewportHeight - scrollZone)) / scrollZone * 6;
                    double maxScroll = OverviewScroller.Extent.Height - viewportHeight;
                    OverviewScroller.Offset = new Vector(0, Math.Min(maxScroll, OverviewScroller.Offset.Y + speed));
                }

                // Calculate insert position within the source WrapPanel
                var panelPos = e.GetPosition(_dragSourcePanel);
                int insertIndex = CalculateInsertIndex(_dragSourcePanel, panelPos);

                if (insertIndex == _currentInsertIndex) return;
                _currentInsertIndex = insertIndex;

                // Position the insert indicator
                UpdateInsertIndicator(insertIndex);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Drag move error: {ex}");
            }
        }

        private void OnDragPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (!_isDragging) return;

            try
            {
                // Right-click cancels
                if (e.InitialPressMouseButton == MouseButton.Right)
                {
                    CancelDrag(e.Pointer);
                    return;
                }

                // Cleanup event subscriptions
                OverviewScroller.PointerMoved -= OnDragPointerMoved;
                OverviewScroller.PointerReleased -= OnDragPointerReleased;
                e.Pointer.Capture(null);

                // Remove ghost and indicator
                CleanupDragVisuals();

                // Restore card opacity
                if (_draggedCard != null)
                {
                    _draggedCard.Opacity = 1.0;
                    _draggedCard.RenderTransform = null;
                }

                // Perform the reorder if we have a valid drop position
                if (_currentInsertIndex >= 0 && _dragSourcePanel != null && _dragStartIndex >= 0)
                {
                    int fromIndex = _dragStartIndex;
                    int toIndex = _currentInsertIndex;

                    // Adjust target: if moving forward, account for the removal of the source
                    if (toIndex > fromIndex)
                        toIndex--;

                    if (toIndex != fromIndex)
                    {
                        PerformReorder(fromIndex, toIndex);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Drag release error: {ex}");
            }
            finally
            {
                ResetDragState();
            }
        }

        private void CancelDrag(IPointer pointer)
        {
            OverviewScroller.PointerMoved -= OnDragPointerMoved;
            OverviewScroller.PointerReleased -= OnDragPointerReleased;
            pointer.Capture(null);
            CleanupDragVisuals();

            if (_draggedCard != null)
                _draggedCard.Opacity = 1.0;

            ResetDragState();
        }

        private void CleanupDragVisuals()
        {
            if (_dragGhost != null)
            {
                DragCanvas.Children.Remove(_dragGhost);
                _dragGhost = null;
            }
            if (_insertIndicator != null)
            {
                DragCanvas.Children.Remove(_insertIndicator);
                _insertIndicator = null;
            }
        }

        private void ResetDragState()
        {
            _isDragging = false;
            _dragPending = false;
            _draggedCard = null;
            _dragSourcePanel = null;
            _dragStartIndex = -1;
            _currentInsertIndex = -1;
            _dragGroupName = null;
        }

        private int CalculateInsertIndex(WrapPanel panel, Point posInPanel)
        {
            // Count only character cards (exclude ghost card)
            int cardCount = 0;
            for (int i = 0; i < panel.Children.Count; i++)
            {
                if (panel.Children[i] is Button btn && btn.DataContext is Character)
                    cardCount++;
            }

            if (cardCount == 0) return 0;

            // Use actual card bounds for accurate hit detection
            for (int i = 0; i < panel.Children.Count; i++)
            {
                if (panel.Children[i] is not Button btn || btn.DataContext is not Character)
                    continue;

                var bounds = btn.Bounds;
                double cardMidX = bounds.X + bounds.Width / 2;

                // Check if pointer is within this card's vertical band
                if (posInPanel.Y >= bounds.Y && posInPanel.Y < bounds.Y + bounds.Height)
                {
                    if (posInPanel.X < cardMidX)
                        return i;
                }
            }

            // If pointer is below or to the right of all cards, insert at end
            return cardCount;
        }

        private void UpdateInsertIndicator(int insertIndex)
        {
            if (_insertIndicator == null || _dragSourcePanel == null) return;

            // Get the position of the insert point in scroller coordinates
            int charCardIndex = 0;
            Button? targetCard = null;
            bool insertAtEnd = true;

            for (int i = 0; i < _dragSourcePanel.Children.Count; i++)
            {
                if (_dragSourcePanel.Children[i] is not Button btn || btn.DataContext is not Character)
                    continue;

                if (charCardIndex == insertIndex)
                {
                    targetCard = btn;
                    insertAtEnd = false;
                    break;
                }
                charCardIndex++;
            }

            Point indicatorPos;
            if (targetCard != null)
            {
                // Position to the left of this card
                var cardTopLeft = targetCard.TranslatePoint(new Point(0, 0), OverviewScroller);
                if (cardTopLeft == null)
                {
                    _insertIndicator.IsVisible = false;
                    return;
                }
                indicatorPos = cardTopLeft.Value;
            }
            else if (insertAtEnd)
            {
                // Position to the right of the last card
                Button? lastCard = null;
                for (int i = _dragSourcePanel.Children.Count - 1; i >= 0; i--)
                {
                    if (_dragSourcePanel.Children[i] is Button btn && btn.DataContext is Character)
                    {
                        lastCard = btn;
                        break;
                    }
                }
                if (lastCard == null)
                {
                    _insertIndicator.IsVisible = false;
                    return;
                }
                var lastTopLeft = lastCard.TranslatePoint(new Point(lastCard.Bounds.Width, 0), OverviewScroller);
                if (lastTopLeft == null)
                {
                    _insertIndicator.IsVisible = false;
                    return;
                }
                indicatorPos = lastTopLeft.Value;
            }
            else
            {
                _insertIndicator.IsVisible = false;
                return;
            }

            _insertIndicator.Height = 90;
            Canvas.SetLeft(_insertIndicator, indicatorPos.X - 1);
            Canvas.SetTop(_insertIndicator, indicatorPos.Y);
            _insertIndicator.IsVisible = true;
        }

        private void PerformReorder(int fromIndex, int toIndex)
        {
            if (_dragGroupName == null || _dragSourcePanel == null) return;

            if (_dragGroupName == "Ungrouped")
            {
                // Reorder in UngroupedCharacterOrder
                var characters = GetCharactersFromPanel(_dragSourcePanel);
                if (fromIndex >= characters.Count) return;

                var movedChar = characters[fromIndex];
                characters.RemoveAt(fromIndex);
                if (toIndex > characters.Count) toIndex = characters.Count;
                characters.Insert(toIndex, movedChar);

                // Update the settings list
                Settings.UngroupedCharacterOrder = characters.Select(c => c.Guid).ToList();
            }
            else
            {
                // Reorder within a named group's CharacterGuids
                var group = Settings.CharacterGroups.FirstOrDefault(g => g.Name == _dragGroupName);
                if (group == null) return;

                var guids = group.CharacterGuids;
                if (fromIndex >= guids.Count) return;

                var movedGuid = guids[fromIndex];
                guids.RemoveAt(fromIndex);
                if (toIndex > guids.Count) toIndex = guids.Count;
                guids.Insert(toIndex, movedGuid);
            }

            // Persist and notify
            Settings.Save();
            AppServices.EventAggregator?.Publish(Common.Events.SettingsChangedEvent.Instance);
        }

        private static List<Character> GetCharactersFromPanel(WrapPanel panel)
        {
            var result = new List<Character>();
            foreach (var child in panel.Children)
            {
                if (child is Button btn && btn.DataContext is Character ch)
                    result.Add(ch);
            }
            return result;
        }

        private static int GetCardIndex(WrapPanel panel, Button card)
        {
            int index = 0;
            foreach (var child in panel.Children)
            {
                if (child is Button btn && btn.DataContext is Character)
                {
                    if (btn == card) return index;
                    index++;
                }
            }
            return -1;
        }

        #endregion
    }
}
