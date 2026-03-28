// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using EveLens.Avalonia.Converters;
using EveLens.Avalonia.Services;
using EveLens.Common;
using EveLens.Common.Models;
using EveLens.Common.Service;
using EveLens.Common.Services;
using EveLens.Common.SettingsObjects;
using EveLens.Common.ViewModels;

namespace EveLens.Avalonia.Views.Dialogs
{
    public partial class SkillFarmDashboardWindow : Window
    {
        private readonly SkillFarmDashboardViewModel _vm = new();
        private string _filter = string.Empty;
        private bool _readyOnly;

        public SkillFarmDashboardWindow()
        {
            InitializeComponent();

            _vm.Refresh();
            RebuildUI();

            // Fetch prices from ESI async, then rebuild when they arrive
            FetchPricesAndRebuild();
        }

        private async void FetchPricesAndRebuild()
        {
            try
            {
                await _vm.RefreshPricesAsync();
                _vm.Refresh();
                RebuildUI();
            }
            catch { }
        }

        private void RebuildUI()
        {
            BuildPricesBar();
            BuildSummaryCards();
            BuildAlerts();
            BuildCharacterTable();
            UpdateStatus();
        }

        #region Market Prices Bar

        private void BuildPricesBar()
        {
            PricesPanel.Children.Clear();

            var pairs = new[]
            {
                ("Large Injector", _vm.InjectorPrice),
                ("Skill Extractor", _vm.ExtractorPrice),
                ("PLEX", _vm.PlexPrice),
                ("Profit/Extract", _vm.InjectorPrice - _vm.ExtractorPrice)
            };

            foreach (var (label, price) in pairs)
            {
                var lbl = new TextBlock
                {
                    Text = $"{label}:",
                    FontSize = FontScaleService.Small,
                    Foreground = FindBrush("EveTextDisabledBrush"),
                    VerticalAlignment = VerticalAlignment.Center
                };
                var val = new TextBlock
                {
                    Text = price > 0 ? FormatIsk(price) : "loading...",
                    FontSize = FontScaleService.Small,
                    Foreground = price > 0 ? FindBrush("EveSuccessGreenBrush") : FindBrush("EveTextDisabledBrush"),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(4, 0, 0, 0)
                };

                var panel = new StackPanel { Orientation = Orientation.Horizontal };
                panel.Children.Add(lbl);
                panel.Children.Add(val);
                PricesPanel.Children.Add(panel);
            }
        }

        #endregion

        #region Summary Cards

        private void BuildSummaryCards()
        {
            SummaryPanel.Children.Clear();

            int totalInj = (int)_vm.TotalExtractionsToday;
            double extractorCost = totalInj * _vm.ExtractorPrice;
            double injectorRevenue = totalInj * _vm.InjectorPrice;
            double netProfit = _vm.TotalRevenueToday;
            double profitPerExtract = _vm.InjectorPrice > 0
                ? _vm.InjectorPrice - _vm.ExtractorPrice
                    - (_vm.InjectorPrice * 0.02) // ~2% tax at Accounting V
                : 0;

            SummaryPanel.Children.Add(BuildStatCard(
                $"{totalInj}", "Injectors",
                $"{_vm.ReadyCount} ready \u00B7 {_vm.PausedCount} paused",
                FindBrush("EveAccentPrimaryBrush")));

            SummaryPanel.Children.Add(BuildStatCard(
                _vm.PricesLoaded ? FormatIsk(extractorCost) : "...",
                "Extractor Cost",
                $"{totalInj} \u00D7 {FormatIsk(_vm.ExtractorPrice)}",
                FindBrush("EveErrorRedBrush")));

            SummaryPanel.Children.Add(BuildStatCard(
                _vm.PricesLoaded ? FormatIsk(injectorRevenue) : "...",
                "Injector Revenue",
                $"{totalInj} \u00D7 {FormatIsk(_vm.InjectorPrice)}",
                FindBrush("EveSuccessGreenBrush")));

            // Net Profit + Best Seller combined
            var bestSeller = _vm.Entries
                .OrderBy(e => e.SalesTaxPercent)
                .FirstOrDefault();

            string netDetail = "after tax";
            double displayNet = netProfit;
            if (bestSeller != null && _vm.PricesLoaded && totalInj > 0)
            {
                double bestTaxRate = bestSeller.SalesTaxPercent / 100.0;
                double optimizedNet = (totalInj * _vm.InjectorPrice * (1 - bestTaxRate)) - extractorCost;
                if (optimizedNet > netProfit)
                {
                    displayNet = optimizedNet;
                    netDetail = $"sell all via {bestSeller.Character.Name.Split(' ')[0]} ({bestSeller.SalesTaxPercent:F1}% tax)";
                }
            }

            SummaryPanel.Children.Add(BuildStatCard(
                _vm.PricesLoaded ? FormatIsk(displayNet) : "...",
                "Net Profit",
                netDetail,
                displayNet > 0 ? FindBrush("EveSuccessGreenBrush") : FindBrush("EveErrorRedBrush")));

            SummaryPanel.Children.Add(BuildStatCard(
                _vm.PricesLoaded ? FormatIsk(profitPerExtract) : "...",
                "Per Extraction",
                $"PLEX: {FormatIsk(_vm.PlexPrice)}",
                FindBrush("EveAccentPrimaryBrush")));

            // ── MONTHLY ECONOMICS ──
            // Calculate ongoing monthly costs and revenue based on actual SP/hr
            int farmAccounts = _vm.TotalCharacters;
            double totalSpPerHour = _vm.Entries.Sum(e => e.SpPerHour);

            // If all paused, estimate potential with +5 implants (2700 SP/hr per char)
            bool allPaused = totalSpPerHour == 0 && farmAccounts > 0;
            double estimatedSpPerHour = allPaused ? farmAccounts * 2700.0 : totalSpPerHour;
            double monthlySpTotal = estimatedSpPerHour * 24 * 30;
            double monthlyExtractions = monthlySpTotal / 500_000;
            double omegaCostPerMonth = farmAccounts * 500.0 * _vm.PlexPrice;

            double bestTaxRateForMonthly = bestSeller != null
                ? bestSeller.SalesTaxPercent / 100.0 : 0.075;
            double monthlyRevenue = monthlyExtractions * _vm.InjectorPrice * (1 - bestTaxRateForMonthly);
            double monthlyExtractorCost = monthlyExtractions * _vm.ExtractorPrice;
            double monthlyNet = monthlyRevenue - monthlyExtractorCost - omegaCostPerMonth;
            double omegaSavings = omegaCostPerMonth > 0
                ? (monthlyRevenue - monthlyExtractorCost) : 0;

            if (_vm.PricesLoaded && farmAccounts > 0)
            {
                SummaryPanel.Children.Add(BuildStatCard(
                    $"{monthlyExtractions:N0}/mo",
                    "Monthly Injectors",
                    allPaused
                        ? $"estimated at 2,700 SP/hr each"
                        : $"{totalSpPerHour:N0} SP/hr total",
                    FindBrush("EveAccentPrimaryBrush")));

                SummaryPanel.Children.Add(BuildStatCard(
                    FormatIsk(omegaCostPerMonth),
                    "Monthly Omega",
                    $"{farmAccounts} × 500 PLEX",
                    FindBrush("EveErrorRedBrush")));

                SummaryPanel.Children.Add(BuildStatCard(
                    FormatIsk(monthlyNet),
                    monthlyNet >= 0 ? "Monthly Profit" : "Monthly Cost",
                    monthlyNet < 0
                        ? $"saves {FormatIsk(omegaSavings)} vs full price"
                        : "after Omega + extractors",
                    monthlyNet >= 0
                        ? FindBrush("EveSuccessGreenBrush")
                        : FindBrush("EveWarningYellowBrush")));
            }
        }

        private Border BuildStatCard(string value, string label, string detail, IBrush? color)
        {
            return new Border
            {
                Background = FindBrush("EveBackgroundMediumBrush"),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 8),
                MinWidth = 110,
                Margin = new Thickness(0, 0, 6, 6),
                Child = new StackPanel
                {
                    Spacing = 1,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = value,
                            FontSize = FontScaleService.Subheading,
                            FontWeight = FontWeight.Bold,
                            Foreground = color ?? Brushes.White
                        },
                        new TextBlock
                        {
                            Text = label,
                            FontSize = FontScaleService.Caption,
                            Foreground = FindBrush("EveTextSecondaryBrush")
                        },
                        new TextBlock
                        {
                            Text = detail,
                            FontSize = FontScaleService.Caption,
                            Foreground = FindBrush("EveTextDisabledBrush")
                        }
                    }
                }
            };
        }

        #endregion

        #region Alerts — Grouped by type with portrait badges

        private void BuildAlerts()
        {
            AlertsList.Children.Clear();

            if (_vm.Alerts.Count == 0)
            {
                AlertsPanel.IsVisible = false;
                return;
            }

            AlertsPanel.IsVisible = true;

            // Group alerts by type
            var groups = _vm.Alerts.GroupBy(a => a.Type).ToList();

            foreach (var group in groups)
            {
                var alertType = group.Key;
                var characters = group.ToList();

                IBrush badgeColor;
                string icon;
                string summary;

                switch (alertType)
                {
                    case FarmAlertType.ReadyButPaused:
                        badgeColor = FindBrush("EveErrorRedBrush") ?? Brushes.Red;
                        icon = "\u26A0";
                        summary = $"Ready but paused ({characters.Count}) — extract before SP is wasted";
                        break;
                    case FarmAlertType.NoImplants:
                        badgeColor = FindBrush("EveWarningYellowBrush") ?? Brushes.Yellow;
                        icon = "\u26A1";
                        summary = $"No implants ({characters.Count}) — training slower than optimal";
                        break;
                    case FarmAlertType.SuboptimalImplants:
                        badgeColor = FindBrush("EveWarningYellowBrush") ?? Brushes.Yellow;
                        icon = "\u26A1";
                        summary = $"Suboptimal implants ({characters.Count}) — upgrade to +5";
                        break;
                    default:
                        badgeColor = FindBrush("EveTextDisabledBrush") ?? Brushes.Gray;
                        icon = "\u2139";
                        summary = $"{alertType} ({characters.Count})";
                        break;
                }

                var section = new StackPanel { Spacing = 4, Margin = new Thickness(0, 2) };

                // Summary text
                section.Children.Add(new TextBlock
                {
                    Text = $"{icon} {summary}",
                    FontSize = FontScaleService.Small,
                    Foreground = badgeColor,
                    FontWeight = FontWeight.SemiBold
                });

                // Mini portrait strip with badges
                var portraitStrip = new WrapPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(16, 2, 0, 0)
                };

                foreach (var alert in characters)
                {
                    var entry = _vm.Entries.FirstOrDefault(e => e.Character.Name == alert.CharacterName);
                    if (entry == null) continue;

                    var portraitImage = new Image { Width = 24, Height = 24, Stretch = Stretch.UniformToFill };
                    portraitImage.Tag = entry.Character.CharacterID;

                    var portraitBorder = new Border
                    {
                        Width = 24, Height = 24,
                        CornerRadius = new CornerRadius(3),
                        ClipToBounds = true,
                        Background = FindBrush("EveBackgroundDarkestBrush"),
                        Child = portraitImage
                    };

                    // Badge overlay
                    var badge = new Border
                    {
                        Width = 10, Height = 10,
                        CornerRadius = new CornerRadius(5),
                        Background = badgeColor,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Top,
                        Margin = new Thickness(0, -2, -2, 0),
                        Child = new TextBlock
                        {
                            Text = "!",
                            FontSize = 7,
                            Foreground = Brushes.Black,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    };

                    var container = new Grid
                    {
                        Width = 24, Height = 24,
                        Margin = new Thickness(0, 0, 4, 4),
                        Children = { portraitBorder, badge }
                    };

                    // Tooltip with details
                    string tooltip = alertType == FarmAlertType.ReadyButPaused
                        ? $"{alert.CharacterName}: {entry.ExtractionsAvailable} injectors ready"
                        : $"{alert.CharacterName}: {alert.Message}";
                    ToolTip.SetTip(container, tooltip);

                    portraitStrip.Children.Add(container);
                    LoadPortraitAsync(portraitImage, entry.Character.CharacterID);
                }

                section.Children.Add(portraitStrip);
                AlertsList.Children.Add(section);
            }
        }

        #endregion

        #region Character Table — Redesigned

        private void BuildCharacterTable()
        {
            CharacterTable.Children.Clear();

            var entries = _vm.Entries.AsEnumerable();
            if (_readyOnly) entries = entries.Where(e => e.ExtractionsAvailable > 0);
            if (!string.IsNullOrEmpty(_filter))
                entries = entries.Where(e => e.Character.Name.Contains(_filter, StringComparison.OrdinalIgnoreCase));

            var list = entries.ToList();

            if (list.Count == 0 && _vm.TotalCharacters == 0)
            {
                EmptyState.IsVisible = true;
                TableScroller.IsVisible = false;
                return;
            }

            EmptyState.IsVisible = false;
            TableScroller.IsVisible = true;

            // Header
            CharacterTable.Children.Add(BuildHeaderRow());

            // Data rows
            foreach (var entry in list)
                CharacterTable.Children.Add(BuildRow(entry));
        }

        private Border BuildHeaderRow()
        {
            var grid = MakeGrid();
            AddToGrid(grid, "Character", 0, HorizontalAlignment.Left, true);
            AddToGrid(grid, "SP", 1, HorizontalAlignment.Right, true);
            AddToGrid(grid, "Injectors", 2, HorizontalAlignment.Right, true);
            AddToGrid(grid, "Extractor Cost", 3, HorizontalAlignment.Right, true);
            AddToGrid(grid, "Revenue", 4, HorizontalAlignment.Right, true);
            AddToGrid(grid, "Net Profit", 5, HorizontalAlignment.Right, true);
            AddToGrid(grid, "SP/hr", 6, HorizontalAlignment.Right, true);
            AddToGrid(grid, "Impl", 7, HorizontalAlignment.Center, true);
            AddToGrid(grid, "Tax", 8, HorizontalAlignment.Right, true);
            AddToGrid(grid, "Status", 9, HorizontalAlignment.Right, true);

            return new Border
            {
                Background = FindBrush("EveBackgroundMediumBrush"),
                Padding = new Thickness(6, 5),
                BorderBrush = FindBrush("EveBorderBrush"),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Child = grid
            };
        }

        private Border BuildRow(FarmCharacterEntry entry)
        {
            var grid = MakeGrid();

            // Character name + portrait + remove
            var namePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            var portraitImage = new Image { Width = 22, Height = 22, Stretch = Stretch.UniformToFill };
            portraitImage.Tag = entry.Character.CharacterID;
            namePanel.Children.Add(new Border
            {
                Width = 22, Height = 22, CornerRadius = new CornerRadius(3),
                ClipToBounds = true, Background = FindBrush("EveBackgroundDarkestBrush"),
                Child = portraitImage
            });
            namePanel.Children.Add(new TextBlock
            {
                Text = entry.Character.Name,
                FontSize = FontScaleService.Body,
                Foreground = FindBrush("EveAccentPrimaryBrush"),
                VerticalAlignment = VerticalAlignment.Center
            });

            var capturedEntry = entry;
            var removeBtn = new Button
            {
                Content = "\u2715", FontSize = FontScaleService.Tiny,
                Padding = new Thickness(2, 0), Background = Brushes.Transparent,
                BorderThickness = new Thickness(0), Foreground = FindBrush("EveTextDisabledBrush"),
                Cursor = new Cursor(StandardCursorType.Hand), MinWidth = 0, MinHeight = 0,
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 0, 0)
            };
            removeBtn.Click += (_, _) => { _vm.RemoveFarmCharacter(capturedEntry.Character); _vm.Refresh(); RebuildUI(); };
            namePanel.Children.Add(removeBtn);

            Grid.SetColumn(namePanel, 0);
            grid.Children.Add(namePanel);

            // SP
            AddCell(grid, $"{entry.CurrentSP:N0}", 1, FindBrush("EveTextPrimaryBrush"));

            // Injectors available
            string injText = entry.ExtractionsAvailable > 0 ? $"{entry.ExtractionsAvailable}×" : "—";
            AddCell(grid, injText, 2, entry.ExtractionsAvailable > 0
                ? FindBrush("EveSuccessGreenBrush") : FindBrush("EveTextDisabledBrush"));

            // Extractor cost (how much you spend)
            double extractorCost = entry.ExtractionsAvailable * entry.ExtractorCost;
            AddCell(grid, extractorCost > 0 ? FormatIsk(extractorCost) : "—", 3,
                FindBrush("EveErrorRedBrush"));

            // Revenue (what you get from selling injectors)
            double grossRev = entry.ExtractionsAvailable * entry.GrossRevenuePerExtraction;
            AddCell(grid, grossRev > 0 ? FormatIsk(grossRev) : "—", 4,
                FindBrush("EveSuccessGreenBrush"));

            // Net profit
            double netProfit = entry.ExtractionsAvailable * entry.NetProfitPerExtraction;
            AddCell(grid, netProfit != 0 ? FormatIsk(netProfit) : "—", 5,
                netProfit > 0 ? FindBrush("EveSuccessGreenBrush") : FindBrush("EveErrorRedBrush"));

            // SP/hr
            AddCell(grid, entry.SpPerHour > 0 ? $"{entry.SpPerHour:N0}" : "0", 6,
                entry.SpPerHour > 0 ? FindBrush("EveTextSecondaryBrush") : FindBrush("EveErrorRedBrush"));

            // Implants
            AddCell(grid, entry.ImplantText, 7,
                entry.ImplantLevel >= 5 ? FindBrush("EveSuccessGreenBrush")
                : entry.ImplantLevel > 0 ? FindBrush("EveWarningYellowBrush")
                : FindBrush("EveTextDisabledBrush"), HorizontalAlignment.Center);

            // Tax
            AddCell(grid, $"{entry.SalesTaxPercent:F1}%", 8, FindBrush("EveTextDisabledBrush"));

            // Status
            IBrush statusColor;
            if (entry.ExtractionsAvailable > 0 && entry.IsTraining)
                statusColor = FindBrush("EveSuccessGreenBrush") ?? Brushes.Green;
            else if (entry.ExtractionsAvailable > 0)
                statusColor = FindBrush("EveErrorRedBrush") ?? Brushes.Red;
            else if (entry.IsTraining)
                statusColor = FindBrush("EveWarningYellowBrush") ?? Brushes.Yellow;
            else
                statusColor = FindBrush("EveErrorRedBrush") ?? Brushes.Red;

            AddCell(grid, entry.StatusText, 9, statusColor);

            LoadPortraitAsync(portraitImage, entry.Character.CharacterID);

            return new Border
            {
                Background = entry.ExtractionsAvailable > 0 ? GetAccentTint(12) : Brushes.Transparent,
                Padding = new Thickness(6, 4),
                BorderBrush = FindBrush("EveBorderBrush"),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Child = grid
            };
        }

        // Grid with 10 columns: name(*), SP, injectors, cost, revenue, net, sp/hr, impl, tax, status
        private static Grid MakeGrid()
        {
            return new Grid
            {
                ColumnDefinitions = ColumnDefinitions.Parse("*,Auto,Auto,Auto,Auto,Auto,Auto,Auto,Auto,Auto"),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
        }

        private void AddToGrid(Grid grid, string text, int col, HorizontalAlignment align, bool header)
        {
            grid.Children.Add(new TextBlock
            {
                Text = text,
                FontSize = header ? FontScaleService.Small : FontScaleService.Body,
                FontWeight = header ? FontWeight.SemiBold : FontWeight.Normal,
                Foreground = FindBrush("EveTextSecondaryBrush"),
                HorizontalAlignment = align,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0),
                MinWidth = col == 0 ? 0 : 70,
                [Grid.ColumnProperty] = col
            });
        }

        private void AddCell(Grid grid, string text, int col, IBrush? foreground,
            HorizontalAlignment align = HorizontalAlignment.Right)
        {
            grid.Children.Add(new TextBlock
            {
                Text = text,
                FontSize = FontScaleService.Small,
                Foreground = foreground ?? FindBrush("EveTextPrimaryBrush"),
                HorizontalAlignment = align,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0),
                MinWidth = 70,
                [Grid.ColumnProperty] = col
            });
        }

        #endregion

        #region Event Handlers

        private void OnAddCharacterClick(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;

            var allChars = AppServices.Characters.Where(c => c.Monitored).ToList();
            var farmGuids = Settings.UI.SkillFarm.FarmCharacters
                .Select(f => f.CharacterGuid).ToHashSet();

            var flyout = new Flyout { Placement = PlacementMode.BottomEdgeAlignedLeft };
            var panel = new StackPanel { Spacing = 1, MinWidth = 220 };

            panel.Children.Add(new TextBlock
            {
                Text = "Designate as skill farm:",
                FontSize = FontScaleService.Small,
                Foreground = FindBrush("EveTextDisabledBrush"),
                Margin = new Thickness(8, 4, 0, 4)
            });

            var available = allChars.Where(c => !farmGuids.Contains(c.Guid)).ToList();

            if (available.Count == 0)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = "All characters already in farm",
                    FontSize = FontScaleService.Small,
                    Foreground = FindBrush("EveTextDisabledBrush"),
                    Margin = new Thickness(8, 4)
                });
            }
            else
            {
                // Sort: highest SP first (most valuable to add)
                foreach (var character in available.OrderByDescending(c => c.SkillPoints))
                {
                    var capturedChar = character;
                    bool extractable = character.SkillPoints >= 5_000_000;
                    string spText = character.SkillPoints >= 1_000_000
                        ? $"{character.SkillPoints / 1_000_000.0:N1}M SP"
                        : $"{character.SkillPoints / 1_000.0:N0}K SP";

                    var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };

                    var portraitImg = new Image { Width = 20, Height = 20, Stretch = Stretch.UniformToFill };
                    portraitImg.Tag = character.CharacterID;
                    row.Children.Add(new Border
                    {
                        Width = 20, Height = 20, CornerRadius = new CornerRadius(3),
                        ClipToBounds = true, Background = FindBrush("EveBackgroundDarkestBrush"),
                        Child = portraitImg, VerticalAlignment = VerticalAlignment.Center
                    });

                    row.Children.Add(new TextBlock
                    {
                        Text = character.Name,
                        FontSize = FontScaleService.Body,
                        Foreground = FindBrush("EveAccentPrimaryBrush"),
                        VerticalAlignment = VerticalAlignment.Center
                    });

                    row.Children.Add(new TextBlock
                    {
                        Text = spText,
                        FontSize = FontScaleService.Small,
                        Foreground = extractable
                            ? FindBrush("EveSuccessGreenBrush")
                            : FindBrush("EveTextDisabledBrush"),
                        VerticalAlignment = VerticalAlignment.Center
                    });

                    var charBtn = new Button
                    {
                        Content = row,
                        Background = Brushes.Transparent,
                        BorderThickness = new Thickness(0),
                        Padding = new Thickness(8, 4),
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        HorizontalContentAlignment = HorizontalAlignment.Left,
                        Cursor = new Cursor(StandardCursorType.Hand)
                    };
                    charBtn.Click += (_, _) =>
                    {
                        flyout.Hide();
                        _vm.AddFarmCharacter(capturedChar);
                        _vm.Refresh();
                        RebuildUI();
                    };
                    panel.Children.Add(charBtn);
                    LoadPortraitAsync(portraitImg, character.CharacterID);
                }
            }

            flyout.Content = panel;
            flyout.ShowAt(btn);
        }

        private void OnRefreshClick(object? sender, RoutedEventArgs e)
        {
            FetchPricesAndRebuild();
        }

        private void OnFilterChanged(object? sender, TextChangedEventArgs e)
        {
            _filter = FilterBox.Text?.Trim() ?? "";
            BuildCharacterTable();
            UpdateStatus();
        }

        private void OnReadyOnlyToggled(object? sender, RoutedEventArgs e)
        {
            _readyOnly = ReadyOnlyToggle.IsChecked == true;
            BuildCharacterTable();
            UpdateStatus();
        }

        #endregion

        #region Helpers

        private void UpdateStatus()
        {
            if (_vm.TotalCharacters == 0)
            {
                StatusText.Text = "Add characters to begin";
                return;
            }

            var parts = new[]
            {
                $"Farm: {_vm.TotalCharacters} characters",
                $"Ready: {_vm.ReadyCount}",
                $"Total injectors: {_vm.TotalExtractionsToday:N0}",
                _vm.PricesLoaded ? $"Net: {FormatIsk(_vm.TotalRevenueToday)}" : "Prices loading..."
            };
            StatusText.Text = string.Join("  |  ", parts);
        }

        private static string FormatIsk(double amount)
        {
            if (Math.Abs(amount) >= 1_000_000_000) return $"{amount / 1_000_000_000:N2}B";
            if (Math.Abs(amount) >= 1_000_000) return $"{amount / 1_000_000:N1}M";
            if (Math.Abs(amount) >= 1_000) return $"{amount / 1_000:N0}K";
            return $"{amount:N0}";
        }

        private IBrush? FindBrush(string key)
        {
            if (this.TryFindResource(key, this.ActualThemeVariant, out var res) && res is IBrush b) return b;
            return null;
        }

        private IBrush GetAccentTint(byte alpha)
        {
            if (this.TryFindResource("EveAccentPrimary", this.ActualThemeVariant, out var res) && res is Color c)
                return new SolidColorBrush(new Color(alpha, c.R, c.G, c.B));
            return new SolidColorBrush(Color.Parse("#10FFFFFF"));
        }

        private async void LoadPortraitAsync(Image image, long characterId)
        {
            try
            {
                var drawingImage = await ImageService.GetCharacterImageAsync(characterId);
                if (drawingImage != null)
                {
                    var converted = DrawingImageToAvaloniaConverter.Instance.Convert(
                        drawingImage, typeof(Bitmap), null!, CultureInfo.InvariantCulture);
                    if (converted is Bitmap bitmap) image.Source = bitmap;
                }
            }
            catch { }
        }

        protected override void OnClosed(EventArgs e)
        {
            _vm.Dispose();
            base.OnClosed(e);
        }

        #endregion
    }
}
