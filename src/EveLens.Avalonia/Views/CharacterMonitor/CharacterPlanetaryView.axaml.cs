// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avalonia.Interactivity;
using EveLens.Common.Models;
using EveLens.Common.ViewModels;
using EveLens.Common.ViewModels.Lists;
using EveLens.Common.Constants;
using EveLens.Common.Enumerations.CCPAPI;
using EveLens.Common.Events;
using EveLens.Common.Services;
using EveLens.Common.Services.Planetary;
using EveLens.Avalonia.Controls;
using EveLens.Avalonia.Services;

namespace EveLens.Avalonia.Views.CharacterMonitor
{
    public partial class CharacterPlanetaryView : UserControl
    {
        private IDisposable? _dataUpdatedSub;
        private long _characterId;
        private PlanetaryListViewModel? _listViewModel;
        private PlanetaryDashboardViewModel? _dashboardViewModel;
        private ColonyFlowCanvas? _flowCanvas;
        private bool _showDashboard = true;

        public CharacterPlanetaryView()
        {
            InitializeComponent();
            LocalizeUI();
        }

        private void LocalizeUI()
        {
            EnableTitle.Text = Loc.Get("ListView.EnablePlanetary");
            EnableSubtext.Text = Loc.Get("ListView.EnableToFetch");
            EnableBtn.Content = Loc.Get("ListView.EnablePlanetaryBtn");
            ScopeTitle.Text = Loc.Get("ListView.ScopeNotAuthorized");
            ScopeSubtext.Text = Loc.Get("ListView.ScopeNotAuthorizedDesc");
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _dataUpdatedSub ??= AppServices.EventAggregator?.Subscribe<CharacterPlanetaryColoniesUpdatedEvent>(OnDataUpdated);
            LoadData();
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            if ((_dashboardViewModel != null || _listViewModel != null) && DataContext is Character)
                LoadData();
        }

        private void LoadData()
        {
            Character? character = DataContext as Character
                ?? (DataContext as ObservableCharacter)?.Character;
            if (character == null)
            {
                var parent = this.FindAncestorOfType<CharacterMonitorView>();
                character = (parent?.DataContext as ObservableCharacter)?.Character
                    ?? parent?.DataContext as Character;
            }
            if (character == null) return;

            _characterId = character.CharacterID;

            var parentView = this.FindAncestorOfType<CharacterMonitorView>();
            var oc = parentView?.DataContext as ObservableCharacter;
            var prompt = this.FindControl<Border>("EnablePrompt");
            var scopePrompt = this.FindControl<Border>("ScopePrompt");
            var content = this.FindControl<DockPanel>("DataContent");

            if (oc != null && !oc.IsEndpointEnabled(ESIAPICharacterMethods.PlanetaryColonies))
            {
                if (prompt != null) prompt.IsVisible = true;
                if (content != null) content.IsVisible = false;
                return;
            }
            if (prompt != null) prompt.IsVisible = false;

            if (oc != null && !oc.HasScopeFor(ESIAPICharacterMethods.PlanetaryColonies))
            {
                if (scopePrompt != null) scopePrompt.IsVisible = true;
                if (content != null) content.IsVisible = false;
                return;
            }
            if (scopePrompt != null) scopePrompt.IsVisible = false;
            if (content != null) content.IsVisible = true;

            if (_showDashboard)
                LoadDashboard(character);
            else
                LoadDataGrid(character);

            UpdateToggleVisuals();
        }

        private void LoadDashboard(Character character)
        {
            _dashboardViewModel ??= new PlanetaryDashboardViewModel();
            if (_dashboardViewModel.Character != character)
                _dashboardViewModel.Character = character;
            else
                _dashboardViewModel.Refresh();

            DashboardPanel.IsVisible = true;
            ItemsGrid.IsVisible = false;

            BuildColonyCards();
            UpdateStatusBar();
        }

        private void LoadDataGrid(Character character)
        {
            _listViewModel ??= new PlanetaryListViewModel();
            if (_listViewModel.Character != character)
                _listViewModel.Character = character;
            else
                _listViewModel.ForceRefresh();

            DashboardPanel.IsVisible = false;
            ItemsGrid.IsVisible = true;
            ItemsGrid.ItemsSource = _listViewModel.GroupedItems?.SelectMany(g => g.Items).ToList();
            UpdateStatusBar();
        }

        private void BuildColonyCards()
        {
            CardsPanel.Children.Clear();

            if (_dashboardViewModel == null) return;

            // Beta banner
            CardsPanel.Children.Add(BuildBetaBanner());

            if (_dashboardViewModel.Colonies.Count > 0)
            {
                foreach (var card in _dashboardViewModel.Colonies)
                    CardsPanel.Children.Add(BuildCard(card));
            }
            else
            {
                CardsPanel.Children.Add(BuildEmptyState());
            }
        }

        private static Control BuildBetaBanner()
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(6, 2, 6, 6)
            };

            var badge = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(180, 230, 180, 30)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 2),
                Child = new TextBlock
                {
                    Text = "BETA",
                    FontSize = FontScaleService.Caption,
                    FontWeight = FontWeight.Bold,
                    Foreground = Brushes.Black
                }
            };

            var desc = new TextBlock
            {
                Text = "Planetary data depends on ESI availability. Economics shown only when extraction data is present.",
                FontSize = FontScaleService.Caption,
                Foreground = new SolidColorBrush(Color.FromRgb(230, 180, 30)),
                VerticalAlignment = VerticalAlignment.Center,
                FontStyle = FontStyle.Italic
            };

            panel.Children.Add(badge);
            panel.Children.Add(desc);

            return new Border
            {
                Child = panel,
                Background = new SolidColorBrush(Color.FromArgb(20, 230, 180, 30)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 6),
                BorderBrush = new SolidColorBrush(Color.FromArgb(60, 230, 180, 30)),
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 0, 0, 4)
            };
        }

        private Control BuildCard(ColonyCardData card)
        {
            var (healthBrush, healthBgBrush, healthBorderBrush) = GetHealthVisuals(card.Health);

            // Security color
            IBrush secBrush;
            try { secBrush = new SolidColorBrush(Color.Parse(card.SecurityColor)); }
            catch { secBrush = Brushes.Gray; }

            var contentPanel = new StackPanel { Spacing = 6 };

            // Row 1: Planet name + sec badge
            var headerRow = new DockPanel();
            var secBadge = new Border
            {
                Background = secBrush,
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(5, 1),
                Child = new TextBlock
                {
                    Text = card.SecurityDisplay,
                    FontSize = FontScaleService.Caption,
                    Foreground = Brushes.White,
                    FontWeight = FontWeight.Bold
                },
                VerticalAlignment = VerticalAlignment.Center,
                Opacity = 0.9
            };
            DockPanel.SetDock(secBadge, Dock.Right);
            headerRow.Children.Add(secBadge);
            headerRow.Children.Add(new TextBlock
            {
                Text = card.PlanetName,
                FontSize = FontScaleService.Subheading,
                FontWeight = FontWeight.SemiBold,
                Foreground = Application.Current!.FindResource("EveAccentPrimaryBrush") as IBrush ?? Brushes.Gold,
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            contentPanel.Children.Add(headerRow);

            // Row 2: Type + System
            contentPanel.Children.Add(new TextBlock
            {
                Text = $"{card.PlanetTypeName} -- {card.SystemName}",
                FontSize = FontScaleService.Caption,
                Foreground = Application.Current!.FindResource("EveTextDisabledBrush") as IBrush ?? Brushes.Gray
            });

            // Row 3: HERO TIMER — big countdown
            var timerPanel = new Border
            {
                Background = healthBgBrush,
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 8),
                Margin = new Thickness(0, 2)
            };
            var timerContent = new DockPanel();
            var timerText = new TextBlock
            {
                Text = card.TimeRemainingDisplay,
                FontSize = FontScaleService.Title,
                FontWeight = FontWeight.Bold,
                Foreground = healthBrush,
                VerticalAlignment = VerticalAlignment.Center
            };
            var timerLabel = new TextBlock
            {
                Text = card.HealthLabel,
                FontSize = FontScaleService.Caption,
                Foreground = healthBrush,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                Opacity = 0.8
            };
            DockPanel.SetDock(timerLabel, Dock.Right);
            timerContent.Children.Add(timerLabel);
            timerContent.Children.Add(timerText);
            timerPanel.Child = timerContent;
            contentPanel.Children.Add(timerPanel);

            // Row 4: Extractors + Factories
            var countsRow = new DockPanel { Margin = new Thickness(0, 2, 0, 0) };
            var ecuText = new TextBlock
            {
                Text = $"{card.ActiveExtractors}/{card.TotalExtractors} extractors",
                FontSize = FontScaleService.Small,
                Foreground = card.ActiveExtractors == card.TotalExtractors
                    ? (Application.Current!.FindResource("EveTextPrimaryBrush") as IBrush ?? Brushes.White)
                    : (Application.Current!.FindResource("EveWarningYellowBrush") as IBrush ?? Brushes.Orange)
            };
            var factoryText = new TextBlock
            {
                Text = $"{card.FactoryCount} factories",
                FontSize = FontScaleService.Small,
                Foreground = Application.Current!.FindResource("EveTextSecondaryBrush") as IBrush ?? Brushes.LightGray,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            DockPanel.SetDock(factoryText, Dock.Right);
            countsRow.Children.Add(factoryText);
            countsRow.Children.Add(ecuText);
            contentPanel.Children.Add(countsRow);

            // Row 5: Output + Economics (conditional)
            if (card.HasYieldData)
            {
                var econRow = new DockPanel { Margin = new Thickness(0, 2, 0, 0) };
                var outputLabel = new TextBlock
                {
                    Text = $"{card.OutputTierLabel}: {card.OutputName}",
                    FontSize = FontScaleService.Small,
                    Foreground = Application.Current!.FindResource("EveTextPrimaryBrush") as IBrush ?? Brushes.White,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                var iskLabel = new TextBlock
                {
                    Text = FormatIsk(card.NetIskPerDay) + "/day",
                    FontSize = FontScaleService.Small,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = Application.Current!.FindResource("EveSuccessGreenBrush") as IBrush ?? Brushes.LimeGreen,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                DockPanel.SetDock(iskLabel, Dock.Right);
                econRow.Children.Add(iskLabel);
                econRow.Children.Add(outputLabel);
                contentPanel.Children.Add(econRow);
            }
            else if (card.Health != ColonyHealthStatus.NoExtractors)
            {
                contentPanel.Children.Add(new TextBlock
                {
                    Text = "No yield data from ESI",
                    FontSize = FontScaleService.Caption,
                    Foreground = Application.Current!.FindResource("EveTextDisabledBrush") as IBrush ?? Brushes.DarkGray,
                    FontStyle = FontStyle.Italic,
                    Margin = new Thickness(0, 2, 0, 0)
                });
            }

            // Card border
            var cardBorder = new Border
            {
                Width = 300,
                MinHeight = 140,
                Padding = new Thickness(14, 12),
                CornerRadius = new CornerRadius(8),
                BorderThickness = new Thickness(1),
                BorderBrush = healthBorderBrush,
                Background = Application.Current!.FindResource("EveBackgroundMediumBrush") as IBrush ?? Brushes.DarkSlateGray,
                Margin = new Thickness(5),
                Child = contentPanel
            };

            // Left accent bar
            var accentBar = new Border
            {
                Width = 3,
                Background = healthBrush,
                HorizontalAlignment = HorizontalAlignment.Left,
                CornerRadius = new CornerRadius(8, 0, 0, 8)
            };

            var cardGrid = new Grid { Width = 300, MinHeight = 140, Margin = new Thickness(5) };
            cardGrid.Children.Add(cardBorder);
            cardGrid.Children.Add(accentBar);

            var btn = new Button
            {
                Content = cardGrid,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                Tag = card.Colony,
                Cursor = new global::Avalonia.Input.Cursor(global::Avalonia.Input.StandardCursorType.Hand)
            };
            btn.Click += OnColonyCardClick;

            return btn;
        }

        private Control BuildEmptyState()
        {
            return new Border
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(20),
                Child = new TextBlock
                {
                    Text = "No planetary colonies found. Set up extractors in-game to see data here.",
                    FontSize = FontScaleService.Subheading,
                    Foreground = Application.Current!.FindResource("EveTextDisabledBrush") as IBrush ?? Brushes.Gray,
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center
                }
            };
        }

        private void UpdateStatusBar()
        {
            if (_showDashboard && _dashboardViewModel != null)
            {
                var parts = $"Colonies: {_dashboardViewModel.TotalColonies}";
                if (_dashboardViewModel.IdleColonies > 0)
                    parts += $" | {_dashboardViewModel.IdleColonies} idle";
                if (_dashboardViewModel.ExpiringColonies > 0)
                    parts += $" | {_dashboardViewModel.ExpiringColonies} expiring soon";
                if (_dashboardViewModel.HasAnyEconomicsData)
                    parts += $" | Est. {FormatIsk(_dashboardViewModel.TotalNetIskPerDay)}/day";
                StatusBar.Text = parts;
            }
            else if (_listViewModel != null)
            {
                var items = _listViewModel.GroupedItems?.SelectMany(g => g.Items);
                int count = items?.Count() ?? 0;
                StatusBar.Text = $"Items: {count}";
            }
        }

        private void UpdateToggleVisuals()
        {
            if (_showDashboard)
            {
                DashboardToggle.Background = Application.Current!.FindResource("EveAccentPrimaryBrush") as IBrush ?? Brushes.Gold;
                DashboardToggle.Foreground = Brushes.Black;
                DataToggle.Background = Brushes.Transparent;
                DataToggle.Foreground = Application.Current!.FindResource("EveTextSecondaryBrush") as IBrush ?? Brushes.Gray;
            }
            else
            {
                DataToggle.Background = Application.Current!.FindResource("EveAccentPrimaryBrush") as IBrush ?? Brushes.Gold;
                DataToggle.Foreground = Brushes.Black;
                DashboardToggle.Background = Brushes.Transparent;
                DashboardToggle.Foreground = Application.Current!.FindResource("EveTextSecondaryBrush") as IBrush ?? Brushes.Gray;
            }
        }

        private static (IBrush fill, IBrush bg, IBrush border) GetHealthVisuals(ColonyHealthStatus health)
        {
            return health switch
            {
                ColonyHealthStatus.Optimal => (
                    Brushes.LimeGreen,
                    new SolidColorBrush(Color.FromArgb(25, 50, 205, 50)),
                    new SolidColorBrush(Color.FromArgb(50, 50, 205, 50))),
                ColonyHealthStatus.Expiring => (
                    Brushes.Orange,
                    new SolidColorBrush(Color.FromArgb(25, 255, 165, 0)),
                    new SolidColorBrush(Color.FromArgb(50, 255, 165, 0))),
                ColonyHealthStatus.Idle => (
                    Brushes.Red,
                    new SolidColorBrush(Color.FromArgb(25, 255, 60, 60)),
                    new SolidColorBrush(Color.FromArgb(50, 255, 60, 60))),
                _ => (
                    Brushes.Gray,
                    new SolidColorBrush(Color.FromArgb(15, 128, 128, 128)),
                    new SolidColorBrush(Color.FromArgb(40, 128, 128, 128)))
            };
        }

        private static string FormatIsk(double isk)
        {
            if (Math.Abs(isk) >= 1_000_000_000)
                return $"{isk / 1_000_000_000:F1}B";
            if (Math.Abs(isk) >= 1_000_000)
                return $"{isk / 1_000_000:F1}M";
            if (Math.Abs(isk) >= 1_000)
                return $"{isk / 1_000:F1}K";
            return $"{isk:F0}";
        }

        // ── Event handlers ──

        private void OnToggleDashboard(object? sender, RoutedEventArgs e)
        {
            if (_showDashboard) return;
            _showDashboard = true;
            LoadData();
        }

        private void OnToggleData(object? sender, RoutedEventArgs e)
        {
            if (!_showDashboard) return;
            _showDashboard = false;
            LoadData();
        }

        private void OnFilterChanged(object? sender, TextChangedEventArgs e)
        {
            string text = FilterBox.Text ?? "";
            ClearFilterBtn.IsVisible = text.Length > 0;

            if (_showDashboard && _dashboardViewModel != null)
            {
                _dashboardViewModel.TextFilter = text;
                BuildColonyCards();
                UpdateStatusBar();
            }
            else if (_listViewModel != null)
            {
                _listViewModel.TextFilter = text;
                ItemsGrid.ItemsSource = _listViewModel.GroupedItems?.SelectMany(g => g.Items).ToList();
                UpdateStatusBar();
            }
        }

        private void OnClearFilter(object? sender, RoutedEventArgs e)
        {
            FilterBox.Text = "";
        }

        private void OnColonyCardClick(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not PlanetaryColony colony) return;
            ShowColonyDetail(colony);
        }

        private void ShowColonyDetail(PlanetaryColony colony)
        {
            _flowCanvas ??= new ColonyFlowCanvas();
            _flowCanvas.SetColony(colony);
            _flowCanvas.MinHeight = 300;

            FlowCanvasHost.Content = _flowCanvas;
            DetailTitle.Text = $"{colony.PlanetName} -- {colony.PlanetTypeName} -- {colony.SolarSystem?.Name ?? "Unknown"}";

            // Stats panel
            DetailStatsPanel.Children.Clear();
            var analysis = ProductionChainAnalyzer.Analyze(colony);
            var (healthBrush, _, _) = GetHealthVisuals(analysis.Health);

            // Timer stat (hero)
            var timeUntilIdle = analysis.TimeUntilFirstIdle;
            string timerDisplay = analysis.Health == ColonyHealthStatus.Idle ? "IDLE"
                : timeUntilIdle <= TimeSpan.Zero ? "Expired"
                : timeUntilIdle.TotalDays >= 1 ? $"{(int)timeUntilIdle.TotalDays}d {timeUntilIdle.Hours}h"
                : timeUntilIdle.TotalHours >= 1 ? $"{(int)timeUntilIdle.TotalHours}h {timeUntilIdle.Minutes}m"
                : $"{timeUntilIdle.Minutes}m";

            DetailStatsPanel.Children.Add(BuildDetailStat("Time Left", timerDisplay, healthBrush));
            DetailStatsPanel.Children.Add(BuildDetailStat("Status", analysis.Health.ToString(), healthBrush));
            DetailStatsPanel.Children.Add(BuildDetailStat("Extractors",
                $"{analysis.ActiveExtractorCount}/{analysis.TotalExtractorCount}",
                analysis.ActiveExtractorCount == analysis.TotalExtractorCount ? Brushes.LimeGreen : Brushes.Orange));
            DetailStatsPanel.Children.Add(BuildDetailStat("Factories", analysis.FactoryCount.ToString(), null));

            // Conditional economics
            if (analysis.HasYieldData)
            {
                int cceLevel = 0;
                if (_dashboardViewModel?.Character != null)
                {
                    var skill = Common.Data.StaticSkills.GetSkillByName("Customs Code Expertise");
                    if (skill != null) cceLevel = (int)_dashboardViewModel.Character.GetSkillLevel(skill);
                }
                var econ = new ColonyEconomicsService(cceLevel).Calculate(analysis);

                DetailStatsPanel.Children.Add(BuildDetailStat("Gross/day", FormatIsk(econ.GrossIskPerDay), Brushes.LimeGreen));
                DetailStatsPanel.Children.Add(BuildDetailStat("Tax/day", $"-{FormatIsk(econ.TaxCostPerDay)}", Brushes.Orange));
                DetailStatsPanel.Children.Add(BuildDetailStat("Net/day", FormatIsk(econ.NetIskPerDay), Brushes.LimeGreen));
                DetailStatsPanel.Children.Add(BuildDetailStat("Tax Rate", $"{econ.TaxRate * 100:F1}%", null));
            }
            else
            {
                DetailStatsPanel.Children.Add(BuildDetailStat("Economics", "No data", Brushes.Gray));
            }

            // Switch to detail view
            DashboardPanel.IsVisible = false;
            DetailPanel.IsVisible = true;
        }

        private void OnBackToCards(object? sender, RoutedEventArgs e)
        {
            DetailPanel.IsVisible = false;
            DashboardPanel.IsVisible = true;
            _flowCanvas?.Clear();
        }

        private static Border BuildDetailStat(string label, string value, IBrush? valueBrush)
        {
            var panel = new StackPanel { Spacing = 2, HorizontalAlignment = HorizontalAlignment.Center };
            panel.Children.Add(new TextBlock
            {
                Text = value,
                FontSize = FontScaleService.Body,
                FontWeight = FontWeight.Bold,
                Foreground = valueBrush ?? (Application.Current!.FindResource("EveTextPrimaryBrush") as IBrush ?? Brushes.White),
                HorizontalAlignment = HorizontalAlignment.Center
            });
            panel.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = FontScaleService.Caption,
                Foreground = Application.Current!.FindResource("EveTextDisabledBrush") as IBrush ?? Brushes.Gray,
                HorizontalAlignment = HorizontalAlignment.Center
            });
            return new Border
            {
                Padding = new Thickness(12, 6),
                CornerRadius = new CornerRadius(6),
                Background = Application.Current!.FindResource("EveBackgroundDarkBrush") as IBrush ?? Brushes.Black,
                Margin = new Thickness(4),
                Child = panel
            };
        }

        private void OnEnableEndpoint(object? sender, RoutedEventArgs e)
        {
            var parentView = this.FindAncestorOfType<CharacterMonitorView>();
            var oc = parentView?.DataContext as ObservableCharacter;
            oc?.EnableEndpoint(ESIAPICharacterMethods.PlanetaryColonies);
            LoadData();
        }

        private void OnDataUpdated(CharacterPlanetaryColoniesUpdatedEvent evt)
        {
            if (evt.Character?.CharacterID == _characterId)
                global::Avalonia.Threading.Dispatcher.UIThread.Post(LoadData);
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            _dataUpdatedSub?.Dispose();
            _dataUpdatedSub = null;
            _dashboardViewModel?.Dispose();
            _dashboardViewModel = null;
            _listViewModel?.Dispose();
            _listViewModel = null;
        }
    }
}
