// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using EveLens.Avalonia.Services;
using EveLens.Common.Enumerations;
using EveLens.Common.Models;
using EveLens.Common.Services;

namespace EveLens.Avalonia.Views.Dialogs
{
    /// <summary>
    /// Attribute optimization as a dedicated window (Issue #71). Read-only analysis —
    /// current vs. optimal attributes, time saved, remap placements — with a single atomic
    /// "Apply to Plan". Replaces the fragile inline remap editing that mutated the live
    /// plan table in multiple steps.
    /// </summary>
    public partial class OptimizeAttributesWindow : Window
    {
        private static readonly IBrush GoldBrush = new SolidColorBrush(Color.Parse("#FFE6A817"));
        private static readonly IBrush GreenBrush = new SolidColorBrush(Color.Parse("#FF81C784"));
        private static readonly IBrush DimBrush = new SolidColorBrush(Color.Parse("#FF909090"));
        private static readonly IBrush PanelBg = new SolidColorBrush(Color.Parse("#FF1E1E32"));

        private BasePlan? _analysisPlan;
        private BasePlan? _targetPlan;
        private Character? _character;
        private RemapPlanningService.RemapProposal? _proposal;
        private int _analysisVersion;

        /// <summary>Raised after a proposal has been applied so the editor can refresh once.</summary>
        public event Action? Applied;

        public OptimizeAttributesWindow()
        {
            InitializeComponent();
            CancelButton.Click += (_, _) => Close();
            ApplyButton.Click += (_, _) => ApplyProposal();
            // Pass the strategy EXPLICITLY per handler. Reading StrategyAutoPlace.IsChecked
            // inside a shared handler was event-order dependent: when clicking "whole plan",
            // its checked event can fire while the other radio is still checked, so the
            // analysis ran with the OLD strategy and rendered the wrong result ("the whole-
            // plan card never comes back" bug).
            StrategyWholePlan.IsCheckedChanged += (_, _) =>
            { if (StrategyWholePlan.IsChecked == true) _ = RunAnalysisAsync(autoPlace: false); };
            StrategyAutoPlace.IsCheckedChanged += (_, _) =>
            { if (StrategyAutoPlace.IsChecked == true) _ = RunAnalysisAsync(autoPlace: true); };
            // Clone what-if: rerun with the CURRENT strategy when the clone state changes
            CloneAuto.IsCheckedChanged += (_, _) =>
            { if (CloneAuto.IsChecked == true) _ = RunAnalysisAsync(StrategyAutoPlace.IsChecked == true); };
            CloneOmega.IsCheckedChanged += (_, _) =>
            { if (CloneOmega.IsChecked == true) _ = RunAnalysisAsync(StrategyAutoPlace.IsChecked == true); };
            CloneAlpha.IsCheckedChanged += (_, _) =>
            { if (CloneAlpha.IsChecked == true) _ = RunAnalysisAsync(StrategyAutoPlace.IsChecked == true); };
        }

        private AccountStatusMode? SelectedCloneOverride =>
            CloneOmega.IsChecked == true ? AccountStatusMode.Omega
            : CloneAlpha.IsChecked == true ? AccountStatusMode.Alpha
            : null;

        /// <summary>
        /// Initializes the window. <paramref name="analysisPlan"/> is the plan IN THE ORDER
        /// THE PANEL SHOWS (the sorted display plan) — training order changes total time, so
        /// analyzing the raw plan while the panel shows the sorted one produced two different
        /// "current" numbers for the same plan. <paramref name="targetPlan"/> is the real
        /// plan that Apply writes remap points to.
        /// </summary>
        public void Initialize(BasePlan analysisPlan, BasePlan targetPlan, Character character)
        {
            _analysisPlan = analysisPlan;
            _targetPlan = targetPlan;
            _character = character;

            int available = character.AvailableReMaps;
            bool timedAvailable = character.LastReMapTimed == DateTime.MinValue
                || DateTime.UtcNow >= character.LastReMapTimed.AddDays(365);
            int budget = available + (timedAvailable ? 1 : 0);
            RemapBudgetText.Text = string.Format(Loc.Get("Optimizer.RemapBudget"), budget);

            _ = RunAnalysisAsync(autoPlace: StrategyAutoPlace.IsChecked == true);
        }

        private async Task RunAnalysisAsync(bool autoPlace)
        {
            if (_analysisPlan == null || _character == null) return;

            // Version stamp: rapid strategy toggles fire overlapping analyses, and the
            // SLOWER (stale) one used to land last and render over the fresh result —
            // which read as "results stop coming" when switching strategies.
            int version = ++_analysisVersion;

            ApplyButton.IsEnabled = false;
            ResultsPanel.Children.Clear();
            ResultsPanel.Children.Add(new TextBlock
            {
                Text = "⚡ " + Loc.Get("Plan.Analyzing"),
                FontSize = FontScaleService.Body,
                Foreground = GoldBrush,
            });

            int maxRemaps = MaxRemapBudget();

            try
            {
                // The optimizer brute-forces 14k attribute combinations per segment —
                // off the UI thread. The plan object is not mutated by analysis.
                var cloneOverride = SelectedCloneOverride;
                var proposal = await Task.Run(() =>
                    RemapPlanningService.ProposeAtAttributeBoundaries(
                        _analysisPlan, autoPlace ? Math.Max(maxRemaps, 1) : 1,
                        cloneOverride: cloneOverride));

                if (version != _analysisVersion)
                    return; // superseded by a newer strategy selection

                _proposal = proposal;
                RenderProposal(proposal);
            }
            catch (Exception ex)
            {
                if (version != _analysisVersion)
                    return;
                ResultsPanel.Children.Clear();
                ResultsPanel.Children.Add(new TextBlock
                {
                    Text = string.Format(Loc.Get("Optimizer.Failed"), ex.Message),
                    FontSize = FontScaleService.Body,
                    Foreground = new SolidColorBrush(Color.Parse("#FFE57373")),
                    TextWrapping = TextWrapping.Wrap,
                });
            }
        }

        private int MaxRemapBudget()
        {
            if (_character == null) return 1;
            bool timedAvailable = _character.LastReMapTimed == DateTime.MinValue
                || DateTime.UtcNow >= _character.LastReMapTimed.AddDays(365);
            return Math.Max(1, _character.AvailableReMaps + (timedAvailable ? 1 : 0));
        }

        private void RenderProposal(RemapPlanningService.RemapProposal proposal)
        {
            ResultsPanel.Children.Clear();

            // Headline: current → optimized, time saved. Label the "current" so the user
            // knows it already includes their applied remap points (the "where does 398d
            // come from?" confusion).
            string currentLabel = proposal.CurrentIncludesRemaps
                ? string.Format(Loc.Get("Optimizer.CurrentWithRemaps"), FormatTime(proposal.CurrentDuration))
                : FormatTime(proposal.CurrentDuration);
            var headline = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            headline.Children.Add(Text(currentLabel, DimBrush,
                FontScaleService.Heading));
            headline.Children.Add(Text("→", DimBrush, FontScaleService.Heading));
            headline.Children.Add(Text(FormatTime(proposal.OptimizedDuration), GreenBrush,
                FontScaleService.Heading, FontWeight.Bold));
            if (proposal.TimeSaved > TimeSpan.Zero)
                headline.Children.Add(Text(
                    string.Format(Loc.Get("Optimizer.Saves"), FormatTime(proposal.TimeSaved)),
                    GreenBrush, FontScaleService.Body, FontWeight.SemiBold));
            ResultsPanel.Children.Add(headline);

            bool isImprovement = proposal.TimeSaved > TimeSpan.Zero;
            if (!isImprovement)
            {
                // Current setup (including already-applied remap points) matches or beats
                // this strategy. Cards below stay visible as reference, but Apply is
                // blocked — one click must never make a plan slower.
                ResultsPanel.Children.Add(Text(Loc.Get("Optimizer.AlreadyOptimal"),
                    DimBrush, FontScaleService.Body));
            }

            // One card per segment/remap
            for (int i = 0; i < proposal.Remaps.Count; i++)
            {
                var remap = proposal.Remaps[i];
                var card = new Border
                {
                    Background = PanelBg,
                    CornerRadius = new global::Avalonia.CornerRadius(6),
                    Padding = new global::Avalonia.Thickness(14, 10),
                };
                var stack = new StackPanel { Spacing = 4 };

                string title = i == 0
                    ? Loc.Get("Optimizer.SegmentStart")
                    : string.Format(Loc.Get("Optimizer.SegmentBefore"),
                        $"{remap.Skill.LocalizedName} {Common.Models.Skill.GetRomanFromInt(remap.Level)}");
                stack.Children.Add(Text(title, GoldBrush, FontScaleService.Body, FontWeight.SemiBold));
                stack.Children.Add(Text(
                    $"{remap.SegmentLabel} · {FormatTime(remap.SegmentDuration)}",
                    DimBrush, FontScaleService.Small));

                // Attribute spread line: PER 27  WIL 21  INT 17  MEM 17  CHA 17
                var attrLine = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
                foreach (var attr in new[] { EveAttribute.Perception, EveAttribute.Willpower,
                    EveAttribute.Intelligence, EveAttribute.Memory, EveAttribute.Charisma })
                {
                    attrLine.Children.Add(Text(
                        $"{AttrShort(attr)} {remap.Attributes[attr]}",
                        Brushes.White, FontScaleService.Body));
                }
                stack.Children.Add(attrLine);

                card.Child = stack;
                ResultsPanel.Children.Add(card);
            }

            // Apply only when it actually improves the plan; applying a no-gain proposal
            // could clear better hand-placed remap points (Apply replaces all of them).
            ApplyButton.IsEnabled = isImprovement && proposal.Remaps.Count > 0;
        }

        private void ApplyProposal()
        {
            if (_targetPlan == null || _proposal == null) return;

            // Single atomic mutation — clears old remap points, writes the proposal.
            // Applied to the REAL plan; remap points carry to the display plan on refresh
            // (matched by skill+level).
            RemapPlanningService.Apply(_targetPlan, _proposal);
            Applied?.Invoke();
            Close();
        }


        private static TextBlock Text(string text, IBrush brush, double size,
            FontWeight weight = FontWeight.Normal) => new()
        {
            Text = text,
            Foreground = brush,
            FontSize = size,
            FontWeight = weight,
            VerticalAlignment = VerticalAlignment.Center,
        };

        private static string AttrShort(EveAttribute attr) => attr switch
        {
            EveAttribute.Perception => "PER",
            EveAttribute.Willpower => "WIL",
            EveAttribute.Intelligence => "INT",
            EveAttribute.Memory => "MEM",
            EveAttribute.Charisma => "CHA",
            _ => "?",
        };

        private static string FormatTime(TimeSpan t) =>
            t.TotalDays >= 1 ? $"{(int)t.TotalDays}d {t.Hours}h" : $"{(int)t.TotalHours}h {t.Minutes}m";
    }
}
