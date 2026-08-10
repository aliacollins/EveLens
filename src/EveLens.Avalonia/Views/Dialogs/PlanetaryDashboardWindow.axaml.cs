// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using EveLens.Avalonia.Services;
using EveLens.Common.Helpers;
using EveLens.Common.Models;
using EveLens.Common.Services;
using EveLens.Common.Services.Planetary;
using EveLens.Common.ViewModels;

namespace EveLens.Avalonia.Views.Dialogs
{
    public partial class PlanetaryDashboardWindow : Window
    {
        private readonly PlanetaryOverviewViewModel _vm = new();
        private bool _attentionOnly;
        private string _filter = "";
        private IDisposable? _pricesSub;

        public PlanetaryDashboardWindow()
        {
            InitializeComponent();
            _vm.Refresh();
            RebuildUI();

            // Market prices load asynchronously after the first request; without a repaint
            // the ISK/day column stays 0 until the window is reopened (Issue #66).
            _pricesSub = AppServices.EventAggregator?.Subscribe<Common.Events.ItemPricesUpdatedEvent>(
                _ => global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    _vm.Refresh();
                    RebuildUI();
                }));
        }

        protected override void OnClosed(EventArgs e)
        {
            _pricesSub?.Dispose();
            _pricesSub = null;
            base.OnClosed(e);
        }

        private void RebuildUI()
        {
            BuildSummaryCards();
            BuildContent();
            UpdateStatus();
        }

        private void BuildSummaryCards()
        {
            SummaryCards.Children.Clear();

            SummaryCards.Children.Add(BuildStat("Characters", _vm.TotalCharacters.ToString(), null));
            SummaryCards.Children.Add(BuildStat("Colonies", _vm.TotalColonies.ToString(), null));

            var activeBrush = _vm.IdleExtractors > 0 ? Brushes.Orange : Brushes.LimeGreen;
            SummaryCards.Children.Add(BuildStat("Active ECU",
                $"{_vm.ActiveExtractors}/{_vm.ActiveExtractors + _vm.IdleExtractors}", activeBrush));

            if (_vm.ExpiringCount > 0)
                SummaryCards.Children.Add(BuildStat("Expiring", _vm.ExpiringCount.ToString(), Brushes.Orange));

            if (_vm.CharactersNeedingAttention > 0)
                SummaryCards.Children.Add(BuildStat("Need Attention",
                    _vm.CharactersNeedingAttention.ToString(), Brushes.Red));

            if (_vm.HasAnyEconomicsData)
                SummaryCards.Children.Add(BuildStat("Est. Net/day", FormatIsk(_vm.TotalNetIskPerDay), Brushes.LimeGreen));
        }

        private static Border BuildStat(string label, string value, IBrush? valueBrush)
        {
            var panel = new StackPanel { Spacing = 1, HorizontalAlignment = HorizontalAlignment.Center };
            panel.Children.Add(new TextBlock
            {
                Text = value,
                FontSize = FontScaleService.Subheading,
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
                Child = panel
            };
        }

        private void BuildContent()
        {
            ContentPanel.Children.Clear();

            // Beta banner
            ContentPanel.Children.Add(BuildBetaBanner());

            var entries = _vm.Entries.AsEnumerable();

            if (_attentionOnly)
                entries = entries.Where(e => e.NeedsAttention);

            if (!string.IsNullOrWhiteSpace(_filter))
                entries = entries.Where(e => e.CharacterName.Contains(_filter, StringComparison.OrdinalIgnoreCase));

            var entryList = entries.ToList();

            // Build alert timeline section if there are expiring/idle extractors
            var alerts = BuildAlertTimeline(entryList);
            if (alerts != null)
                ContentPanel.Children.Add(alerts);

            // Character rows
            ContentPanel.Children.Add(BuildSectionHeader("All Characters"));
            ContentPanel.Children.Add(BuildHeaderRow());

            foreach (var entry in entryList)
                ContentPanel.Children.Add(BuildCharacterRow(entry));

            if (entryList.Count == 0)
                ContentPanel.Children.Add(BuildEmptyMessage());
        }

        private Control? BuildAlertTimeline(List<PlanetaryCharacterEntry> entries)
        {
            var alertItems = new List<(string charName, string planetName, TimeSpan timeLeft, ColonyHealthStatus health)>();

            foreach (var entry in entries)
            {
                if (entry.Character is not CCPCharacter ccp) continue;

                foreach (var colony in ccp.PlanetaryColonies)
                {
                    var analysis = ProductionChainAnalyzer.Analyze(colony);
                    if (analysis.Health == ColonyHealthStatus.Idle)
                    {
                        alertItems.Add((entry.CharacterName, colony.PlanetName, TimeSpan.Zero, ColonyHealthStatus.Idle));
                    }
                    else if (analysis.Health == ColonyHealthStatus.Expiring)
                    {
                        alertItems.Add((entry.CharacterName, colony.PlanetName, analysis.TimeUntilFirstIdle, ColonyHealthStatus.Expiring));
                    }
                }
            }

            if (alertItems.Count == 0) return null;

            // Sort: idle first, then by time ascending
            alertItems.Sort((a, b) =>
            {
                if (a.health == ColonyHealthStatus.Idle && b.health != ColonyHealthStatus.Idle) return -1;
                if (b.health == ColonyHealthStatus.Idle && a.health != ColonyHealthStatus.Idle) return 1;
                return a.timeLeft.CompareTo(b.timeLeft);
            });

            var section = new StackPanel { Spacing = 3 };
            section.Children.Add(BuildSectionHeader("Alerts"));

            foreach (var alert in alertItems.Take(12))
            {
                var (brush, bgBrush) = alert.health == ColonyHealthStatus.Idle
                    ? (Brushes.Red, new SolidColorBrush(Color.FromArgb(20, 255, 60, 60)))
                    : (Brushes.Orange, new SolidColorBrush(Color.FromArgb(15, 255, 165, 0)));

                string timeText = alert.health == ColonyHealthStatus.Idle ? "IDLE"
                    : alert.timeLeft.TotalDays >= 1 ? $"{(int)alert.timeLeft.TotalDays}d {alert.timeLeft.Hours}h"
                    : alert.timeLeft.TotalHours >= 1 ? $"{(int)alert.timeLeft.TotalHours}h {alert.timeLeft.Minutes}m"
                    : $"{alert.timeLeft.Minutes}m";

                var row = new Border
                {
                    Background = bgBrush,
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(12, 6),
                    Margin = new Thickness(0, 1)
                };

                var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto") };

                var timeBlock = new TextBlock
                {
                    Text = timeText,
                    FontSize = FontScaleService.Body,
                    FontWeight = FontWeight.Bold,
                    Foreground = brush,
                    VerticalAlignment = VerticalAlignment.Center,
                    MinWidth = 70
                };
                Grid.SetColumn(timeBlock, 0);
                grid.Children.Add(timeBlock);

                var descBlock = new TextBlock
                {
                    Text = $"{alert.charName} -- {alert.planetName}",
                    FontSize = FontScaleService.Body,
                    Foreground = Application.Current!.FindResource("EveTextPrimaryBrush") as IBrush ?? Brushes.White,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(12, 0, 0, 0)
                };
                Grid.SetColumn(descBlock, 1);
                grid.Children.Add(descBlock);

                var statusBlock = new TextBlock
                {
                    Text = alert.health == ColonyHealthStatus.Idle ? "Needs restart" : "Expiring soon",
                    FontSize = FontScaleService.Caption,
                    Foreground = brush,
                    VerticalAlignment = VerticalAlignment.Center,
                    Opacity = 0.8
                };
                Grid.SetColumn(statusBlock, 2);
                grid.Children.Add(statusBlock);

                row.Child = grid;
                section.Children.Add(row);
            }

            if (alertItems.Count > 12)
            {
                section.Children.Add(new TextBlock
                {
                    Text = $"+ {alertItems.Count - 12} more...",
                    FontSize = FontScaleService.Caption,
                    Foreground = Application.Current!.FindResource("EveTextDisabledBrush") as IBrush ?? Brushes.Gray,
                    Margin = new Thickness(12, 4, 0, 0)
                });
            }

            return new Border
            {
                Child = section,
                Background = Application.Current!.FindResource("EveBackgroundMediumBrush") as IBrush ?? Brushes.DarkSlateGray,
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 8),
                Margin = new Thickness(0, 0, 0, 8)
            };
        }

        private static Control BuildBetaBanner()
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                HorizontalAlignment = HorizontalAlignment.Left
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
                Text = "PI dashboard is in beta. Economics require ESI extraction data which may not be available for all colonies.",
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
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
        }

        private static TextBlock BuildSectionHeader(string text) => new TextBlock
        {
            Text = text,
            FontSize = FontScaleService.Subheading,
            FontWeight = FontWeight.SemiBold,
            Foreground = Application.Current!.FindResource("EveAccentPrimaryBrush") as IBrush ?? Brushes.Gold,
            Margin = new Thickness(4, 8, 0, 4)
        };

        private TextBlock BuildEmptyMessage() => new TextBlock
        {
            Text = _attentionOnly
                ? "No characters need attention right now."
                : "No characters with planetary colonies found.",
            FontSize = FontScaleService.Body,
            Foreground = Application.Current!.FindResource("EveTextDisabledBrush") as IBrush ?? Brushes.Gray,
            Margin = new Thickness(20),
            HorizontalAlignment = HorizontalAlignment.Center
        };

        private static Grid BuildHeaderRow()
        {
            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,60,80,90,90"),
                Margin = new Thickness(4, 2),
            };

            var headers = new[] { "Character", "Colonies", "ECU", "Next Idle", "Status" };
            for (int i = 0; i < headers.Length; i++)
            {
                var tb = new TextBlock
                {
                    Text = headers[i],
                    FontSize = FontScaleService.Small,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = Application.Current!.FindResource("EveTextSecondaryBrush") as IBrush ?? Brushes.Gray,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(4, 0)
                };
                Grid.SetColumn(tb, i);
                grid.Children.Add(tb);
            }

            return grid;
        }

        private Border BuildCharacterRow(PlanetaryCharacterEntry entry)
        {
            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,60,80,90,90"),
                MinHeight = 36,
            };

            // Name
            var nameBlock = new TextBlock
            {
                Text = entry.CharacterName,
                FontSize = FontScaleService.Body,
                FontWeight = FontWeight.SemiBold,
                Foreground = Application.Current!.FindResource("EveAccentPrimaryBrush") as IBrush ?? Brushes.Gold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0)
            };
            Grid.SetColumn(nameBlock, 0);
            grid.Children.Add(nameBlock);

            // Colonies
            AddCell(grid, 1, entry.ColonyCount.ToString());

            // ECU (active/total)
            var ecuBrush = entry.IdleExtractorCount > 0
                ? (Application.Current!.FindResource("EveWarningYellowBrush") as IBrush ?? Brushes.Orange)
                : (Application.Current!.FindResource("EveTextPrimaryBrush") as IBrush ?? Brushes.White);
            AddCell(grid, 2, $"{entry.ActiveExtractorCount}/{entry.TotalExtractorCount}", ecuBrush);

            // Time until idle
            var timeBrush = entry.WorstHealth == ColonyHealthStatus.Idle ? Brushes.Red
                : entry.WorstHealth == ColonyHealthStatus.Expiring ? Brushes.Orange
                : (Application.Current!.FindResource("EveTextSecondaryBrush") as IBrush ?? Brushes.Gray);
            AddCell(grid, 3, entry.TimeDisplay, timeBrush);

            // Health status
            var healthBrush = GetHealthBrush(entry.WorstHealth);
            AddCell(grid, 4, entry.HealthDisplay, healthBrush);

            // Row container
            var rowBorder = new Border
            {
                Child = grid,
                Padding = new Thickness(8, 4),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(0, 1),
                Background = entry.NeedsAttention
                    ? new SolidColorBrush(Color.FromArgb(15, 255, 80, 80))
                    : Brushes.Transparent,
                Cursor = new Cursor(StandardCursorType.Hand)
            };

            var character = entry.Character;
            rowBorder.PointerPressed += (_, _) => NavigateToCharacterPlanetary(character);

            if (entry.NeedsAttention)
            {
                var outerGrid = new Grid();
                outerGrid.Children.Add(rowBorder);
                outerGrid.Children.Add(new Border
                {
                    Width = 3,
                    Background = entry.WorstHealth == ColonyHealthStatus.Idle ? Brushes.Red : Brushes.Orange,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    CornerRadius = new CornerRadius(3, 0, 0, 3)
                });
                return new Border { Child = outerGrid, Margin = new Thickness(0, 1) };
            }

            return rowBorder;
        }

        private static void AddCell(Grid grid, int col, string text, IBrush? foreground = null)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontSize = FontScaleService.Body,
                Foreground = foreground ?? (Application.Current!.FindResource("EveTextPrimaryBrush") as IBrush ?? Brushes.White),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0)
            };
            Grid.SetColumn(tb, col);
            grid.Children.Add(tb);
        }

        private static IBrush GetHealthBrush(ColonyHealthStatus health) => health switch
        {
            ColonyHealthStatus.Optimal => Brushes.LimeGreen,
            ColonyHealthStatus.Expiring => Brushes.Orange,
            ColonyHealthStatus.Idle => Brushes.Red,
            _ => Brushes.Gray
        };

        private void UpdateStatus()
        {
            var total = _vm.TotalCharacters;
            var showing = _attentionOnly ? _vm.Entries.Count(e => e.NeedsAttention) : _vm.Entries.Count;
            StatusBar.Text = $"Showing {showing} of {total} characters | " +
                             $"{_vm.TotalColonies} colonies, {_vm.ActiveExtractors + _vm.IdleExtractors} extractors";
        }

        private static string FormatIsk(double isk) => FormattingHelper.FormatIsk(isk);

        // ── Event handlers ──

        private void OnRefresh(object? sender, RoutedEventArgs e)
        {
            _vm.Refresh();
            RebuildUI();
        }

        private void OnToggleAttentionOnly(object? sender, RoutedEventArgs e)
        {
            _attentionOnly = !_attentionOnly;
            AttentionOnlyBtn.Background = _attentionOnly
                ? (Application.Current!.FindResource("EveAccentPrimaryBrush") as IBrush ?? Brushes.Gold)
                : Brushes.Transparent;
            AttentionOnlyBtn.Foreground = _attentionOnly
                ? Brushes.Black
                : (Application.Current!.FindResource("EveTextSecondaryBrush") as IBrush ?? Brushes.Gray);
            BuildContent();
            UpdateStatus();
        }

        private void OnFilterChanged(object? sender, TextChangedEventArgs e)
        {
            _filter = FilterBox.Text ?? "";
            BuildContent();
            UpdateStatus();
        }

        private void NavigateToCharacterPlanetary(Character character)
        {
            if (Owner is not MainWindow mainWindow) return;

            Close();

            mainWindow.SelectCharacter(character);

            global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                var mainContent = mainWindow.FindControl<ContentControl>("MainContent");
                if (mainContent?.Content is CharacterMonitor.CharacterMonitorView monitorView)
                {
                    var tabs = monitorView.FindControl<TabControl>("SubTabs");
                    if (tabs == null) return;

                    for (int i = 0; i < tabs.Items.Count; i++)
                    {
                        if (tabs.Items[i] is TabItem tab && tab.Name == "TabPI")
                        {
                            tabs.SelectedIndex = i;
                            break;
                        }
                    }
                }
            }, global::Avalonia.Threading.DispatcherPriority.Background);
        }
    }
}
