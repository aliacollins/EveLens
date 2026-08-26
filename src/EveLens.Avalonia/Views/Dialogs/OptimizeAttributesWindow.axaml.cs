// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
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

        private bool _suppressToggles;

        public OptimizeAttributesWindow()
        {
            InitializeComponent();
            CancelButton.Click += (_, _) => Close();
            ApplyButton.Click += (_, _) => ApplyProposal();
            // Segmented pill groups: exclusive, never empty, restyled on change, and
            // the analysis reruns once per USER change (the suppress flag keeps the
            // programmatic uncheck/recheck from cascading — the successor of the old
            // "whole-plan card never comes back" event-order bug).
            WireToggleGroup(new[] { StrategyAutoPlace, StrategyWholePlan });
            WireToggleGroup(new[] { CloneAuto, CloneOmega, CloneAlpha });
        }

        private void WireToggleGroup(ToggleButton[] group)
        {
            foreach (var button in group)
            {
                var self = button;
                self.IsCheckedChanged += (_, _) =>
                {
                    if (_suppressToggles) return;
                    _suppressToggles = true;
                    if (self.IsChecked == true)
                    {
                        foreach (var other in group)
                            if (other != self) other.IsChecked = false;
                    }
                    else if (group.All(b => b.IsChecked != true))
                    {
                        self.IsChecked = true;   // a segmented control is never empty
                        _suppressToggles = false;
                        StyleToggleGroup(group);
                        return;
                    }
                    _suppressToggles = false;
                    StyleToggleGroup(group);
                    _ = RunAnalysisAsync(StrategyAutoPlace.IsChecked == true);
                };
            }
            StyleToggleGroup(group);
        }

        private static void StyleToggleGroup(ToggleButton[] group)
        {
            foreach (var button in group)
            {
                bool on = button.IsChecked == true;
                button.Background = on
                    ? new SolidColorBrush(Color.Parse("#FF2C2C4E"))
                    : Brushes.Transparent;
                button.Foreground = on ? GoldBrush : DimBrush;
                button.FontWeight = on ? FontWeight.SemiBold : FontWeight.Normal;
            }
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
            RemapsChipText.Text = string.Format(Loc.Get("Optimizer.RemapsAvailFmt"),
                budget, timedAvailable ? 1 : 0, available);

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
                Text = Loc.Get("Plan.Analyzing"),
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
            HeadlineRow.Children.Clear();
            DetailsPanel.Children.Clear();

            // Headline bar: Current X → Optimized Y · Save Z · P% faster · Finish date.
            string currentLabel = proposal.CurrentIncludesRemaps
                ? string.Format(Loc.Get("Optimizer.CurrentWithRemaps"), FormatTime(proposal.CurrentDuration))
                : FormatTime(proposal.CurrentDuration);
            HeadlineRow.Children.Add(Text(Loc.Get("Optimizer.Current"), DimBrush,
                FontScaleService.Body));
            HeadlineRow.Children.Add(Text(currentLabel, DimBrush,
                FontScaleService.Heading, FontWeight.SemiBold));
            HeadlineRow.Children.Add(Text("→", DimBrush, FontScaleService.Heading));
            HeadlineRow.Children.Add(Text(Loc.Get("Optimizer.Optimized"), DimBrush,
                FontScaleService.Body));
            HeadlineRow.Children.Add(Text(FormatTime(proposal.OptimizedDuration), GreenBrush,
                FontScaleService.Heading, FontWeight.Bold));
            bool isImprovement = proposal.TimeSaved > TimeSpan.Zero;
            if (isImprovement)
            {
                double pct = proposal.CurrentDuration > TimeSpan.Zero
                    ? 100.0 * proposal.TimeSaved.Ticks / proposal.CurrentDuration.Ticks : 0;
                HeadlineRow.Children.Add(Text(
                    string.Format(Loc.Get("Optimizer.SaveFasterFmt"),
                        FormatTime(proposal.TimeSaved), pct),
                    GreenBrush, FontScaleService.Body, FontWeight.SemiBold));
                HeadlineRow.Children.Add(Text("|", DimBrush, FontScaleService.Body));
                HeadlineRow.Children.Add(Text(
                    string.Format(Loc.Get("Optimizer.FinishFmt"),
                        DateTime.Now + proposal.OptimizedDuration),
                    DimBrush, FontScaleService.Body));
            }

            if (!isImprovement)
            {
                // Current setup matches or beats the proposal; name the reason when
                // there is one (an active attribute booster) instead of looking broken.
                ResultsPanel.Children.Add(Text(Loc.Get(
                        proposal.CurrentLikelyBoosted
                            ? "Optimizer.BoosterActive"
                            : "Optimizer.AlreadyOptimal"),
                    DimBrush, FontScaleService.Body));
            }

            // ── Timeline: rail with nodes, keep-current card, marker + card per remap ──
            _cards.Clear();
            if (proposal.PrefixSkillCount > 0)
            {
                string range = proposal.PrefixSkillCount > 1
                    ? $"{proposal.PrefixFirstSkill} → {proposal.PrefixLastSkill} · "
                    : $"{proposal.PrefixFirstSkill} · ";
                var keepCard = SegmentCard(
                    "✓ " + Loc.Get("Optimizer.KeepCurrent"), GreenBrush,
                    range + string.Format(Loc.Get("Optimizer.SkillsDurFmt"),
                        proposal.PrefixSkillCount, FormatTime(proposal.PrefixDuration)),
                    CurrentAttributeValues(), proposal.PrefixPairLabel);
                _cards.Add(keepCard);
                int self = _cards.Count - 1;
                keepCard.PointerPressed += (_, _) =>
                { SelectCard(self); ShowKeepCurrentDetails(proposal); };
                ResultsPanel.Children.Add(TimelineRow(RailNode(TealBrush), keepCard));
            }

            for (int i = 0; i < proposal.Remaps.Count; i++)
            {
                var remap = proposal.Remaps[i];
                string skillLabel =
                    $"{remap.Skill.LocalizedName} {Common.Models.Skill.GetRomanFromInt(remap.Level)}";

                ResultsPanel.Children.Add(TimelineRow(RailDiamond(), Text(
                    string.Format(Loc.Get("Optimizer.RemapBeforeFmt"), skillLabel),
                    GoldBrush, FontScaleService.Small, FontWeight.SemiBold)));

                string range = remap.SkillNames.Count > 1
                    ? $"{remap.SkillNames[0]} → {remap.SkillNames[^1]} · "
                    : remap.SkillNames.Count == 1 ? $"{remap.SkillNames[0]} · " : "";
                var card = SegmentCard(remap.PairLabel, GoldBrush,
                    range + string.Format(Loc.Get("Optimizer.SkillsDurFmt"),
                        remap.SkillCount, FormatTime(remap.SegmentDuration)),
                    remap.Attributes, remap.PairLabel);
                _cards.Add(card);
                int self = _cards.Count - 1;
                int index = i;
                card.PointerPressed += (_, _) =>
                { SelectCard(self); ShowRemapDetails(proposal, index); };
                ResultsPanel.Children.Add(TimelineRow(RailNode(GoldBrush), card));
            }
            // Default selection mirrors the default inspector content below.
            SelectCard(proposal.Remaps.Count > 0 ? (proposal.PrefixSkillCount > 0 ? 1 : 0) : 0);

            // Proportional duration bar: how the optimized plan divides its time.
            if (proposal.Remaps.Count > 0)
                ResultsPanel.Children.Add(DurationBar(proposal));

            // Default inspector content: the first remap tells the story best.
            if (proposal.Remaps.Count > 0)
                ShowRemapDetails(proposal, 0);
            else
                ShowKeepCurrentDetails(proposal);

            // Apply only when it actually improves the plan; applying a no-gain proposal
            // could clear better hand-placed remap points (Apply replaces all of them).
            ApplyButton.IsEnabled = isImprovement && proposal.Remaps.Count > 0;
        }

        private static readonly IBrush TealBrush = new SolidColorBrush(Color.Parse("#FF2BB5AD"));
        private static readonly IBrush RailBrush = new SolidColorBrush(Color.Parse("#FF33334E"));
        private readonly System.Collections.Generic.List<Border> _cards = new();

        /// <summary>The selected card wears a gold border; the timeline is a
        /// master-detail and the master must show which row the inspector explains.</summary>
        private void SelectCard(int index)
        {
            for (int i = 0; i < _cards.Count; i++)
                _cards[i].BorderBrush = i == index ? GoldBrush : Brushes.Transparent;
        }

        /// <summary>One timeline row: the rail (vertical line + node) beside content.</summary>
        private static Control TimelineRow(Control node, Control content)
        {
            var rail = new Grid { Width = 22 };
            rail.Children.Add(new Border
            {
                Width = 2,
                Background = RailBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
            });
            rail.Children.Add(node);
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("22,*") };
            grid.Children.Add(rail);
            Grid.SetColumn(content, 1);
            grid.Children.Add(content);
            return grid;
        }

        private static Control RailNode(IBrush brush) => new Border
        {
            Width = 10, Height = 10,
            CornerRadius = new global::Avalonia.CornerRadius(5),
            Background = brush,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new global::Avalonia.Thickness(0, 14, 0, 0),
        };

        private static Control RailDiamond() => new Border
        {
            Width = 9, Height = 9,
            Background = GoldBrush,
            RenderTransform = new RotateTransform(45),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        /// <summary>One timeline card: colored title, "N skills · duration", attribute
        /// chips (top two bolded), and the dominant pair tag.</summary>
        private Border SegmentCard(string title, IBrush titleBrush, string subtitle,
            System.Collections.Generic.IReadOnlyDictionary<EveAttribute, long> attrs,
            string pairLabel)
        {
            var stack = new StackPanel { Spacing = 5 };
            stack.Children.Add(Text(title, titleBrush, FontScaleService.Body, FontWeight.SemiBold));
            stack.Children.Add(Text(subtitle, DimBrush, FontScaleService.Small));

            long secondHighest = attrs.Values.OrderByDescending(v => v).Skip(1).First();
            var attrLine = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            foreach (var attr in new[] { EveAttribute.Perception, EveAttribute.Willpower,
                EveAttribute.Intelligence, EveAttribute.Memory, EveAttribute.Charisma })
            {
                attrLine.Children.Add(new Border
                {
                    Background = new SolidColorBrush(Color.Parse("#FF14142A")),
                    CornerRadius = new global::Avalonia.CornerRadius(4),
                    Padding = new global::Avalonia.Thickness(8, 3),
                    Child = Text($"{AttrShort(attr)} {attrs[attr]}",
                        attrs[attr] >= secondHighest ? Brushes.White : DimBrush,
                        FontScaleService.Small,
                        attrs[attr] >= secondHighest ? FontWeight.Bold : FontWeight.Normal),
                });
            }
            stack.Children.Add(attrLine);

            if (!string.IsNullOrEmpty(pairLabel))
            {
                stack.Children.Add(new Border
                {
                    Background = new SolidColorBrush(Color.Parse("#FF1A2A3E")),
                    CornerRadius = new global::Avalonia.CornerRadius(4),
                    Padding = new global::Avalonia.Thickness(8, 2),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Child = Text(pairLabel, new SolidColorBrush(Color.Parse("#FF7FB3D5")),
                        FontScaleService.Caption),
                });
            }

            return new Border
            {
                Background = PanelBg,
                CornerRadius = new global::Avalonia.CornerRadius(6),
                Padding = new global::Avalonia.Thickness(14, 10),
                // Always present, so selection never causes a layout shift.
                BorderThickness = new global::Avalonia.Thickness(1),
                BorderBrush = Brushes.Transparent,
                Cursor = new global::Avalonia.Input.Cursor(
                    global::Avalonia.Input.StandardCursorType.Hand),
                Child = stack,
            };
        }

        /// <summary>The inspector for one proposed remap: why here, segment length,
        /// budget used, starts-after, and the affected-skills flyout.</summary>
        private void ShowRemapDetails(RemapPlanningService.RemapProposal proposal, int index)
        {
            var remap = proposal.Remaps[index];
            DetailsPanel.Children.Clear();
            string skillLabel =
                $"{remap.Skill.LocalizedName} {Common.Models.Skill.GetRomanFromInt(remap.Level)}";
            DetailsPanel.Children.Add(Text(
                string.Format(Loc.Get("Optimizer.RemapBeforeFmt"), skillLabel),
                GoldBrush, FontScaleService.Body, FontWeight.SemiBold));

            DetailsPanel.Children.Add(Text(Loc.Get("Optimizer.WhyHere"), Brushes.White,
                FontScaleService.Small, FontWeight.SemiBold));
            // "plan-start-focused training ends here" read as broken copy: the
            // remap-at-start case gets its own sentence instead of a placeholder.
            bool atPlanStart = index == 0 && proposal.PrefixSkillCount == 0;
            string whyText = atPlanStart
                ? string.Format(Loc.Get("Optimizer.WhyHereStartFmt"),
                    remap.SkillCount, remap.PairLabel)
                : string.Format(Loc.Get("Optimizer.WhyHereFmt"),
                    index == 0 ? proposal.PrefixPairLabel
                        : proposal.Remaps[index - 1].PairLabel,
                    remap.SkillCount, remap.PairLabel);
            var why = Text(whyText, DimBrush, FontScaleService.Small);
            why.TextWrapping = TextWrapping.Wrap;
            DetailsPanel.Children.Add(why);

            if (proposal.TimeSaved > TimeSpan.Zero)
                DetailsPanel.Children.Add(DetailRow(Loc.Get("Optimizer.TimeSavedTotal"),
                    FormatTime(proposal.TimeSaved), GreenBrush));
            DetailsPanel.Children.Add(DetailRow(Loc.Get("Optimizer.SegmentLength"),
                FormatTime(remap.SegmentDuration), GreenBrush));
            DetailsPanel.Children.Add(DetailRow(Loc.Get("Optimizer.RemapsUsed"),
                string.Format(Loc.Get("Optimizer.OfFmt"), index + 1, MaxRemapBudget()),
                Brushes.White));
            if (!string.IsNullOrEmpty(remap.StartsAfter))
                DetailsPanel.Children.Add(DetailRow(Loc.Get("Optimizer.StartsAfter"),
                    remap.StartsAfter, GreenBrush));

            var affectedBtn = new Button
            {
                Content = Loc.Get("Optimizer.ViewAffected"),
                FontSize = FontScaleService.Small,
                Padding = new global::Avalonia.Thickness(12, 5),
                CornerRadius = new global::Avalonia.CornerRadius(12),
                Margin = new global::Avalonia.Thickness(0, 6, 0, 0),
            };
            var list = new StackPanel { Spacing = 2 };
            foreach (string name in remap.SkillNames)
                list.Children.Add(Text(name, DimBrush, FontScaleService.Small));
            affectedBtn.Flyout = new Flyout
            {
                Content = new ScrollViewer { MaxHeight = 280, Content = list },
            };
            DetailsPanel.Children.Add(affectedBtn);
        }

        private void ShowKeepCurrentDetails(RemapPlanningService.RemapProposal proposal)
        {
            DetailsPanel.Children.Clear();
            DetailsPanel.Children.Add(Text(Loc.Get("Optimizer.KeepCurrent"),
                GreenBrush, FontScaleService.Body, FontWeight.SemiBold));
            var body = Text(string.Format(Loc.Get("Optimizer.KeepCurrentWhy"),
                    proposal.PrefixSkillCount), DimBrush, FontScaleService.Small);
            body.TextWrapping = TextWrapping.Wrap;
            DetailsPanel.Children.Add(body);
            if (proposal.PrefixDuration > TimeSpan.Zero)
                DetailsPanel.Children.Add(DetailRow(Loc.Get("Optimizer.SegmentLength"),
                    FormatTime(proposal.PrefixDuration), GreenBrush));
        }

        private Control DetailRow(string label, string value, IBrush valueBrush)
        {
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
            grid.Children.Add(Text(label, DimBrush, FontScaleService.Small));
            var v = Text(value, valueBrush, FontScaleService.Small, FontWeight.SemiBold);
            v.HorizontalAlignment = HorizontalAlignment.Right;
            Grid.SetColumn(v, 1);
            grid.Children.Add(v);
            return grid;
        }

        /// <summary>The character's live base attributes, for the keep-current card.</summary>
        private System.Collections.Generic.Dictionary<EveAttribute, long> CurrentAttributeValues()
        {
            var result = new System.Collections.Generic.Dictionary<EveAttribute, long>();
            foreach (var attr in new[] { EveAttribute.Perception, EveAttribute.Willpower,
                EveAttribute.Intelligence, EveAttribute.Memory, EveAttribute.Charisma })
            {
                result[attr] = _character?[attr].Base ?? 17;
            }
            return result;
        }

        /// <summary>A slim proportional bar: how the optimized plan divides its time —
        /// teal for the keep-current prefix, gold shades for the remap segments.</summary>
        private Control DurationBar(RemapPlanningService.RemapProposal proposal)
        {
            var columns = new ColumnDefinitions();
            var pieces = new System.Collections.Generic.List<(TimeSpan Dur, IBrush Brush)>();
            if (proposal.PrefixDuration > TimeSpan.Zero)
                pieces.Add((proposal.PrefixDuration,
                    new SolidColorBrush(Color.Parse("#FF2BB5AD"))));
            string[] golds = { "#FFE6A817", "#FFC98F12", "#FFB07C0E", "#FF96690B" };
            for (int i = 0; i < proposal.Remaps.Count; i++)
                pieces.Add((proposal.Remaps[i].SegmentDuration,
                    new SolidColorBrush(Color.Parse(golds[i % golds.Length]))));

            var grid = new Grid { Height = 6, Margin = new global::Avalonia.Thickness(0, 6, 0, 0) };
            foreach (var piece in pieces)
                columns.Add(new ColumnDefinition(
                    Math.Max(1, piece.Dur.TotalHours), GridUnitType.Star));
            grid.ColumnDefinitions = columns;
            for (int i = 0; i < pieces.Count; i++)
            {
                var bar = new Border
                {
                    Background = pieces[i].Brush,
                    CornerRadius = new global::Avalonia.CornerRadius(3),
                    Margin = new global::Avalonia.Thickness(i == 0 ? 0 : 1, 0, 0, 0),
                };
                Grid.SetColumn(bar, i);
                grid.Children.Add(bar);
            }

            double total = pieces.Sum(p => p.Dur.TotalHours);
            var captions = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 14,
                Margin = new global::Avalonia.Thickness(0, 3, 0, 0),
            };
            foreach (var piece in pieces)
                captions.Children.Add(Text(
                    $"{FormatTime(piece.Dur)} ({(total > 0 ? 100 * piece.Dur.TotalHours / total : 0):F1}%)",
                    DimBrush, FontScaleService.Caption));

            var wrap = new StackPanel();
            wrap.Children.Add(grid);
            wrap.Children.Add(captions);
            return wrap;
        }

        private void ApplyProposal()
        {
            if (_targetPlan == null || _proposal == null) return;

            // Single atomic mutation — clears old remap points, writes the proposal.
            // Applied to the REAL plan; remap points carry to the display plan on refresh
            // (matched by skill+level).
            RemapPlanningService.Apply(_targetPlan, _proposal);
            // Record the clone this schedule was computed FOR: applying an Omega
            // what-if on an Alpha character must leave a verdict that says Omega.
            _targetPlan.OptimizedForClone =
                (SelectedCloneOverride ?? (AccountStatusMode?)null)?.ToString()
                ?? _character?.EffectiveCharacterStatus.ToString();
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
