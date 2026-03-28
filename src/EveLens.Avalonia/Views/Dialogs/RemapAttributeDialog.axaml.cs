// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using EveLens.Common.Enumerations;
using EveLens.Common.Models;

using EveLens.Common.Models;
using EveLens.Avalonia.Services;
namespace EveLens.Avalonia.Views.Dialogs
{
    public partial class RemapAttributeDialog : Window
    {
        private RemappingPoint? _remapPoint;
        private EveAttribute _dominantPrimary;
        private EveAttribute _dominantSecondary;
        private readonly Dictionary<EveAttribute, int> _values = new();
        private readonly Dictionary<EveAttribute, TextBlock> _valueTbs = new();
        private readonly Dictionary<EveAttribute, Border> _bars = new();
        private readonly Dictionary<EveAttribute, Button> _decBtns = new();
        private readonly Dictionary<EveAttribute, Button> _incBtns = new();

        private static readonly EveAttribute[] AllAttrs =
        {
            EveAttribute.Intelligence, EveAttribute.Perception,
            EveAttribute.Charisma, EveAttribute.Willpower, EveAttribute.Memory
        };

        private static readonly IBrush GoldBrush = new SolidColorBrush(Color.Parse("#FFE6A817"));

        public bool WasApplied { get; private set; }

        public RemapAttributeDialog()
        {
            InitializeComponent();
        }

        public void Initialize(RemappingPoint rp, EveAttribute dominantPrimary, EveAttribute dominantSecondary)
        {
            _remapPoint = rp;
            _dominantPrimary = dominantPrimary;
            _dominantSecondary = dominantSecondary;

            string priName = GetShortName(dominantPrimary);
            string secName = GetShortName(dominantSecondary);

            SubtitleText.Text = $"Distribute 14 spare points across attributes. Skills below favor {priName}/{secName}.";
            MatchBtn.Content = $"Match {priName}/{secName}";

            // Initialize values from remap point (or base 17 each)
            foreach (var attr in AllAttrs)
            {
                int val = (int)rp[attr];
                if (val < 17 || val > 27) val = 17;
                _values[attr] = val;
            }

            // If all are 17 (fresh remap), start with match as hint
            int totalSpare = 0;
            foreach (var v in _values.Values) totalSpare += v - 17;
            if (totalSpare == 0)
            {
                // Default to match allocation
                _values[dominantPrimary] = 27;
                if (dominantSecondary != dominantPrimary)
                    _values[dominantSecondary] = 21;
            }

            BuildAttrRows();
            UpdateUI();
        }

        private void BuildAttrRows()
        {
            AttrPanel.Children.Clear();

            foreach (var attr in AllAttrs)
            {
                var row = new Grid
                {
                    ColumnDefinitions = ColumnDefinitions.Parse("Auto,50,*,Auto"),
                };

                // [-] button
                var decBtn = new Button
                {
                    Content = "\u2212",
                    FontSize = FontScaleService.Subheading,
                    Width = 28, Height = 28,
                    Padding = new Thickness(0),
                    CornerRadius = new CornerRadius(14),
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                };
                var capturedAttr = attr;
                decBtn.Click += (_, _) => AdjustAttr(capturedAttr, -1);
                Grid.SetColumn(decBtn, 0);
                row.Children.Add(decBtn);
                _decBtns[attr] = decBtn;

                // Attribute name + value
                var valueTb = new TextBlock
                {
                    FontSize = FontScaleService.Heading,
                    FontWeight = FontWeight.Bold,
                    Foreground = GetBrush(attr),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                Grid.SetColumn(valueTb, 1);
                row.Children.Add(valueTb);
                _valueTbs[attr] = valueTb;

                // Bar
                double barMaxW = 160;
                var barContainer = new Panel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(8, 0),
                };
                barContainer.Children.Add(new Border
                {
                    Width = barMaxW,
                    Height = 10,
                    CornerRadius = new CornerRadius(5),
                    Background = new SolidColorBrush(Color.Parse("#FF252535")),
                    HorizontalAlignment = HorizontalAlignment.Left,
                });
                var barFill = new Border
                {
                    Height = 10,
                    CornerRadius = new CornerRadius(5),
                    Background = GetBrush(attr),
                    HorizontalAlignment = HorizontalAlignment.Left,
                };
                barContainer.Children.Add(barFill);
                Grid.SetColumn(barContainer, 2);
                row.Children.Add(barContainer);
                _bars[attr] = barFill;

                // [+] button
                var incBtn = new Button
                {
                    Content = "+",
                    FontSize = FontScaleService.Subheading,
                    Width = 28, Height = 28,
                    Padding = new Thickness(0),
                    CornerRadius = new CornerRadius(14),
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                };
                incBtn.Click += (_, _) => AdjustAttr(capturedAttr, +1);
                Grid.SetColumn(incBtn, 3);
                row.Children.Add(incBtn);
                _incBtns[attr] = incBtn;

                AttrPanel.Children.Add(row);
            }
        }

        private void AdjustAttr(EveAttribute attr, int delta)
        {
            int newVal = _values[attr] + delta;
            if (newVal < 17 || newVal > 27) return;

            int currentSpare = 0;
            foreach (var v in _values.Values) currentSpare += v - 17;
            int newSpare = currentSpare + delta;
            if (newSpare < 0 || newSpare > 14) return;

            _values[attr] = newVal;
            UpdateUI();
        }

        private void UpdateUI()
        {
            int totalSpare = 0;
            foreach (var v in _values.Values) totalSpare += v - 17;
            int remaining = 14 - totalSpare;

            double barMaxW = 160;

            foreach (var attr in AllAttrs)
            {
                int val = _values[attr];
                int remappable = val - 17;

                _valueTbs[attr].Text = $"{GetShortName(attr)} {val}";
                _bars[attr].Width = Math.Max(0, (remappable / 10.0) * barMaxW);
                _decBtns[attr].IsEnabled = remappable > 0;
                _incBtns[attr].IsEnabled = remaining > 0 && remappable < 10;
            }

            if (remaining > 0)
            {
                RemainingText.Text = $"\u26A0 {remaining} point{(remaining != 1 ? "s" : "")} left to assign";
                RemainingText.Foreground = new SolidColorBrush(Color.Parse("#FFFFD54F"));
            }
            else
            {
                RemainingText.Text = "\u2713 All 14 points assigned";
                RemainingText.Foreground = new SolidColorBrush(Color.Parse("#FF81C784"));
            }

            ApplyBtn.IsEnabled = remaining == 0;
        }

        private void OnMatchClick(object? sender, RoutedEventArgs e)
        {
            foreach (var attr in AllAttrs)
                _values[attr] = 17;

            _values[_dominantPrimary] = 27;
            if (_dominantSecondary != _dominantPrimary)
                _values[_dominantSecondary] = 21;

            UpdateUI();
        }

        private void OnApplyClick(object? sender, RoutedEventArgs e)
        {
            if (_remapPoint == null) return;

            _remapPoint.SetAttributes(
                _values[EveAttribute.Intelligence],
                _values[EveAttribute.Perception],
                _values[EveAttribute.Charisma],
                _values[EveAttribute.Willpower],
                _values[EveAttribute.Memory]);

            WasApplied = true;
            Close();
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            WasApplied = false;
            Close();
        }

        private static string GetShortName(EveAttribute attr) => attr switch
        {
            EveAttribute.Intelligence => "INT",
            EveAttribute.Perception => "PER",
            EveAttribute.Charisma => "CHA",
            EveAttribute.Willpower => "WIL",
            EveAttribute.Memory => "MEM",
            _ => attr.ToString()
        };

        private static IBrush GetBrush(EveAttribute attr) => attr switch
        {
            EveAttribute.Intelligence => new SolidColorBrush(Color.Parse("#FF4FC3F7")),
            EveAttribute.Perception => new SolidColorBrush(Color.Parse("#FFEF5350")),
            EveAttribute.Charisma => new SolidColorBrush(Color.Parse("#FF66BB6A")),
            EveAttribute.Willpower => new SolidColorBrush(Color.Parse("#FFAB47BC")),
            EveAttribute.Memory => new SolidColorBrush(Color.Parse("#FFFFA726")),
            _ => Brushes.Gray
        };
    }
}
