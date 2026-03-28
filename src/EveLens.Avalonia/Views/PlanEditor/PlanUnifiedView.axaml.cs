// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using EveLens.Common.Enumerations;
using EveLens.Common.Helpers;
using EveLens.Common.Models;
using EveLens.Common.Services;
using EveLens.Common.SettingsObjects;
using EveLens.Common.ViewModels;
using EveLens.Avalonia.ViewModels;
using EveLens.SkillPlanner;

using EveLens.SkillPlanner;
using EveLens.Avalonia.Services;
namespace EveLens.Avalonia.Views.PlanEditor
{
    public partial class PlanUnifiedView : UserControl
    {
        private PlanEditorViewModel? _viewModel;
        private PlanOptimizerViewModel? _optimizerVm;
        private string _currentFilter = "";
        private List<IPlanDisplayItem>? _currentDisplayItems;
        private List<PlanEntryDisplayItem>? _currentEntryItems;
        private HashSet<string> _previousEntryKeys = new();
        private HashSet<string> _recentlyMovedKeys = new();
        private string _activeSidebarTab = "Plan";
        private bool _isSidebarExpanded = true;
        private bool _showAdvanced;



        // Segment data for sidebar rendering
        private List<SegmentInfo> _segments = new();

        // Attribute color brushes
        private static readonly IBrush AttrIntBrush = new SolidColorBrush(Color.Parse("#FF4FC3F7"));
        private static readonly IBrush AttrPerBrush = new SolidColorBrush(Color.Parse("#FFEF5350"));
        private static readonly IBrush AttrChaBrush = new SolidColorBrush(Color.Parse("#FF66BB6A"));
        private static readonly IBrush AttrWilBrush = new SolidColorBrush(Color.Parse("#FFAB47BC"));
        private static readonly IBrush AttrMemBrush = new SolidColorBrush(Color.Parse("#FFFFA726"));
        private static readonly IBrush GoldBrush = new SolidColorBrush(Color.Parse("#FFE6A817"));

        private static IBrush GetAttributeBrush(EveAttribute attr) => attr switch
        {
            EveAttribute.Intelligence => AttrIntBrush,
            EveAttribute.Perception => AttrPerBrush,
            EveAttribute.Charisma => AttrChaBrush,
            EveAttribute.Willpower => AttrWilBrush,
            EveAttribute.Memory => AttrMemBrush,
            _ => Brushes.Gray
        };

        private static string GetAttributeShortName(EveAttribute attr) => attr switch
        {
            EveAttribute.Intelligence => "INT",
            EveAttribute.Perception => "PER",
            EveAttribute.Charisma => "CHA",
            EveAttribute.Willpower => "WIL",
            EveAttribute.Memory => "MEM",
            _ => attr.ToString()
        };

        private static string GetAttributeFullName(EveAttribute attr) => attr switch
        {
            EveAttribute.Intelligence => "Intelligence",
            EveAttribute.Perception => "Perception",
            EveAttribute.Charisma => "Charisma",
            EveAttribute.Willpower => "Willpower",
            EveAttribute.Memory => "Memory",
            _ => attr.ToString()
        };

        public PlanUnifiedView()
        {
            InitializeComponent();
        }

        public void SetViewModel(PlanEditorViewModel viewModel)
        {
            _viewModel = viewModel;
            UpdateStatsHeader();
            Refresh();
            BuildSidebarContent();
        }

        public void RefreshSkillList(string filter)
        {
            _currentFilter = filter;
            Refresh();
        }

        #region Segmented List Building

        private void Refresh()
        {
            if (_viewModel?.DisplayPlan == null)
            {
                PlanItemsControl.ItemsSource = null;
                return;
            }

            Character? character = _viewModel.Character;
            PlanEntry[] entries = _viewModel.DisplayPlan.ToArray();

            // Build segmented display list
            var displayItems = new List<IPlanDisplayItem>();
            var entryItems = new List<PlanEntryDisplayItem>();
            _segments = new List<SegmentInfo>();

            // Identify segments by remap points
            var segmentEntries = new List<List<PlanEntry>>();
            var segmentRemapEntries = new List<PlanEntry?>(); // entry that has the remap point
            var currentSegment = new List<PlanEntry>();
            segmentEntries.Add(currentSegment);
            segmentRemapEntries.Add(null);

            foreach (var entry in entries)
            {
                if (entry.Remapping != null && currentSegment.Count > 0)
                {
                    // Start new segment
                    currentSegment = new List<PlanEntry>();
                    segmentEntries.Add(currentSegment);
                    segmentRemapEntries.Add(entry);
                }
                currentSegment.Add(entry);
            }

            // If first entry has a remap but we didn't split (it was the first),
            // record it on the first segment
            if (entries.Length > 0 && entries[0].Remapping != null)
                segmentRemapEntries[0] = entries[0];

            // Build display items for each segment
            for (int segIdx = 0; segIdx < segmentEntries.Count; segIdx++)
            {
                var segEntries = segmentEntries[segIdx];
                if (segEntries.Count == 0) continue;

                // Determine segment focus (dominant primary attribute pair)
                var attrCounts = new Dictionary<EveAttribute, int>();
                double totalSpPerHour = 0;
                var segTrainingTime = TimeSpan.Zero;

                foreach (var entry in segEntries)
                {
                    var pri = entry.Skill.PrimaryAttribute;
                    attrCounts.TryGetValue(pri, out int count);
                    attrCounts[pri] = count + 1;
                    totalSpPerHour += entry.SpPerHour;
                    segTrainingTime += entry.TrainingTime;
                }

                double avgSpPerHour = segEntries.Count > 0 ? totalSpPerHour / segEntries.Count : 0;

                // Find dominant primary and secondary
                var sortedAttrs = attrCounts.OrderByDescending(kv => kv.Value).ToList();
                var dominantPrimary = sortedAttrs.Count > 0 ? sortedAttrs[0].Key : EveAttribute.Intelligence;

                // Find dominant secondary among skills with the dominant primary
                var secCounts = new Dictionary<EveAttribute, int>();
                foreach (var entry in segEntries)
                {
                    if (entry.Skill.PrimaryAttribute == dominantPrimary)
                    {
                        var sec = entry.Skill.SecondaryAttribute;
                        secCounts.TryGetValue(sec, out int c);
                        secCounts[sec] = c + 1;
                    }
                }
                var dominantSecondary = secCounts.Count > 0
                    ? secCounts.OrderByDescending(kv => kv.Value).First().Key
                    : EveAttribute.Memory;

                string priShort = GetAttributeShortName(dominantPrimary);
                string secShort = GetAttributeShortName(dominantSecondary);
                string focusLabel = $"{GetAttributeFullName(dominantPrimary)} / {GetAttributeFullName(dominantSecondary)} Focus";

                // Store segment info for sidebar
                _segments.Add(new SegmentInfo
                {
                    Index = segIdx,
                    FocusLabel = focusLabel,
                    PrimaryShort = priShort,
                    SecondaryShort = secShort,
                    SkillCount = segEntries.Count,
                    TrainingTime = segTrainingTime,
                    AvgSpPerHour = avgSpPerHour,
                    DominantPrimary = dominantPrimary,
                    DominantSecondary = dominantSecondary,
                    RemapEntry = segmentRemapEntries[segIdx],
                });

                // Insert remap divider if this segment has a remap point
                if (segmentRemapEntries[segIdx] != null)
                {
                    var remapEntry = segmentRemapEntries[segIdx]!;
                    var rp = remapEntry.Remapping!;
                    bool isComputed = rp.Status == RemappingPointStatus.UpToDate;

                    if (isComputed)
                    {
                        // Build delta list comparing remap target to character's current attrs
                        var deltas = new List<AttributeDelta>();
                        var allAttrs = new[]
                        {
                            EveAttribute.Intelligence, EveAttribute.Perception,
                            EveAttribute.Charisma, EveAttribute.Willpower, EveAttribute.Memory
                        };
                        foreach (var attr in allAttrs)
                        {
                            int remapVal = (int)rp[attr];
                            int currentVal = character != null ? (int)character[attr].Base : remapVal;
                            int delta = remapVal - currentVal;
                            if (delta != 0)
                            {
                                deltas.Add(new AttributeDelta
                                {
                                    Name = GetAttributeShortName(attr),
                                    NewValue = remapVal,
                                    Delta = delta,
                                    ValueBrush = GetAttributeBrush(attr),
                                    DeltaBrush = delta > 0
                                        ? new SolidColorBrush(Color.Parse("#FF81C784"))
                                        : new SolidColorBrush(Color.Parse("#FFCF6679")),
                                });
                            }
                        }

                        // Remap availability
                        string availText = "";
                        IBrush? availBrush = null;
                        if (character is Character charModel)
                        {
                            bool canRemap = charModel.AvailableReMaps > 0
                                || charModel.LastReMapTimed == DateTime.MinValue
                                || DateTime.UtcNow >= charModel.LastReMapTimed.AddDays(365);
                            if (canRemap)
                            {
                                availText = "Remap available";
                                availBrush = new SolidColorBrush(Color.Parse("#FF81C784"));
                            }
                            else
                            {
                                int daysUntil = (int)(charModel.LastReMapTimed.AddDays(365) - DateTime.UtcNow).TotalDays;
                                availText = $"Remap in {daysUntil} days";
                                availBrush = new SolidColorBrush(Color.Parse("#FFFFD54F"));
                            }
                        }

                        displayItems.Add(new PlanRemapDivider
                        {
                            Intelligence = (int)rp[EveAttribute.Intelligence],
                            Perception = (int)rp[EveAttribute.Perception],
                            Willpower = (int)rp[EveAttribute.Willpower],
                            Charisma = (int)rp[EveAttribute.Charisma],
                            Memory = (int)rp[EveAttribute.Memory],
                            AttributeSummary = FormatRemapAttributes(rp),
                            IsComputed = true,
                            Deltas = deltas,
                            AvailabilityText = availText,
                            AvailabilityBrush = availBrush,
                            DominantPrimary = dominantPrimary,
                            DominantSecondary = dominantSecondary,
                            SourceEntry = remapEntry,
                        });
                    }
                    else
                    {
                        displayItems.Add(new PlanRemapDivider
                        {
                            AttributeSummary = "Not optimized",
                            IsComputed = false,
                            DominantPrimary = dominantPrimary,
                            DominantSecondary = dominantSecondary,
                            SourceEntry = remapEntry,
                        });
                    }
                }

                // Insert section header
                displayItems.Add(new PlanSectionHeader
                {
                    SegmentIndex = segIdx,
                    FocusLabel = focusLabel,
                    PrimaryShortName = priShort,
                    SecondaryShortName = secShort,
                    SkillCount = segEntries.Count,
                    TrainingTimeText = FormatTime(segTrainingTime),
                    AvgSpPerHourText = $"{avgSpPerHour:N0} SP/hr",
                    BackgroundBrush = PlanSectionHeader.GetFocusBackground(priShort),
                    AccentBrush = PlanSectionHeader.GetAccentBrush(priShort),
                });

                // Insert skill entries
                foreach (var entry in segEntries)
                {
                    PlanEntryStatus status = DetermineStatus(entry, character);
                    var item = new PlanEntryDisplayItem(entry, status)
                    {
                        SegmentAverageSpPerHour = avgSpPerHour,
                    };

                    // Set trained level
                    if (entry.CharacterSkill != null)
                        item.TrainedLevel = (int)entry.CharacterSkill.Level;

                    entryItems.Add(item);
                    displayItems.Add(item);
                }
            }

            // Apply text filter — only filter skill entries, keep headers/dividers
            if (!string.IsNullOrEmpty(_currentFilter))
            {
                string lowerFilter = _currentFilter.ToLowerInvariant();
                var filteredDisplay = new List<IPlanDisplayItem>();
                var filteredEntries = new List<PlanEntryDisplayItem>();

                foreach (var item in displayItems)
                {
                    if (item is PlanEntryDisplayItem entryItem)
                    {
                        if (entryItem.SkillName.ToLowerInvariant().Contains(lowerFilter) ||
                            entryItem.GroupName.ToLowerInvariant().Contains(lowerFilter))
                        {
                            filteredDisplay.Add(entryItem);
                            filteredEntries.Add(entryItem);
                        }
                    }
                    else
                    {
                        // Keep section headers and dividers if there are matching entries after them
                        filteredDisplay.Add(item);
                    }
                }

                // Remove orphaned headers/dividers (those not followed by any skill entries)
                var cleanedDisplay = new List<IPlanDisplayItem>();
                for (int i = 0; i < filteredDisplay.Count; i++)
                {
                    if (filteredDisplay[i] is PlanEntryDisplayItem)
                    {
                        cleanedDisplay.Add(filteredDisplay[i]);
                    }
                    else
                    {
                        // Check if any skill entry follows before the next header/divider
                        bool hasFollowingEntry = false;
                        for (int j = i + 1; j < filteredDisplay.Count; j++)
                        {
                            if (filteredDisplay[j] is PlanEntryDisplayItem)
                            {
                                hasFollowingEntry = true;
                                break;
                            }
                            if (filteredDisplay[j] is PlanSectionHeader || filteredDisplay[j] is PlanRemapDivider)
                                break;
                        }
                        if (hasFollowingEntry)
                            cleanedDisplay.Add(filteredDisplay[i]);
                    }
                }

                displayItems = cleanedDisplay;
                entryItems = filteredEntries;
            }

            _currentDisplayItems = displayItems;
            _currentEntryItems = entryItems;

            // Set position flags and prerequisite-aware move availability on entry items
            for (int i = 0; i < entryItems.Count; i++)
            {
                var item = entryItems[i];
                item.IsFirstItem = (i == 0);
                item.IsLastItem = (i == entryItems.Count - 1);

                bool canUp = i > 0;
                if (canUp)
                {
                    var above = entryItems[i - 1];
                    if (item.Entry.IsDependentOf(above.Entry))
                        canUp = false;
                }
                item.CanMoveUp = canUp;

                bool canDown = i < entryItems.Count - 1;
                if (canDown)
                {
                    var below = entryItems[i + 1];
                    if (below.Entry.IsDependentOf(item.Entry))
                        canDown = false;
                }
                item.CanMoveDown = canDown;
            }

            // Apply move highlight
            if (_recentlyMovedKeys.Count > 0)
            {
                foreach (var item in entryItems)
                {
                    string key = $"{item.Entry.Skill.ID}_{item.Entry.Level}";
                    if (_recentlyMovedKeys.Contains(key))
                        item.IsRecentlyMoved = true;
                }
                _recentlyMovedKeys = new();
            }

            // Highlight newly-added skills
            var newKeys = new HashSet<string>(entryItems.Select(d => $"{d.Entry.Skill.ID}_{d.Entry.Level}"));
            if (_previousEntryKeys.Count > 0)
            {
                foreach (var item in entryItems)
                {
                    string key = $"{item.Entry.Skill.ID}_{item.Entry.Level}";
                    if (!_previousEntryKeys.Contains(key))
                        item.IsNewlyAdded = true;
                }
            }
            _previousEntryKeys = newKeys;

            RebuildItemsControlFromDisplayItems(displayItems);
            BuildTimelineBar();
            UpdateStatsHeader();
        }

        private void RebuildItemsControlFromDisplayItems(List<IPlanDisplayItem> items)
        {
            var controls = new List<Control>();
            foreach (var item in items)
            {
                switch (item)
                {
                    case PlanRemapDivider divider:
                        controls.Add(BuildRemapDividerRow(divider));
                        break;
                    case PlanSectionHeader header:
                        controls.Add(BuildSectionHeaderRow(header));
                        break;
                    case PlanEntryDisplayItem entry:
                        controls.Add(BuildSkillRow(entry));
                        break;
                }
            }
            PlanItemsControl.ItemsSource = controls;
        }

        private static string FormatRemapAttributes(RemappingPoint rp)
        {
            return $"INT {rp[EveAttribute.Intelligence]}  PER {rp[EveAttribute.Perception]}  " +
                   $"WIL {rp[EveAttribute.Willpower]}  CHA {rp[EveAttribute.Charisma]}  " +
                   $"MEM {rp[EveAttribute.Memory]}";
        }

        #endregion

        #region Row Builders

        private Control BuildRemapDividerRow(PlanRemapDivider divider)
        {
            var border = new Border
            {
                Background = divider.BackgroundBrush,
                Padding = new Thickness(8, 4),
                BorderBrush = new SolidColorBrush(Color.Parse("#40E6A817")),
                BorderThickness = new Thickness(0, 1),
            };

            // Context menu for all remap dividers (computed or not)
            border.ContextMenu = BuildRemapContextMenu(divider);

            if (!divider.IsComputed)
            {
                // Uncomputed: compact one-liner with hint
                border.Child = new TextBlock
                {
                    Text = "\u25C7 Remap point \u2014 right-click to configure",
                    FontSize = FontScaleService.Caption,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = new SolidColorBrush(Color.Parse("#99E6A817")),
                    VerticalAlignment = VerticalAlignment.Center,
                };
                return border;
            }

            // Computed: compact one-liner — ◆ Remap — INT→24  PER→21  ... · saves Xd  ✓
            var line = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 4,
            };

            line.Children.Add(new TextBlock
            {
                Text = "\u25C6 Remap \u2014",
                FontSize = FontScaleService.Caption,
                FontWeight = FontWeight.Bold,
                Foreground = GoldBrush,
                VerticalAlignment = VerticalAlignment.Center,
            });

            // Show changed attributes with target values
            if (divider.Deltas.Count > 0)
            {
                foreach (var delta in divider.Deltas)
                {
                    line.Children.Add(new TextBlock
                    {
                        Text = $"{delta.Name}\u2192{delta.NewValue}",
                        FontSize = FontScaleService.Caption,
                        Foreground = delta.ValueBrush,
                        FontWeight = FontWeight.SemiBold,
                        VerticalAlignment = VerticalAlignment.Center,
                    });
                }
            }
            else
            {
                // No changes — show all attributes
                line.Children.Add(new TextBlock
                {
                    Text = $"INT\u2192{divider.Intelligence}  PER\u2192{divider.Perception}  WIL\u2192{divider.Willpower}  CHA\u2192{divider.Charisma}  MEM\u2192{divider.Memory}",
                    FontSize = FontScaleService.Caption,
                    Foreground = new SolidColorBrush(Color.Parse("#FFD0D0D0")),
                    VerticalAlignment = VerticalAlignment.Center,
                });
            }

            // Time savings
            if (!string.IsNullOrEmpty(divider.TimeSavingsText))
            {
                line.Children.Add(new TextBlock
                {
                    Text = "\u00B7",
                    FontSize = FontScaleService.Caption,
                    Foreground = new SolidColorBrush(Color.Parse("#FF606060")),
                    VerticalAlignment = VerticalAlignment.Center,
                });
                line.Children.Add(new TextBlock
                {
                    Text = divider.TimeSavingsText,
                    FontSize = FontScaleService.Caption,
                    Foreground = new SolidColorBrush(Color.Parse("#FF81C784")),
                    VerticalAlignment = VerticalAlignment.Center,
                });
            }

            // Availability icon
            if (!string.IsNullOrEmpty(divider.AvailabilityText))
            {
                bool isAvailable = divider.AvailabilityText.Contains("available", StringComparison.OrdinalIgnoreCase);
                line.Children.Add(new TextBlock
                {
                    Text = isAvailable ? "\u2713" : "\u23F0",
                    FontSize = FontScaleService.Caption,
                    Foreground = divider.AvailabilityBrush ?? new SolidColorBrush(Color.Parse("#FF909090")),
                    VerticalAlignment = VerticalAlignment.Center,
                });
            }

            border.Child = line;

            // Tooltip with full attribute breakdown
            var tooltipText = $"Remap to: INT {divider.Intelligence}  PER {divider.Perception}  " +
                              $"WIL {divider.Willpower}  CHA {divider.Charisma}  MEM {divider.Memory}";
            if (!string.IsNullOrEmpty(divider.AvailabilityText))
                tooltipText += $"\n{divider.AvailabilityText}";
            if (divider.Deltas.Count > 0)
            {
                tooltipText += "\n\nChanges:";
                foreach (var delta in divider.Deltas)
                {
                    string sign = delta.Delta > 0 ? "+" : "";
                    tooltipText += $"\n  {delta.Name}: {delta.NewValue} ({sign}{delta.Delta})";
                }
            }
            ToolTip.SetTip(border, tooltipText);

            return border;
        }

        private void ApplyMatchRemap(PlanRemapDivider divider)
        {
            if (divider.SourceEntry?.Remapping == null || _viewModel?.Plan == null) return;

            var planEntry = _viewModel.Plan.GetEntry(divider.SourceEntry.Skill, divider.SourceEntry.Level);
            if (planEntry?.Remapping == null) return;

            // Standard EVE remap: primary → 27 (base 17 + 10), secondary → 21 (base 17 + 4), rest → 17
            int baseVal = 17;
            int priVal = 27;
            int secVal = 21;

            var vals = new Dictionary<EveAttribute, int>
            {
                [EveAttribute.Intelligence] = baseVal,
                [EveAttribute.Perception] = baseVal,
                [EveAttribute.Charisma] = baseVal,
                [EveAttribute.Willpower] = baseVal,
                [EveAttribute.Memory] = baseVal,
            };
            vals[divider.DominantPrimary] = priVal;
            // Only set secondary if it's different from primary
            if (divider.DominantSecondary != divider.DominantPrimary)
                vals[divider.DominantSecondary] = secVal;

            planEntry.Remapping.SetAttributes(
                vals[EveAttribute.Intelligence],
                vals[EveAttribute.Perception],
                vals[EveAttribute.Charisma],
                vals[EveAttribute.Willpower],
                vals[EveAttribute.Memory]);

            _viewModel.UpdateDisplayPlan();
            Refresh();
            BuildSidebarContent();
        }

        private ContextMenu BuildRemapContextMenu(PlanRemapDivider divider)
        {
            var menu = new ContextMenu();

            string priName = GetAttributeShortName(divider.DominantPrimary);
            string secName = GetAttributeShortName(divider.DominantSecondary);

            // Auto-optimize
            var autoItem = new MenuItem { Header = $"\u26A1 Auto-optimize all remap points" };
            autoItem.Click += (_, _) => EnsureOptimizationRun();
            menu.Items.Add(autoItem);

            // Match dominant attributes
            var matchItem = new MenuItem { Header = $"Match {priName}/{secName} ({priName} 27, {secName} 21)" };
            matchItem.Click += (_, _) => ApplyMatchRemap(divider);
            menu.Items.Add(matchItem);

            // Set manually (opens dialog)
            var manualItem = new MenuItem { Header = "Set attributes manually\u2026" };
            manualItem.Click += async (_, _) =>
            {
                try
                {
                    await OpenManualRemapDialog(divider);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error opening remap dialog: {ex}");
                }
            };
            menu.Items.Add(manualItem);

            menu.Items.Add(new Separator());

            // Remove remap point
            var removeItem = new MenuItem { Header = "Remove remap point" };
            removeItem.Click += (_, _) =>
            {
                if (_viewModel?.Plan == null || divider.SourceEntry == null) return;
                var planEntry = _viewModel.Plan.GetEntry(divider.SourceEntry.Skill, divider.SourceEntry.Level);
                if (planEntry != null)
                {
                    planEntry.Remapping = null!;
                    _viewModel.UpdateDisplayPlan();
                    Refresh();
                    BuildSidebarContent();
                }
            };
            menu.Items.Add(removeItem);

            return menu;
        }

        private async System.Threading.Tasks.Task OpenManualRemapDialog(PlanRemapDivider divider)
        {
            if (divider.SourceEntry?.Remapping == null || _viewModel?.Plan == null) return;

            var planEntry = _viewModel.Plan.GetEntry(divider.SourceEntry.Skill, divider.SourceEntry.Level);
            if (planEntry?.Remapping == null) return;

            var rp = planEntry.Remapping;
            var parentWindow = this.FindAncestorOfType<Window>();
            if (parentWindow == null) return;

            var dialog = new EveLens.Avalonia.Views.Dialogs.RemapAttributeDialog();
            dialog.Initialize(rp, divider.DominantPrimary, divider.DominantSecondary);
            await dialog.ShowDialog(parentWindow);

            if (dialog.WasApplied)
            {
                _viewModel.UpdateDisplayPlan();
                Refresh();
                BuildSidebarContent();
            }
        }

        private Control BuildSectionHeaderRow(PlanSectionHeader header)
        {
            var border = new Border
            {
                Background = header.BackgroundBrush,
                Padding = new Thickness(8, 5),
                Margin = new Thickness(0, header.SegmentIndex > 0 ? 2 : 0, 0, 0),
            };

            var grid = new Grid
            {
                ColumnDefinitions = ColumnDefinitions.Parse("Auto,*,Auto,Auto"),
            };

            // Colored accent strip
            var strip = new Border
            {
                Width = 3,
                Background = header.AccentBrush,
                CornerRadius = new CornerRadius(1),
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Stretch,
            };
            Grid.SetColumn(strip, 0);
            grid.Children.Add(strip);

            // Focus label
            var label = new TextBlock
            {
                Text = header.FocusLabel,
                FontSize = FontScaleService.Body,
                FontWeight = FontWeight.SemiBold,
                Foreground = header.AccentBrush,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(label, 1);
            grid.Children.Add(label);

            // Skill count
            var countText = new TextBlock
            {
                Text = $"{header.SkillCount} skills",
                FontSize = FontScaleService.Small,
                Foreground = new SolidColorBrush(Color.Parse("#FF909090")),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0),
            };
            Grid.SetColumn(countText, 2);
            grid.Children.Add(countText);

            // Training time
            var timeText = new TextBlock
            {
                Text = header.TrainingTimeText,
                FontSize = FontScaleService.Small,
                FontWeight = FontWeight.SemiBold,
                Foreground = new SolidColorBrush(Color.Parse("#FFF0F0F0")),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0),
            };
            Grid.SetColumn(timeText, 3);
            grid.Children.Add(timeText);

            border.Child = grid;
            return border;
        }

        private Control BuildSkillRow(PlanEntryDisplayItem item)
        {
            bool isOmega = item.Entry.OmegaRequired;

            var normalBg = item.RowBackground;
            var hoverBg = new SolidColorBrush(Color.Parse("#FF252540"));

            var border = new Border
            {
                Padding = new Thickness(0, 3),
                Background = normalBg,
                Cursor = new Cursor(StandardCursorType.Hand),
            };

            border.PointerEntered += (_, _) => border.Background = hoverBg;
            border.PointerExited += (_, _) => border.Background = normalBg;

            // Context menu on the skill row
            border.ContextMenu = BuildSkillContextMenu(item);

            var grid = new Grid
            {
                ColumnDefinitions = ColumnDefinitions.Parse("*,100,45,75,75,65,60"),
            };

            // Column 0: Skill name with Omega badge, move buttons, indicators
            var namePanel = new Panel();

            // Newly added / recently moved indicators
            if (item.IsNewlyAdded)
            {
                namePanel.Children.Add(new Border
                {
                    Width = 3,
                    Background = new SolidColorBrush(Color.Parse("#FFE6A817")),
                    HorizontalAlignment = HorizontalAlignment.Left,
                });
            }
            else if (item.IsRecentlyMoved)
            {
                namePanel.Children.Add(new Border
                {
                    Width = 3,
                    Background = new SolidColorBrush(Color.Parse("#FFFFD54F")),
                    HorizontalAlignment = HorizontalAlignment.Left,
                });
            }

            var nameStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 0, 0),
            };

            // Skillbook not owned indicator
            bool needsBook = item.Entry.Level == 1
                && item.Entry.CharacterSkill != null
                && !item.Entry.CharacterSkill.IsOwned
                && !item.Entry.CharacterSkill.IsKnown;

            if (needsBook)
            {
                var bookBadge = new Border
                {
                    Background = new SolidColorBrush(Color.Parse("#20CF6679")),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(3, 0),
                    Margin = new Thickness(0, 0, 4, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = new TextBlock
                    {
                        Text = "\uD83D\uDCD6",
                        FontSize = FontScaleService.Caption,
                    }
                };
                ToolTip.SetTip(bookBadge, $"Skillbook not owned \u2014 {item.Entry.Skill.FormattedCost}");
                nameStack.Children.Add(bookBadge);
            }

            // Omega indicator — subtle if character is already Omega
            bool isCharOmega = _viewModel?.Character is Character c
                && c.EffectiveCharacterStatus == AccountStatus.Omega;

            if (isOmega)
            {
                if (isCharOmega)
                {
                    // Already Omega — subtle gray badge, no alarm color
                    var omegaBadge = new TextBlock
                    {
                        Text = "\u03A9",
                        FontSize = FontScaleService.Caption,
                        Foreground = new SolidColorBrush(Color.Parse("#FF707070")),
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 4, 0),
                    };
                    ToolTip.SetTip(omegaBadge, "Omega skill \u2014 your account is Omega");
                    nameStack.Children.Add(omegaBadge);
                }
                else
                {
                    // Alpha account — highlight that Omega is needed
                    var omegaBadge = new Border
                    {
                        Background = new SolidColorBrush(Color.Parse("#20FFD54F")),
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(3, 0),
                        Margin = new Thickness(0, 0, 4, 0),
                        VerticalAlignment = VerticalAlignment.Center,
                        Child = new TextBlock
                        {
                            Text = "\u03A9",
                            FontSize = FontScaleService.Small,
                            FontWeight = FontWeight.Bold,
                            Foreground = new SolidColorBrush(Color.Parse("#FFFFD54F")),
                        }
                    };
                    ToolTip.SetTip(omegaBadge, "Requires Omega clone");
                    nameStack.Children.Add(omegaBadge);
                }
            }

            nameStack.Children.Add(new TextBlock
            {
                Text = item.SkillName,
                FontSize = FontScaleService.Body,
                FontWeight = FontWeight.SemiBold,
                Foreground = isOmega && !isCharOmega
                    ? new SolidColorBrush(Color.Parse("#FFFFD54F"))
                    : new SolidColorBrush(Color.Parse("#FFF0F0F0")),
                TextTrimming = TextTrimming.CharacterEllipsis,
            });

            if (item.CanMoveUp)
            {
                var upBtn = new Button
                {
                    Content = "\u25B2",
                    FontSize = FontScaleService.Tiny,
                    Padding = new Thickness(3, 0),
                    CornerRadius = new CornerRadius(3),
                    Background = Brushes.Transparent,
                    Foreground = new SolidColorBrush(Color.Parse("#FF606060")),
                    BorderThickness = new Thickness(0),
                    Margin = new Thickness(6, 0, 0, 0),
                    Cursor = new Cursor(StandardCursorType.Hand),
                    DataContext = item,
                };
                ToolTip.SetTip(upBtn, "Move Up");
                upBtn.Click += OnMoveItemUp;
                nameStack.Children.Add(upBtn);
            }

            if (item.CanMoveDown)
            {
                var downBtn = new Button
                {
                    Content = "\u25BC",
                    FontSize = FontScaleService.Tiny,
                    Padding = new Thickness(3, 0),
                    CornerRadius = new CornerRadius(3),
                    Background = Brushes.Transparent,
                    Foreground = new SolidColorBrush(Color.Parse("#FF606060")),
                    BorderThickness = new Thickness(0),
                    Margin = new Thickness(1, 0, 0, 0),
                    Cursor = new Cursor(StandardCursorType.Hand),
                    DataContext = item,
                };
                ToolTip.SetTip(downBtn, "Move Down");
                downBtn.Click += OnMoveItemDown;
                nameStack.Children.Add(downBtn);
            }

            namePanel.Children.Add(nameStack);
            Grid.SetColumn(namePanel, 0);
            grid.Children.Add(namePanel);

            // Column 1: Training time
            var timeTb = new TextBlock
            {
                Text = item.TrainingTimeText,
                FontSize = FontScaleService.Small,
                Foreground = item.TimeBrush,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 8, 0),
            };
            ToolTip.SetTip(timeTb, item.FinishDateText);
            Grid.SetColumn(timeTb, 1);
            grid.Children.Add(timeTb);

            // Column 2: Rank
            grid.Children.Add(CreateCenteredText(item.RankText, 10,
                new SolidColorBrush(Color.Parse("#FF909090")), 2));

            // Column 3: Primary attribute pill
            grid.Children.Add(CreateAttributePill(item.PrimaryAttr, item.PrimaryAttrBrush,
                item.PrimaryAttrPillBg, 3));

            // Column 4: Secondary attribute pill
            grid.Children.Add(CreateAttributePill(item.SecondaryAttr, item.SecondaryAttrBrush,
                item.SecondaryAttrPillBg, 4));

            // Column 5: SP/hr (color-coded)
            var sphrTb = new TextBlock
            {
                Text = item.SpPerHourText,
                FontSize = FontScaleService.Small,
                Foreground = item.SpPerHourBrush,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 8, 0),
            };
            Grid.SetColumn(sphrTb, 5);
            grid.Children.Add(sphrTb);

            // Column 6: Level blocks (5 small rectangles)
            var levelPanel = BuildLevelBlocks(item.TrainedLevel, item.TargetLevel, item.IsCurrentlyTraining);
            Grid.SetColumn(levelPanel, 6);
            grid.Children.Add(levelPanel);

            border.Child = grid;
            return border;
        }

        private static Control CreateCenteredText(string text, double fontSize, IBrush foreground, int column)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontSize = fontSize,
                Foreground = foreground,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            Grid.SetColumn(tb, column);
            return tb;
        }

        private static Control CreateAttributePill(string text, IBrush foreground, IBrush background, int column)
        {
            var border = new Border
            {
                Background = background,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(6, 1),
                Margin = new Thickness(2, 2),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = text,
                    FontSize = FontScaleService.Caption,
                    Foreground = foreground,
                    HorizontalAlignment = HorizontalAlignment.Center,
                }
            };
            Grid.SetColumn(border, column);
            return border;
        }

        private static Control BuildLevelBlocks(int trainedLevel, int targetLevel, bool isTraining)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 2,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };

            for (int i = 1; i <= 5; i++)
            {
                IBrush fill;
                if (i <= trainedLevel)
                    fill = new SolidColorBrush(Color.Parse("#FFE6A817")); // Gold = trained
                else if (i == targetLevel && isTraining)
                    fill = new SolidColorBrush(Color.Parse("#FF81C784")); // Green = training
                else if (i <= targetLevel)
                    fill = new SolidColorBrush(Color.Parse("#FF4A4A5A")); // Dark = planned but not trained
                else
                    fill = new SolidColorBrush(Color.Parse("#FF252535")); // Darkest = not planned

                panel.Children.Add(new Border
                {
                    Width = 10,
                    Height = 12,
                    CornerRadius = new CornerRadius(2),
                    Background = fill,
                });
            }

            return panel;
        }

        private ContextMenu BuildSkillContextMenu(PlanEntryDisplayItem item)
        {
            var menu = new ContextMenu();

            var planToMenu = new MenuItem { Header = "Plan to Level" };
            int currentLevel = (int)item.Entry.Level;
            for (int lvl = 1; lvl <= 5; lvl++)
            {
                int targetLevel = lvl;
                var mi = new MenuItem
                {
                    Header = $"Level {RomanNumeral(lvl)}",
                    IsEnabled = lvl != currentLevel,
                };
                mi.Click += (_, _) =>
                {
                    if (_viewModel?.Plan == null) return;
                    var plan = _viewModel.Plan;
                    int plannedLevel = plan.GetPlannedLevel(item.Entry.Skill);

                    if (targetLevel >= plannedLevel)
                    {
                        // Adding higher levels — PlanTo handles prerequisites
                        plan.PlanTo(item.Entry.Skill, targetLevel);
                    }
                    else
                    {
                        // Lowering — collect entries above target and cascade remove
                        var entriesToRemove = new List<PlanEntry>();
                        for (int i = plannedLevel; i > targetLevel; i--)
                        {
                            var entry = plan.GetEntry(item.Entry.Skill, i);
                            if (entry != null)
                                entriesToRemove.Add(entry);
                        }
                        if (entriesToRemove.Count > 0)
                        {
                            var op = plan.TryRemoveSet(entriesToRemove);
                            op.Perform();
                        }
                    }

                    _viewModel.UpdateDisplayPlan();
                    Refresh();
                    BuildSidebarContent();
                    UpdateParentStatusBar();
                };
                planToMenu.Items.Add(mi);
            }
            menu.Items.Add(planToMenu);
            menu.Items.Add(new Separator());

            var moveUp = new MenuItem { Header = "Move Up", IsEnabled = item.CanMoveUp };
            moveUp.Click += (_, _) => MoveItemUp(item);
            menu.Items.Add(moveUp);

            var moveDown = new MenuItem { Header = "Move Down", IsEnabled = item.CanMoveDown };
            moveDown.Click += (_, _) => MoveItemDown(item);
            menu.Items.Add(moveDown);

            var moveTop = new MenuItem { Header = "Move to Top" };
            moveTop.Click += (_, _) => OnMoveToTopItem(item);
            menu.Items.Add(moveTop);

            var priority = new MenuItem { Header = "Change Priority..." };
            priority.Click += (_, _) => OnChangePriorityItem(item);
            menu.Items.Add(priority);

            menu.Items.Add(new Separator());

            var copy = new MenuItem { Header = "Copy to Clipboard" };
            copy.Click += (_, _) => CopyPlanToClipboard();
            menu.Items.Add(copy);

            menu.Items.Add(new Separator());

            bool hasRemap = item.Entry.Remapping != null;
            // Count existing remap points in the plan and check against available remaps
            int existingRemapCount = _viewModel?.Plan?.Count(e => e.Remapping != null) ?? 0;
            int availableRemaps = 0;
            bool canRemapTimed = false;
            if (_viewModel?.Character is Character charForRemap)
            {
                availableRemaps = charForRemap.AvailableReMaps;
                canRemapTimed = charForRemap.LastReMapTimed == DateTime.MinValue
                    || DateTime.UtcNow >= charForRemap.LastReMapTimed.AddDays(365);
            }
            int totalRemapsAllowed = availableRemaps + (canRemapTimed ? 1 : 0);
            bool canInsertRemap = hasRemap || existingRemapCount < totalRemapsAllowed;

            var remapItem = new MenuItem
            {
                Header = hasRemap ? "Remove Remap Point" : "Insert Remap Point",
                IsEnabled = canInsertRemap,
            };
            if (!canInsertRemap)
                ToolTip.SetTip(remapItem, $"No remaps available ({existingRemapCount} used of {totalRemapsAllowed})");
            remapItem.Click += (_, _) =>
            {
                if (_viewModel?.Plan == null) return;
                // Set on the ORIGINAL plan entry, not the display copy
                var planEntry = _viewModel.Plan.GetEntry(item.Entry.Skill, item.Entry.Level);
                if (planEntry == null) return;

                if (hasRemap)
                    planEntry.Remapping = null!;
                else
                    planEntry.Remapping = new RemappingPoint();

                _viewModel.UpdateDisplayPlan();
                Refresh();
                BuildSidebarContent();
            };
            menu.Items.Add(remapItem);

            menu.Items.Add(new Separator());

            var remove = new MenuItem { Header = "Remove from Plan" };
            remove.Click += (_, _) => RemoveItem(item);
            menu.Items.Add(remove);

            return menu;
        }

        #endregion

        #region Timeline Bar

        private void BuildTimelineBar()
        {
            TimelineBar.Children.Clear();
            if (_segments.Count == 0) return;

            var totalTime = _segments.Sum(s => s.TrainingTime.TotalSeconds);
            if (totalTime <= 0) return;

            // Use the TimelineBar's actual width, fall back to parent
            double barWidth = TimelineBar.Bounds.Width;
            if (barWidth <= 0) barWidth = 800;

            foreach (var seg in _segments)
            {
                double fraction = seg.TrainingTime.TotalSeconds / totalTime;
                double width = Math.Max(2, fraction * barWidth);

                var segBorder = new Border
                {
                    Width = width,
                    Height = 6,
                    Background = PlanSectionHeader.GetAccentBrush(seg.PrimaryShort),
                };
                ToolTip.SetTip(segBorder, $"{seg.FocusLabel} - {FormatTime(seg.TrainingTime)}");
                TimelineBar.Children.Add(segBorder);
            }
        }

        #endregion

        #region Sidebar

        private void OnSidebarToggle(object? sender, RoutedEventArgs e)
        {
            _isSidebarExpanded = !_isSidebarExpanded;
            UpdateSidebarVisibility();
        }

        private void OnCollapsedSidebarClick(object? sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tab)
            {
                _activeSidebarTab = tab;
                PlanTab.IsChecked = tab == "Plan";
                AddTab.IsChecked = tab == "Add";

                _isSidebarExpanded = true;
                UpdateSidebarVisibility();
                BuildSidebarContent();
            }
        }

        private void UpdateSidebarVisibility()
        {
            SidebarExpanded.IsVisible = _isSidebarExpanded;
            SidebarCollapsed.IsVisible = !_isSidebarExpanded;
            SidebarBorder.Width = _isSidebarExpanded ? 280 : 36;
            SidebarToggle.IsChecked = _isSidebarExpanded;
        }

        private void OnSidebarTabClick(object? sender, RoutedEventArgs e)
        {
            if (sender == PlanTab)
                _activeSidebarTab = "Plan";
            else if (sender == AddTab)
                _activeSidebarTab = "Add";

            PlanTab.IsChecked = _activeSidebarTab == "Plan";
            AddTab.IsChecked = _activeSidebarTab == "Add";

            BuildSidebarContent();
        }

        private void BuildSidebarContent()
        {
            SidebarContent.Children.Clear();

            switch (_activeSidebarTab)
            {
                case "Plan":
                    BuildPlanPanel();
                    break;
                case "Add":
                    BuildAddPanel();
                    break;
            }
        }

        #region Plan Tab

        private void BuildPlanPanel()
        {
            if (_viewModel == null) return;

            var character = _viewModel.Character as Character;
            var stats = _viewModel.PlanStats;

            // ── Hero card: Training time ──
            var heroCard = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#10E6A817")),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 10),
                Margin = new Thickness(0, 0, 0, 6),
            };
            var heroStack = new StackPanel { Spacing = 2 };

            heroStack.Children.Add(new TextBlock
            {
                Text = FormatTime(stats.TrainingTime),
                FontSize = FontScaleService.Title,
                FontWeight = FontWeight.Bold,
                Foreground = GoldBrush,
            });

            int totalEntries = _viewModel.EntryCount;
            int trainedCount = 0;
            if (_currentEntryItems != null)
                trainedCount = _currentEntryItems.Count(e => e.Status == PlanEntryStatus.Trained);
            int remaining = totalEntries - trainedCount;

            heroStack.Children.Add(new TextBlock
            {
                Text = trainedCount > 0
                    ? $"{remaining} of {totalEntries} skills remaining"
                    : $"{totalEntries} skills to train",
                FontSize = FontScaleService.Body,
                Foreground = new SolidColorBrush(Color.Parse("#FFB0B0B0")),
            });

            // Finish date
            if (stats.TrainingTime > TimeSpan.Zero)
            {
                var finishDate = DateTime.UtcNow + stats.TrainingTime;
                heroStack.Children.Add(new TextBlock
                {
                    Text = $"Finishes {finishDate:MMM d, yyyy}",
                    FontSize = FontScaleService.Small,
                    Foreground = new SolidColorBrush(Color.Parse("#FF808080")),
                });
            }

            heroCard.Child = heroStack;
            SidebarContent.Children.Add(heroCard);

            // ── Cost line: SP · books · ISK ──
            int booksNeeded = stats.NotKnownSkillsCount;
            long booksCost = stats.NotKnownBooksCost;

            var costParts = new List<string>();
            costParts.Add(FormatSP(stats.TotalSkillPoints) + " SP");
            if (booksNeeded > 0)
            {
                costParts.Add($"{booksNeeded} books");
                costParts.Add(FormatISK(booksCost));
            }

            SidebarContent.Children.Add(new TextBlock
            {
                Text = string.Join(" \u00B7 ", costParts),
                FontSize = FontScaleService.Body,
                Foreground = new SolidColorBrush(Color.Parse("#FFB0B0B0")),
                Margin = new Thickness(0, 0, 0, 8),
            });

            // ── Optimize section ──
            BuildOptimizeSection(character);

            // ── Remap line ──
            if (character != null)
            {
                AddThinDivider("Remap");

                int bonusRemaps = character.AvailableReMaps;
                bool canRemapNow = bonusRemaps > 0
                    || character.LastReMapTimed == DateTime.MinValue
                    || DateTime.UtcNow >= character.LastReMapTimed.AddDays(365);

                var remapLine = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 6,
                };

                if (canRemapNow)
                {
                    remapLine.Children.Add(new TextBlock
                    {
                        Text = "\u2713 Available now",
                        FontSize = FontScaleService.Body,
                        Foreground = new SolidColorBrush(Color.Parse("#FF81C784")),
                    });
                }
                else
                {
                    int daysUntil = (int)(character.LastReMapTimed.AddDays(365) - DateTime.UtcNow).TotalDays;
                    remapLine.Children.Add(new TextBlock
                    {
                        Text = $"\u23F0 Next in {daysUntil}d",
                        FontSize = FontScaleService.Body,
                        Foreground = new SolidColorBrush(Color.Parse("#FFFFD54F")),
                    });
                }

                if (bonusRemaps > 0)
                {
                    remapLine.Children.Add(new TextBlock
                    {
                        Text = "\u00B7",
                        FontSize = FontScaleService.Body,
                        Foreground = new SolidColorBrush(Color.Parse("#FF606060")),
                    });
                    remapLine.Children.Add(new TextBlock
                    {
                        Text = $"{bonusRemaps} bonus",
                        FontSize = FontScaleService.Body,
                        Foreground = GoldBrush,
                    });
                }

                SidebarContent.Children.Add(remapLine);
            }
        }

        private void BuildOptimizeSection(Character? character)
        {
            // ── State: Calculating ──
            if (_optimizerVm?.IsCalculating == true)
            {
                SidebarContent.Children.Add(new TextBlock
                {
                    Text = "\u26A1 Analyzing\u2026",
                    FontSize = FontScaleService.Body,
                    Foreground = GoldBrush,
                    Margin = new Thickness(0, 4, 0, 0),
                });
                return;
            }

            // ── State: Has results ──
            if (_optimizerVm?.HasResults == true)
            {
                var savings = _optimizerVm.TimeSaved;
                bool hasSavings = savings > TimeSpan.Zero;

                var attrs = new[]
                {
                    EveAttribute.Intelligence, EveAttribute.Perception,
                    EveAttribute.Charisma, EveAttribute.Willpower, EveAttribute.Memory
                };

                // Compact result line: ⚡ 47d → 38d  ✓ -9d 4h
                var resultLine = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 6,
                    Margin = new Thickness(0, 2, 0, 0),
                };

                resultLine.Children.Add(new TextBlock
                {
                    Text = "\u26A1",
                    FontSize = FontScaleService.Subheading,
                    Foreground = GoldBrush,
                    VerticalAlignment = VerticalAlignment.Center,
                });

                resultLine.Children.Add(new TextBlock
                {
                    Text = FormatTimeCompact(_optimizerVm.CurrentDuration),
                    FontSize = FontScaleService.Subheading,
                    Foreground = new SolidColorBrush(Color.Parse("#FF909090")),
                    VerticalAlignment = VerticalAlignment.Center,
                });

                if (hasSavings || _optimizerVm.IsManuallyEdited)
                {
                    resultLine.Children.Add(new TextBlock
                    {
                        Text = "\u2192",
                        FontSize = FontScaleService.Body,
                        Foreground = new SolidColorBrush(Color.Parse("#FF606060")),
                        VerticalAlignment = VerticalAlignment.Center,
                    });

                    resultLine.Children.Add(new TextBlock
                    {
                        Text = FormatTimeCompact(_optimizerVm.OptimalDuration),
                        FontSize = FontScaleService.Subheading,
                        FontWeight = FontWeight.Bold,
                        Foreground = hasSavings
                            ? new SolidColorBrush(Color.Parse("#FF81C784"))
                            : new SolidColorBrush(Color.Parse("#FF909090")),
                        VerticalAlignment = VerticalAlignment.Center,
                    });

                    if (hasSavings)
                    {
                        resultLine.Children.Add(new TextBlock
                        {
                            Text = $"\u2713 -{FormatTimeCompact(savings)}",
                            FontSize = FontScaleService.Body,
                            FontWeight = FontWeight.SemiBold,
                            Foreground = new SolidColorBrush(Color.Parse("#FF81C784")),
                            VerticalAlignment = VerticalAlignment.Center,
                        });
                    }
                }
                else
                {
                    resultLine.Children.Add(new TextBlock
                    {
                        Text = "\u2713 Already optimal",
                        FontSize = FontScaleService.Body,
                        Foreground = GoldBrush,
                        VerticalAlignment = VerticalAlignment.Center,
                    });
                }

                SidebarContent.Children.Add(resultLine);

                if (hasSavings || _optimizerVm.IsManuallyEdited)
                {
                    // Current attributes line
                    var currentParts = new List<string>();
                    foreach (var attr in attrs)
                        currentParts.Add($"{GetAttributeShortName(attr)} {_optimizerVm.GetCurrent(attr)}");

                    SidebarContent.Children.Add(new TextBlock
                    {
                        Text = "Current: " + string.Join("  ", currentParts),
                        FontSize = FontScaleService.Small,
                        Foreground = new SolidColorBrush(Color.Parse("#FF707070")),
                        Margin = new Thickness(0, 6, 0, 0),
                    });

                    // Guidance text
                    SidebarContent.Children.Add(new TextBlock
                    {
                        Text = hasSavings
                            ? $"To save {FormatTimeCompact(savings)}, remap to:"
                            : "Change attributes to:",
                        FontSize = FontScaleService.Small,
                        Foreground = hasSavings
                            ? new SolidColorBrush(Color.Parse("#FF81C784"))
                            : new SolidColorBrush(Color.Parse("#FFB0B0B0")),
                        Margin = new Thickness(0, 4, 0, 2),
                    });

                    // Target attributes — show each with current→optimal for changed ones
                    foreach (var attr in attrs)
                    {
                        int current = _optimizerVm.GetCurrent(attr);
                        int optimal = _optimizerVm.GetOptimal(attr);
                        int delta = optimal - current;
                        bool changed = delta != 0;

                        var row = new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Spacing = 4,
                            Margin = new Thickness(4, 1, 0, 0),
                        };

                        row.Children.Add(new TextBlock
                        {
                            Text = GetAttributeShortName(attr),
                            FontSize = FontScaleService.Body,
                            FontWeight = FontWeight.SemiBold,
                            Foreground = GetAttributeBrush(attr),
                            Width = 30,
                        });

                        if (changed)
                        {
                            row.Children.Add(new TextBlock
                            {
                                Text = current.ToString(),
                                FontSize = FontScaleService.Body,
                                Foreground = new SolidColorBrush(Color.Parse("#FF707070")),
                                VerticalAlignment = VerticalAlignment.Center,
                            });
                            row.Children.Add(new TextBlock
                            {
                                Text = "\u2192",
                                FontSize = FontScaleService.Small,
                                Foreground = new SolidColorBrush(Color.Parse("#FF505050")),
                                VerticalAlignment = VerticalAlignment.Center,
                            });
                            row.Children.Add(new TextBlock
                            {
                                Text = optimal.ToString(),
                                FontSize = FontScaleService.Body,
                                FontWeight = FontWeight.Bold,
                                Foreground = new SolidColorBrush(Color.Parse("#FFF0F0F0")),
                                VerticalAlignment = VerticalAlignment.Center,
                            });

                            string sign = delta > 0 ? "+" : "";
                            row.Children.Add(new TextBlock
                            {
                                Text = $"({sign}{delta})",
                                FontSize = FontScaleService.Small,
                                Foreground = delta > 0
                                    ? new SolidColorBrush(Color.Parse("#FF81C784"))
                                    : new SolidColorBrush(Color.Parse("#FFCF6679")),
                                VerticalAlignment = VerticalAlignment.Center,
                            });
                        }
                        else
                        {
                            row.Children.Add(new TextBlock
                            {
                                Text = optimal.ToString(),
                                FontSize = FontScaleService.Body,
                                Foreground = new SolidColorBrush(Color.Parse("#FF505050")),
                                VerticalAlignment = VerticalAlignment.Center,
                            });
                        }

                        SidebarContent.Children.Add(row);
                    }

                    // Top improved skills (up to 3)
                    if (_optimizerVm.SkillImpacts.Count > 0)
                    {
                        var topSkills = _optimizerVm.SkillImpacts
                            .Where(s => s.TimeSaved > TimeSpan.Zero)
                            .Take(3)
                            .ToList();

                        if (topSkills.Count > 0)
                        {
                            SidebarContent.Children.Add(new TextBlock
                            {
                                Text = "Top improved:",
                                FontSize = FontScaleService.Small,
                                Foreground = new SolidColorBrush(Color.Parse("#FF707070")),
                                Margin = new Thickness(0, 4, 0, 2),
                            });

                            foreach (var skill in topSkills)
                            {
                                SidebarContent.Children.Add(new TextBlock
                                {
                                    Text = $"\u00B7 {skill.SkillName} {RomanNumeral(skill.Level)} \u2014 {FormatTimeCompact(skill.TimeSaved)} faster",
                                    FontSize = FontScaleService.Small,
                                    Foreground = new SolidColorBrush(Color.Parse("#FFB0B0B0")),
                                    TextTrimming = TextTrimming.CharacterEllipsis,
                                    Margin = new Thickness(4, 1, 0, 0),
                                });
                            }
                        }
                    }
                }

                // Re-analyze button
                var rerunBtn = new Button
                {
                    Content = "\u21BB Re-analyze",
                    FontSize = FontScaleService.Small,
                    Padding = new Thickness(8, 3),
                    CornerRadius = new CornerRadius(12),
                    Margin = new Thickness(0, 6, 0, 0),
                };
                rerunBtn.Click += OnRerunOptimization;
                SidebarContent.Children.Add(rerunBtn);

                // ── Advanced: manual attribute adjustment ──
                var advToggle = new Button
                {
                    Content = _showAdvanced
                        ? "Hide manual adjustment \u25B4"
                        : "Adjust manually \u25BE",
                    FontSize = FontScaleService.Small,
                    Padding = new Thickness(8, 3),
                    CornerRadius = new CornerRadius(12),
                    Background = Brushes.Transparent,
                    Foreground = new SolidColorBrush(Color.Parse("#FF808080")),
                    Margin = new Thickness(0, 4, 0, 0),
                };
                advToggle.Click += (_, _) =>
                {
                    _showAdvanced = !_showAdvanced;
                    BuildSidebarContent();
                };
                SidebarContent.Children.Add(advToggle);

                if (_showAdvanced)
                    BuildAdvancedAttributeSection();

                return;
            }

            // ── State: Error ──
            if (_optimizerVm != null && !string.IsNullOrEmpty(_optimizerVm.ErrorMessage))
            {
                SidebarContent.Children.Add(new TextBlock
                {
                    Text = _optimizerVm.ErrorMessage,
                    FontSize = FontScaleService.Small,
                    Foreground = new SolidColorBrush(Color.Parse("#FFCF6679")),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 4, 0, 4),
                });

                var retryBtn = new Button
                {
                    Content = "Try Again",
                    FontSize = FontScaleService.Small,
                    Padding = new Thickness(8, 3),
                    CornerRadius = new CornerRadius(12),
                };
                retryBtn.Click += OnRerunOptimization;
                SidebarContent.Children.Add(retryBtn);
                return;
            }

            // ── State: Not yet run ──
            var optimizeBtn = new Button
            {
                Content = "\u26A1 Optimize Plan",
                FontSize = FontScaleService.Body,
                Padding = new Thickness(10, 5),
                CornerRadius = new CornerRadius(12),
                Margin = new Thickness(0, 2, 0, 0),
            };
            optimizeBtn.Click += (_, _) => EnsureOptimizationRun();
            SidebarContent.Children.Add(optimizeBtn);
        }

        private void BuildAdvancedAttributeSection()
        {
            if (_optimizerVm == null) return;

            var attrs = new[]
            {
                EveAttribute.Intelligence, EveAttribute.Perception,
                EveAttribute.Charisma, EveAttribute.Willpower, EveAttribute.Memory
            };

            AddThinDivider("Manual adjustment");

            // Live training time display
            var durationLine = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6,
                Margin = new Thickness(0, 0, 0, 4),
            };

            if (_optimizerVm.IsManuallyEdited)
            {
                var manualTime = _optimizerVm.ManualDuration;
                var currentTime = _optimizerVm.CurrentDuration;
                var diff = currentTime - manualTime;

                durationLine.Children.Add(new TextBlock
                {
                    Text = FormatTimeCompact(manualTime),
                    FontSize = FontScaleService.Heading,
                    FontWeight = FontWeight.Bold,
                    Foreground = diff > TimeSpan.Zero
                        ? new SolidColorBrush(Color.Parse("#FF81C784"))
                        : diff < TimeSpan.Zero
                            ? new SolidColorBrush(Color.Parse("#FFCF6679"))
                            : new SolidColorBrush(Color.Parse("#FFF0F0F0")),
                });

                if (diff > TimeSpan.Zero)
                {
                    durationLine.Children.Add(new TextBlock
                    {
                        Text = $"(-{FormatTimeCompact(diff)})",
                        FontSize = FontScaleService.Body,
                        Foreground = new SolidColorBrush(Color.Parse("#FF81C784")),
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Margin = new Thickness(0, 0, 0, 1),
                    });
                }
                else if (diff < TimeSpan.Zero)
                {
                    durationLine.Children.Add(new TextBlock
                    {
                        Text = $"(+{FormatTimeCompact(diff.Negate())})",
                        FontSize = FontScaleService.Body,
                        Foreground = new SolidColorBrush(Color.Parse("#FFCF6679")),
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Margin = new Thickness(0, 0, 0, 1),
                    });
                }

                // vs current label
                durationLine.Children.Add(new TextBlock
                {
                    Text = $"vs {FormatTimeCompact(currentTime)}",
                    FontSize = FontScaleService.Small,
                    Foreground = new SolidColorBrush(Color.Parse("#FF606060")),
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(0, 0, 0, 1),
                });
            }
            else
            {
                durationLine.Children.Add(new TextBlock
                {
                    Text = "Drag to see time impact",
                    FontSize = FontScaleService.Small,
                    Foreground = new SolidColorBrush(Color.Parse("#FF606060")),
                });
            }

            SidebarContent.Children.Add(durationLine);

            // Unassigned points warning
            int unassigned = _optimizerVm.UnassignedPoints;
            if (unassigned > 0)
            {
                SidebarContent.Children.Add(new TextBlock
                {
                    Text = $"\u26A0 {unassigned} point{(unassigned != 1 ? "s" : "")} unassigned",
                    FontSize = FontScaleService.Small,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = new SolidColorBrush(Color.Parse("#FFFFD54F")),
                    Margin = new Thickness(0, 0, 0, 4),
                });
            }

            // Attribute rows: [−] NAME val [bar] [+]
            foreach (var attr in attrs)
            {
                int remappable = _optimizerVm.GetRemappable(attr);
                int basePoints = 17; // EveConstants.CharacterBaseAttributePoints
                int totalBase = basePoints + remappable;
                int implantBonus = _optimizerVm.GetImplantBonus(attr);
                int effective = totalBase + implantBonus;
                int optimalVal = _optimizerVm.GetOptimal(attr);
                bool isOptimal = totalBase == optimalVal;

                var row = new Grid
                {
                    ColumnDefinitions = ColumnDefinitions.Parse("Auto,30,Auto,*,Auto"),
                    Margin = new Thickness(0, 2),
                };

                // [-] button
                var decBtn = new Button
                {
                    Content = "\u2212",
                    FontSize = FontScaleService.Small,
                    Width = 20,
                    Height = 20,
                    Padding = new Thickness(0),
                    CornerRadius = new CornerRadius(10),
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    IsEnabled = _optimizerVm.CanDecrement(attr),
                };
                var capturedAttr = attr;
                decBtn.Click += (_, _) =>
                {
                    _optimizerVm?.AdjustAttribute(capturedAttr, -1);
                    BuildSidebarContent();
                };
                Grid.SetColumn(decBtn, 0);
                row.Children.Add(decBtn);

                // Attribute name + value
                var label = new TextBlock
                {
                    Text = $"{GetAttributeShortName(attr)} {effective}",
                    FontSize = FontScaleService.Body,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = isOptimal
                        ? GetAttributeBrush(attr)
                        : new SolidColorBrush(Color.Parse("#FFF0F0F0")),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                };
                if (implantBonus > 0)
                    ToolTip.SetTip(label, $"Base {totalBase} + {implantBonus} implant = {effective}");
                Grid.SetColumn(label, 1);
                row.Children.Add(label);

                // Spacer
                Grid.SetColumn(new Border { Width = 4 }, 2);
                row.Children.Add(new Border { Width = 4 });
                Grid.SetColumn(row.Children[^1], 2);

                // Colored bar
                double maxBarWidth = 140;
                double barFraction = remappable / 10.0;

                var barContainer = new Panel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 4, 0),
                };
                barContainer.Children.Add(new Border
                {
                    Width = maxBarWidth,
                    Height = 8,
                    CornerRadius = new CornerRadius(4),
                    Background = new SolidColorBrush(Color.Parse("#FF252535")),
                    HorizontalAlignment = HorizontalAlignment.Left,
                });
                barContainer.Children.Add(new Border
                {
                    Width = Math.Max(0, barFraction * maxBarWidth),
                    Height = 8,
                    CornerRadius = new CornerRadius(4),
                    Background = GetAttributeBrush(attr),
                    HorizontalAlignment = HorizontalAlignment.Left,
                });
                Grid.SetColumn(barContainer, 3);
                row.Children.Add(barContainer);

                // [+] button
                var incBtn = new Button
                {
                    Content = "+",
                    FontSize = FontScaleService.Small,
                    Width = 20,
                    Height = 20,
                    Padding = new Thickness(0),
                    CornerRadius = new CornerRadius(10),
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    IsEnabled = _optimizerVm.CanIncrement(attr),
                };
                incBtn.Click += (_, _) =>
                {
                    _optimizerVm?.AdjustAttribute(capturedAttr, +1);
                    BuildSidebarContent();
                };
                Grid.SetColumn(incBtn, 4);
                row.Children.Add(incBtn);

                SidebarContent.Children.Add(row);
            }

            // Reset buttons
            var resetRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6,
                Margin = new Thickness(0, 6, 0, 0),
            };

            var resetOptBtn = new Button
            {
                Content = "Reset to optimal",
                FontSize = FontScaleService.Caption,
                Padding = new Thickness(6, 2),
                CornerRadius = new CornerRadius(10),
            };
            resetOptBtn.Click += (_, _) =>
            {
                _optimizerVm?.ResetToOptimal();
                BuildSidebarContent();
            };
            resetRow.Children.Add(resetOptBtn);

            var resetCurBtn = new Button
            {
                Content = "Reset to current",
                FontSize = FontScaleService.Caption,
                Padding = new Thickness(6, 2),
                CornerRadius = new CornerRadius(10),
            };
            resetCurBtn.Click += (_, _) =>
            {
                _optimizerVm?.ResetToCurrent();
                BuildSidebarContent();
            };
            resetRow.Children.Add(resetCurBtn);

            SidebarContent.Children.Add(resetRow);
        }

        private void AddThinDivider(string label, string? description = null)
        {
            var dividerPanel = new DockPanel { Margin = new Thickness(0, 8, 0, 4) };

            dividerPanel.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = FontScaleService.Small,
                Foreground = new SolidColorBrush(Color.Parse("#FF707070")),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0),
            });
            DockPanel.SetDock(dividerPanel.Children[0], Dock.Left);

            dividerPanel.Children.Add(new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.Parse("#FF404050")),
                VerticalAlignment = VerticalAlignment.Center,
            });

            SidebarContent.Children.Add(dividerPanel);

            if (!string.IsNullOrEmpty(description))
            {
                SidebarContent.Children.Add(new TextBlock
                {
                    Text = description,
                    FontSize = FontScaleService.Caption,
                    Foreground = new SolidColorBrush(Color.Parse("#FF585868")),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 2),
                });
            }
        }

        private static string FormatTimeCompact(TimeSpan time)
        {
            if (time.TotalDays >= 1) return $"{(int)time.TotalDays}d {time.Hours}h";
            if (time.TotalHours >= 1) return $"{(int)time.TotalHours}h {time.Minutes}m";
            return $"{(int)time.TotalMinutes}m";
        }

        private void OnRerunOptimization(object? sender, RoutedEventArgs e)
        {
            if (_optimizerVm != null && _viewModel != null)
            {
                _optimizerVm.ClearResults();

                var plan = _viewModel.Plan;
                var character = _viewModel.Character;
                if (plan != null && character != null)
                {
                    // Auto-detect: if plan has manual remap points, use RemappingPoints strategy
                    var entries = plan.ToArray();
                    bool hasRemapPoints = entries.Any(e2 => e2.Remapping != null);
                    var strategy = hasRemapPoints
                        ? AttributeOptimizationStrategy.RemappingPoints
                        : AttributeOptimizationStrategy.OneYearPlan;
                    _optimizerVm.RunOptimization(plan, character, strategy);
                }
            }
            BuildSidebarContent();
        }

        #endregion

        #region Add Tab

        private void BuildAddPanel()
        {
            SidebarContent.Children.Add(new TextBlock
            {
                Text = "Browse and add skills to your training plan.",
                FontSize = FontScaleService.Small,
                Foreground = new SolidColorBrush(Color.Parse("#FF707070")),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 6),
            });

            SidebarContent.Children.Add(new TextBlock
            {
                Text = "Use the Skills, Ships, Items, or Blueprints tabs to add skills to your plan.",
                FontSize = FontScaleService.Small,
                Foreground = new SolidColorBrush(Color.Parse("#FF909090")),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8),
            });

            var addBtn = new Button
            {
                Content = "+ Add Skills",
                FontSize = FontScaleService.Body,
                Padding = new Thickness(10, 4),
                CornerRadius = new CornerRadius(12),
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            addBtn.Click += OnAddSkills;
            SidebarContent.Children.Add(addBtn);
        }

        #endregion

        #endregion

        #region Stats

        private void UpdateStatsHeader()
        {
            if (_viewModel == null) return;

            var stats = _viewModel.PlanStats;
            SkillCountText.Text = $"{_viewModel.EntryCount} skills \u00B7 {stats.UniqueSkillsCount} unique";
            TrainingTimeText.Text = FormatTime(stats.TrainingTime);
            TotalSpText.Text = FormatSP(stats.TotalSkillPoints);

            if (stats.TrainingTime > TimeSpan.Zero)
            {
                var finishDate = DateTime.UtcNow + stats.TrainingTime;
                FinishDateText.Text = $"Finishes {finishDate:yyyy-MM-dd}";
            }
            else
            {
                FinishDateText.Text = string.Empty;
            }

            Character? character = _viewModel.Character;
            long characterSP = (character as Character)?.SkillPoints ?? 0;
            long missingSP = stats.TotalSkillPoints;
            if (missingSP > 0 && characterSP > 0)
            {
                int spPerInjector = GetSpPerInjector(characterSP);
                int injectorCount = (int)Math.Ceiling((double)missingSP / spPerInjector);
                double costBillions = injectorCount * 0.9;
                InjectorText.Text = $"~{injectorCount} injectors (~{costBillions:F1}B ISK)";
            }
            else
            {
                InjectorText.Text = string.Empty;
            }
        }

        private static int GetSpPerInjector(long characterSP) => characterSP switch
        {
            < 5_000_000 => 500_000,
            < 50_000_000 => 400_000,
            < 80_000_000 => 300_000,
            _ => 150_000
        };

        private static string FormatSP(long sp)
        {
            if (sp >= 1_000_000) return $"{sp / 1_000_000.0:F1}M SP";
            if (sp >= 1_000) return $"{sp / 1_000.0:F0}K SP";
            return $"{sp:N0} SP";
        }

        private static string FormatISK(long isk)
        {
            if (isk >= 1_000_000_000) return $"{isk / 1_000_000_000.0:F1}B ISK";
            if (isk >= 1_000_000) return $"{isk / 1_000_000.0:F1}M ISK";
            if (isk >= 1_000) return $"{isk / 1_000.0:F0}K ISK";
            return $"{isk:N0} ISK";
        }

        #endregion

        #region Status Helpers

        private static PlanEntryStatus DetermineStatus(PlanEntry entry, Character? character)
        {
            if (character == null)
                return PlanEntryStatus.Missing;

            Skill? charSkill = entry.CharacterSkill;
            if (charSkill == null)
                return PlanEntryStatus.Missing;

            if (charSkill.IsTraining && charSkill.Level + 1 == entry.Level)
                return PlanEntryStatus.Training;

            if (charSkill.Level >= entry.Level)
                return PlanEntryStatus.Trained;

            return PlanEntryStatus.Missing;
        }

        #endregion

        #region Add Skills

        private void OnAddSkills(object? sender, RoutedEventArgs e)
        {
            if (this.VisualRoot is PlanEditorWindow window)
            {
                window.SwitchToSkillBrowser();
            }
        }

        #endregion

        #region Optimizer

        /// <summary>
        /// Ensures the optimizer has been created and a run has started.
        /// Safe to call multiple times — won't re-run if results are already available
        /// or a calculation is in progress.
        /// </summary>
        private void EnsureOptimizationRun()
        {
            if (_viewModel == null) return;

            if (_optimizerVm == null)
            {
                _optimizerVm = new PlanOptimizerViewModel();
                _optimizerVm.PropertyChanged += OnOptimizerPropertyChanged;
            }

            // Don't re-run if already calculating or has fresh results
            if (_optimizerVm.IsCalculating || _optimizerVm.HasResults)
                return;

            _optimizerVm.ErrorMessage = "";

            var plan = _viewModel.Plan;
            var character = _viewModel.Character;
            if (plan != null && character != null)
            {
                // Auto-detect: if plan has manual remap points, use RemappingPoints strategy
                var entries = plan.ToArray();
                bool hasRemapPoints = entries.Any(e => e.Remapping != null);
                var strategy = hasRemapPoints
                    ? AttributeOptimizationStrategy.RemappingPoints
                    : AttributeOptimizationStrategy.OneYearPlan;
                _optimizerVm.RunOptimization(plan, character, strategy);
            }
        }

        private void OnOptimizerPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(PlanOptimizerViewModel.IsCalculating)
                or nameof(PlanOptimizerViewModel.HasResults)
                or nameof(PlanOptimizerViewModel.ErrorMessage))
            {
                global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    // Refresh skill list so remap dividers update from "not computed" to computed
                    if (e.PropertyName is nameof(PlanOptimizerViewModel.HasResults))
                        Refresh();

                    if (_activeSidebarTab is "Plan")
                        BuildSidebarContent();
                });
            }
        }

        #endregion

        #region Copy

        private void OnCopyClick(object? sender, RoutedEventArgs e)
        {
            CopyPlanToClipboard();
        }

        private void OnGroupByAttribute(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;
            _viewModel.ToggleSortColumn(PlanEntrySort.PrimaryAttribute);
            Refresh();
        }

        private void CopyPlanToClipboard()
        {
            if (_viewModel == null) return;

            if (_currentEntryItems == null || _currentEntryItems.Count == 0)
                return;

            var entries = _currentEntryItems.Select(d => d.Entry).ToList();

            // Build clean skill plan text (EVE-friendly format)
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Skill Plan: {_viewModel.Plan?.Name ?? "Untitled"}");
            sb.AppendLine();

            foreach (var entry in entries)
            {
                string levelStr = RomanNumeral((int)entry.Level);
                sb.AppendLine($"{entry.Skill.Name} {levelStr}");
            }

            sb.AppendLine();
            sb.AppendLine($"Total: {entries.Count} skills, {FormatTime(_viewModel.PlanStats.TrainingTime)}");

            string text = sb.ToString();
            AppServices.ClipboardService?.SetText(text);
        }

        #endregion

        #region Context Menu Actions

        private void OnMoveToTopItem(PlanEntryDisplayItem item)
        {
            if (_viewModel?.Plan == null) return;

            var plan = _viewModel.Plan;
            var entry = plan.FirstOrDefault(pe =>
                pe.Skill == item.Entry.Skill && pe.Level == item.Entry.Level);
            if (entry != null)
            {
                entry.Priority = 1;
                _viewModel.UpdateDisplayPlan();
                Refresh();
                BuildSidebarContent();
                UpdateParentStatusBar();
            }
        }

        private void OnChangePriorityItem(PlanEntryDisplayItem item)
        {
            if (_viewModel?.Plan == null) return;

            int newPriority = (item.Entry.Priority % 5) + 1;

            var plan = _viewModel.Plan;
            var entry = plan.FirstOrDefault(pe =>
                pe.Skill == item.Entry.Skill && pe.Level == item.Entry.Level);
            if (entry != null)
            {
                entry.Priority = newPriority;
                _viewModel.UpdateDisplayPlan();
                Refresh();
            }
        }

        private void RemoveItem(PlanEntryDisplayItem item)
        {
            if (_viewModel?.Plan == null) return;

            var plan = _viewModel.Plan;
            var planEntry = plan.GetEntry(item.Entry.Skill, item.Entry.Level);
            if (planEntry != null)
            {
                var op = plan.TryRemoveSet(new[] { planEntry });
                op.Perform();
            }

            _viewModel.UpdateDisplayPlan();
            Refresh();
            BuildSidebarContent();
            UpdateParentStatusBar();
        }

        #endregion

        #region Move Operations

        private int FindEntryIndexInDisplayPlan(PlanEntryDisplayItem item)
        {
            if (_viewModel?.DisplayPlan == null) return -1;

            var displayEntries = _viewModel.DisplayPlan.ToArray();
            for (int i = 0; i < displayEntries.Length; i++)
            {
                if (displayEntries[i] == item.Entry)
                    return i;
            }
            return -1;
        }

        private void OnMoveItemUp(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not PlanEntryDisplayItem item) return;
            MoveItemUp(item);
            e.Handled = true;
        }

        private void OnMoveItemDown(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not PlanEntryDisplayItem item) return;
            MoveItemDown(item);
            e.Handled = true;
        }

        private void MoveItemUp(PlanEntryDisplayItem item)
        {
            if (_viewModel == null) return;
            int index = FindEntryIndexInDisplayPlan(item);
            if (index <= 0) return;

            _recentlyMovedKeys = new HashSet<string> { $"{item.Entry.Skill.ID}_{item.Entry.Level}" };
            _viewModel.MoveSelectedUp(new List<int> { index });
            _viewModel.UpdateDisplayPlan();
            Refresh();
            UpdateParentStatusBar();
        }

        private void MoveItemDown(PlanEntryDisplayItem item)
        {
            if (_viewModel == null) return;
            int index = FindEntryIndexInDisplayPlan(item);
            if (index < 0) return;

            var displayEntries = _viewModel.DisplayPlan!.ToArray();
            if (index >= displayEntries.Length - 1) return;

            _recentlyMovedKeys = new HashSet<string> { $"{item.Entry.Skill.ID}_{item.Entry.Level}" };
            _viewModel.MoveSelectedDown(new List<int> { index });
            _viewModel.UpdateDisplayPlan();
            Refresh();
            UpdateParentStatusBar();
        }

        internal void MoveSelectionUp()
        {
            // For keyboard shortcut — move the first visible entry item
            if (_currentEntryItems == null || _currentEntryItems.Count == 0) return;
            // Simple: move first item that can move
            // In the future, track selection state properly
            var item = _currentEntryItems.FirstOrDefault(e => e.CanMoveUp);
            if (item != null) MoveItemUp(item);
        }

        internal void MoveSelectionDown()
        {
            if (_currentEntryItems == null || _currentEntryItems.Count == 0) return;
            var item = _currentEntryItems.LastOrDefault(e => e.CanMoveDown);
            if (item != null) MoveItemDown(item);
        }

        internal void DeleteSelected()
        {
            // Delete first visible entry (keyboard shortcut)
            // Full multi-select would need selection tracking on ItemsControl
            if (_currentEntryItems == null || _currentEntryItems.Count == 0) return;
            var item = _currentEntryItems.FirstOrDefault();
            if (item != null) RemoveItem(item);
        }

        #endregion

        #region Helpers

        private void UpdateParentStatusBar()
        {
            if (this.VisualRoot is PlanEditorWindow window)
                window.UpdateStatusBar();
        }

        private static string FormatTime(TimeSpan time)
        {
            if (time <= TimeSpan.Zero) return "Done";
            if (time.TotalDays >= 1) return $"{(int)time.TotalDays}d {time.Hours}h {time.Minutes}m";
            if (time.TotalHours >= 1) return $"{(int)time.TotalHours}h {time.Minutes}m";
            return $"{(int)time.TotalMinutes}m";
        }

        private static string RomanNumeral(int level) => level switch
        {
            1 => "I",
            2 => "II",
            3 => "III",
            4 => "IV",
            5 => "V",
            _ => level.ToString()
        };

        #endregion

        #region Segment Info

        private sealed class SegmentInfo
        {
            public int Index { get; init; }
            public string FocusLabel { get; init; } = "";
            public string PrimaryShort { get; init; } = "";
            public string SecondaryShort { get; init; } = "";
            public int SkillCount { get; init; }
            public TimeSpan TrainingTime { get; init; }
            public double AvgSpPerHour { get; init; }
            public EveAttribute DominantPrimary { get; init; }
            public EveAttribute DominantSecondary { get; init; }
            public PlanEntry? RemapEntry { get; init; }
        }

        #endregion
    }
}
