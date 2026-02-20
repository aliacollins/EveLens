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
using EVEMon.Common.Enumerations;
using EVEMon.Common.Helpers;
using EVEMon.Common.Models;
using EVEMon.Common.Services;
using EVEMon.Common.SettingsObjects;
using EVEMon.Common.ViewModels;
using EVEMon.Avalonia.ViewModels;
using EVEMon.SkillPlanner;

namespace EVEMon.Avalonia.Views.PlanEditor
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
        private string _activeSidebarTab = "Summary";
        private bool _isSidebarExpanded = true;
        private bool _showFineTune;



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
                        });
                    }
                    else
                    {
                        displayItems.Add(new PlanRemapDivider
                        {
                            AttributeSummary = "Not optimized \u2014 click Optimize",
                            IsComputed = false,
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

                var itemsRef = displayItems;
                global::Avalonia.Threading.DispatcherTimer.RunOnce(() =>
                {
                    if (itemsRef != null)
                    {
                        foreach (var item in itemsRef.OfType<PlanEntryDisplayItem>())
                            item.IsRecentlyMoved = false;
                        RebuildItemsControlFromDisplayItems(itemsRef);
                    }
                }, TimeSpan.FromSeconds(1.5));
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

                if (entryItems.Any(i => i.IsNewlyAdded))
                {
                    var itemsRef = displayItems;
                    global::Avalonia.Threading.DispatcherTimer.RunOnce(() =>
                    {
                        if (itemsRef != null)
                        {
                            foreach (var item in itemsRef.OfType<PlanEntryDisplayItem>())
                                item.IsNewlyAdded = false;
                            RebuildItemsControlFromDisplayItems(itemsRef);
                        }
                    }, TimeSpan.FromSeconds(2));
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

            if (!divider.IsComputed)
            {
                // Uncomputed: compact one-liner
                border.Child = new TextBlock
                {
                    Text = "\u25C7 Remap point \u2014 click Optimize to calculate",
                    FontSize = 9,
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
                FontSize = 9,
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
                        FontSize = 9,
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
                    FontSize = 9,
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
                    FontSize = 9,
                    Foreground = new SolidColorBrush(Color.Parse("#FF606060")),
                    VerticalAlignment = VerticalAlignment.Center,
                });
                line.Children.Add(new TextBlock
                {
                    Text = divider.TimeSavingsText,
                    FontSize = 9,
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
                    FontSize = 9,
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
                FontSize = 11,
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
                FontSize = 10,
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
                FontSize = 10,
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

            var border = new Border
            {
                Padding = new Thickness(0, 3),
                Background = item.RowBackground,
            };

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
                        FontSize = 9,
                    }
                };
                ToolTip.SetTip(bookBadge, $"Skillbook not owned \u2014 {item.Entry.Skill.FormattedCost}");
                nameStack.Children.Add(bookBadge);
            }

            // Omega indicator
            if (isOmega)
            {
                nameStack.Children.Add(new Border
                {
                    Background = new SolidColorBrush(Color.Parse("#20FFD54F")),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(3, 0),
                    Margin = new Thickness(0, 0, 4, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = new TextBlock
                    {
                        Text = "\u03A9",
                        FontSize = 10,
                        FontWeight = FontWeight.Bold,
                        Foreground = new SolidColorBrush(Color.Parse("#FFFFD54F")),
                    }
                });
            }

            nameStack.Children.Add(new TextBlock
            {
                Text = item.SkillName,
                FontSize = 11,
                FontWeight = FontWeight.SemiBold,
                Foreground = isOmega
                    ? new SolidColorBrush(Color.Parse("#FFFFD54F"))
                    : new SolidColorBrush(Color.Parse("#FFF0F0F0")),
                TextTrimming = TextTrimming.CharacterEllipsis,
            });

            if (item.CanMoveUp)
            {
                var upBtn = new Button
                {
                    Content = "\u25B2",
                    FontSize = 7,
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
                    FontSize = 7,
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
                FontSize = 10,
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
                FontSize = 10,
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
                    FontSize = 9,
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
            for (int lvl = 1; lvl <= 5; lvl++)
            {
                var mi = new MenuItem { Header = $"Level {RomanNumeral(lvl)}", Tag = lvl.ToString() };
                mi.Click += OnPlanToLevel;
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
            var remapItem = new MenuItem
            {
                Header = hasRemap ? "Remove Remap Point" : "Insert Remap Point"
            };
            remapItem.Click += (_, _) =>
            {
                if (hasRemap)
                    item.Entry.Remapping = null!;
                else
                    item.Entry.Remapping = new RemappingPoint();
                _viewModel?.UpdateDisplayPlan();
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
                SummaryTab.IsChecked = tab == "Summary";
                OptimizeTab.IsChecked = tab == "Optimize";
                AddTab.IsChecked = tab == "Add";

                if (tab == "Optimize")
                    EnsureOptimizationRun();

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
            if (sender == SummaryTab)
                _activeSidebarTab = "Summary";
            else if (sender == OptimizeTab)
                _activeSidebarTab = "Optimize";
            else if (sender == AddTab)
                _activeSidebarTab = "Add";

            SummaryTab.IsChecked = _activeSidebarTab == "Summary";
            OptimizeTab.IsChecked = _activeSidebarTab == "Optimize";
            AddTab.IsChecked = _activeSidebarTab == "Add";

            // Auto-run optimization when switching to Optimize tab
            if (_activeSidebarTab == "Optimize")
            {
                EnsureOptimizationRun();
            }

            BuildSidebarContent();
        }

        private void BuildSidebarContent()
        {
            SidebarContent.Children.Clear();

            switch (_activeSidebarTab)
            {
                case "Summary":
                    BuildSummaryPanel();
                    break;
                case "Optimize":
                    BuildOptimizePanel();
                    break;
                case "Add":
                    BuildAddPanel();
                    break;
            }
        }

        #region Summary Tab

        private void BuildSummaryPanel()
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
                FontSize = 22,
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
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.Parse("#FFB0B0B0")),
            });

            // Finish date
            if (stats.TrainingTime > TimeSpan.Zero)
            {
                var finishDate = DateTime.UtcNow + stats.TrainingTime;
                heroStack.Children.Add(new TextBlock
                {
                    Text = $"Finishes {finishDate:MMM d, yyyy}",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.Parse("#FF808080")),
                });
            }

            heroCard.Child = heroStack;
            SidebarContent.Children.Add(heroCard);

            // ── Quick stats row: SP · Books · Injectors ──
            var statsGrid = new Grid
            {
                ColumnDefinitions = ColumnDefinitions.Parse("*,*"),
                RowDefinitions = RowDefinitions.Parse("Auto,Auto"),
                Margin = new Thickness(0, 0, 0, 4),
            };

            // SP needed
            AddStatCell(statsGrid, 0, 0, FormatSP(stats.TotalSkillPoints), "total SP");

            // Skillbooks
            int booksNeeded = stats.NotKnownSkillsCount;
            long booksCost = stats.NotKnownBooksCost;
            AddStatCell(statsGrid, 0, 1,
                booksNeeded > 0 ? $"{booksNeeded} books" : "All owned",
                booksNeeded > 0 ? FormatISK(booksCost) : "");

            // Unique skills
            AddStatCell(statsGrid, 1, 0, $"{stats.UniqueSkillsCount} unique", "skills");

            // Injector estimate
            long characterSP = character?.SkillPoints ?? 0;
            long missingSP = stats.TotalSkillPoints;
            if (missingSP > 0 && characterSP > 0)
            {
                int spPerInjector = GetSpPerInjector(characterSP);
                int injectorCount = (int)Math.Ceiling((double)missingSP / spPerInjector);
                AddStatCell(statsGrid, 1, 1, $"~{injectorCount} injectors", "to skip");
            }
            else
            {
                AddStatCell(statsGrid, 1, 1, "", "");
            }

            SidebarContent.Children.Add(statsGrid);

            // ── Attributes ──
            if (character != null)
            {
                AddThinDivider("Attributes");

                var allAttrs = new[]
                {
                    EveAttribute.Intelligence, EveAttribute.Perception,
                    EveAttribute.Charisma, EveAttribute.Willpower, EveAttribute.Memory
                };

                foreach (var attr in allAttrs)
                {
                    long effective = character[attr].EffectiveValue;
                    long implantBonus = character[attr].ImplantBonus;
                    double fraction = Math.Min(1.0, effective / 27.0);
                    double barMaxWidth = 150;

                    var row = new Grid
                    {
                        ColumnDefinitions = ColumnDefinitions.Parse("32,*,Auto"),
                        Margin = new Thickness(0, 2),
                    };

                    // Attribute name
                    var nameTb = new TextBlock
                    {
                        Text = GetAttributeShortName(attr),
                        FontSize = 11,
                        FontWeight = FontWeight.SemiBold,
                        Foreground = GetAttributeBrush(attr),
                        VerticalAlignment = VerticalAlignment.Center,
                    };
                    Grid.SetColumn(nameTb, 0);
                    row.Children.Add(nameTb);

                    // Visual bar
                    var barContainer = new Panel
                    {
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 6, 0),
                    };
                    barContainer.Children.Add(new Border
                    {
                        Width = barMaxWidth,
                        Height = 8,
                        CornerRadius = new CornerRadius(4),
                        Background = new SolidColorBrush(Color.Parse("#FF252535")),
                        HorizontalAlignment = HorizontalAlignment.Left,
                    });
                    barContainer.Children.Add(new Border
                    {
                        Width = Math.Max(2, fraction * barMaxWidth),
                        Height = 8,
                        CornerRadius = new CornerRadius(4),
                        Background = GetAttributeBrush(attr),
                        HorizontalAlignment = HorizontalAlignment.Left,
                    });
                    Grid.SetColumn(barContainer, 1);
                    row.Children.Add(barContainer);

                    // Value + implant tag
                    var valPanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 4,
                        VerticalAlignment = VerticalAlignment.Center,
                    };
                    valPanel.Children.Add(new TextBlock
                    {
                        Text = effective.ToString(),
                        FontSize = 11,
                        FontWeight = FontWeight.SemiBold,
                        Foreground = new SolidColorBrush(Color.Parse("#FFF0F0F0")),
                    });
                    if (implantBonus > 0)
                    {
                        valPanel.Children.Add(new TextBlock
                        {
                            Text = $"+{implantBonus} impl",
                            FontSize = 10,
                            Foreground = new SolidColorBrush(Color.Parse("#FF606060")),
                        });
                    }
                    Grid.SetColumn(valPanel, 2);
                    row.Children.Add(valPanel);

                    SidebarContent.Children.Add(row);
                }

                // ── Implants (compact pills) ──
                AddThinDivider("Implants");
                var implantFlow = new WrapPanel { Margin = new Thickness(0, 0, 0, 2) };
                int emptySlots = 0;

                foreach (var attr in allAttrs)
                {
                    long bonus = character[attr].ImplantBonus;
                    if (bonus > 0)
                    {
                        implantFlow.Children.Add(new Border
                        {
                            Background = new SolidColorBrush(Color.Parse("#20FFFFFF")),
                            CornerRadius = new CornerRadius(8),
                            Padding = new Thickness(6, 2),
                            Margin = new Thickness(0, 0, 4, 3),
                            Child = new TextBlock
                            {
                                Text = $"+{bonus} {GetAttributeShortName(attr)}",
                                FontSize = 10,
                                FontWeight = FontWeight.SemiBold,
                                Foreground = GetAttributeBrush(attr),
                            }
                        });
                    }
                    else
                    {
                        emptySlots++;
                    }
                }

                if (emptySlots > 0)
                {
                    implantFlow.Children.Add(new TextBlock
                    {
                        Text = $"{emptySlots} empty",
                        FontSize = 10,
                        Foreground = new SolidColorBrush(Color.Parse("#FF606060")),
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(2, 0, 0, 0),
                    });
                }

                if (implantFlow.Children.Count == 0)
                {
                    implantFlow.Children.Add(new TextBlock
                    {
                        Text = "No implants",
                        FontSize = 10,
                        Foreground = new SolidColorBrush(Color.Parse("#FF606060")),
                    });
                }
                SidebarContent.Children.Add(implantFlow);

                // ── Remap (one line) ──
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
                        FontSize = 11,
                        Foreground = new SolidColorBrush(Color.Parse("#FF81C784")),
                    });
                }
                else
                {
                    int daysUntil = (int)(character.LastReMapTimed.AddDays(365) - DateTime.UtcNow).TotalDays;
                    remapLine.Children.Add(new TextBlock
                    {
                        Text = $"\u23F0 Next in {daysUntil}d",
                        FontSize = 11,
                        Foreground = new SolidColorBrush(Color.Parse("#FFFFD54F")),
                    });
                }

                if (bonusRemaps > 0)
                {
                    remapLine.Children.Add(new TextBlock
                    {
                        Text = "\u00B7",
                        FontSize = 11,
                        Foreground = new SolidColorBrush(Color.Parse("#FF606060")),
                    });
                    remapLine.Children.Add(new TextBlock
                    {
                        Text = $"{bonusRemaps} bonus",
                        FontSize = 11,
                        Foreground = GoldBrush,
                    });
                }

                SidebarContent.Children.Add(remapLine);
            }

            // ── Plan attribute spread ──
            AddThinDivider("Plan spread");
            BuildPlanMixBars();

            // ── Savings teaser ──
            if (_optimizerVm?.HasResults == true)
            {
                var savings = _optimizerVm.TimeSaved;
                if (savings > TimeSpan.Zero)
                {
                    var savingsCard = new Border
                    {
                        Background = new SolidColorBrush(Color.Parse("#1581C784")),
                        CornerRadius = new CornerRadius(6),
                        Padding = new Thickness(10, 6),
                        Margin = new Thickness(0, 6, 0, 0),
                        Cursor = new Cursor(StandardCursorType.Hand),
                    };
                    var savingsStack = new StackPanel { Spacing = 2 };
                    savingsStack.Children.Add(new TextBlock
                    {
                        Text = $"\u26A1 You could save {FormatTime(savings)}",
                        FontSize = 12,
                        FontWeight = FontWeight.SemiBold,
                        Foreground = new SolidColorBrush(Color.Parse("#FF81C784")),
                    });
                    savingsStack.Children.Add(new TextBlock
                    {
                        Text = "Switch to Optimize tab for details \u203A",
                        FontSize = 10,
                        Foreground = new SolidColorBrush(Color.Parse("#FF707070")),
                    });
                    savingsCard.Child = savingsStack;
                    savingsCard.PointerPressed += (_, _) =>
                    {
                        _activeSidebarTab = "Optimize";
                        SummaryTab.IsChecked = false;
                        OptimizeTab.IsChecked = true;
                        AddTab.IsChecked = false;
                        EnsureOptimizationRun();
                        BuildSidebarContent();
                    };
                    SidebarContent.Children.Add(savingsCard);
                }
            }
            else
            {
                // Optimizer hasn't run yet — show hint
                var hintTb = new TextBlock
                {
                    Text = "\u26A1 Click Optimize to see potential savings",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.Parse("#FF606060")),
                    Margin = new Thickness(0, 6, 0, 0),
                };
                SidebarContent.Children.Add(hintTb);
            }

            // ── Clone Training Times ──
            BuildCloneComparisonSection();

            // ── Owned Skillbooks ──
            BuildOwnedSkillbooksSection();
        }

        private void BuildCloneComparisonSection()
        {
            try
            {
                var character = _viewModel?.Character as Character;
                var plan = _viewModel?.DisplayPlan;
                if (character == null || plan == null) return;

                AddThinDivider("Clone Training Times");

                foreach (var implantSet in character.ImplantSets)
                {
                    try
                    {
                        var scratchpad = character.After(implantSet);
                        var time = plan.GetTotalTime(scratchpad, true);
                        string bonusText = GetImplantBonusText(implantSet);

                        SidebarContent.Children.Add(new TextBlock
                        {
                            Text = $"{implantSet.Name} {bonusText}: {FormatTime(time)}",
                            FontSize = 10,
                            Foreground = new SolidColorBrush(Color.Parse("#FFB0B0B0")),
                            Margin = new Thickness(4, 1, 0, 0),
                        });
                    }
                    catch { /* Skip sets that fail computation */ }
                }

                // No Implants time
                try
                {
                    var noneSet = character.ImplantSets.None;
                    var noneScratchpad = character.After(noneSet);
                    var noneTime = plan.GetTotalTime(noneScratchpad, true);
                    SidebarContent.Children.Add(new TextBlock
                    {
                        Text = $"No Implants: {FormatTime(noneTime)}",
                        FontSize = 10,
                        Foreground = new SolidColorBrush(Color.Parse("#FF707070")),
                        Margin = new Thickness(4, 1, 0, 0),
                    });
                }
                catch { }

                // Edit Sets button
                var editBtn = new Button
                {
                    Content = "Edit Sets...",
                    FontSize = 9,
                    Padding = new Thickness(8, 3),
                    CornerRadius = new CornerRadius(10),
                    Margin = new Thickness(0, 4, 0, 0),
                };
                editBtn.Click += async (_, _) =>
                {
                    try
                    {
                        var editor = new EVEMon.Avalonia.Views.Dialogs.ImplantSetEditorWindow();
                        editor.Initialize(character);
                        var parentWindow = this.FindAncestorOfType<Window>();
                        if (parentWindow != null)
                            await editor.ShowDialog(parentWindow);
                        BuildSidebarContent();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error opening implant editor: {ex}");
                    }
                };
                SidebarContent.Children.Add(editBtn);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error building clone comparison: {ex}");
            }
        }

        private static string GetImplantBonusText(ImplantSet set)
        {
            long maxBonus = 0;
            foreach (var implant in set)
            {
                if (implant.Bonus > maxBonus)
                    maxBonus = implant.Bonus;
            }
            return maxBonus > 0 ? $"(+{maxBonus})" : "";
        }

        private void BuildOwnedSkillbooksSection()
        {
            try
            {
                var character = _viewModel?.Character as Character;
                if (character == null) return;

                var ownedBooks = character.Skills
                    .Where(s => s.IsOwned && !s.IsKnown)
                    .OrderBy(s => s.Name)
                    .ToList();

                if (ownedBooks.Count == 0) return;

                AddThinDivider($"Owned Skillbooks ({ownedBooks.Count})");

                foreach (var skill in ownedBooks.Take(20))
                {
                    SidebarContent.Children.Add(new TextBlock
                    {
                        Text = skill.Name,
                        FontSize = 10,
                        Foreground = new SolidColorBrush(Color.Parse("#FFB0B0B0")),
                        Margin = new Thickness(4, 1, 0, 0),
                    });
                }

                if (ownedBooks.Count > 20)
                {
                    SidebarContent.Children.Add(new TextBlock
                    {
                        Text = $"... and {ownedBooks.Count - 20} more",
                        FontSize = 10,
                        Foreground = new SolidColorBrush(Color.Parse("#FF707070")),
                        Margin = new Thickness(4, 2, 0, 0),
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error building skillbooks section: {ex}");
            }
        }

        private void AddThinDivider(string label)
        {
            var dividerPanel = new DockPanel { Margin = new Thickness(0, 8, 0, 4) };

            dividerPanel.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 10,
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
        }

        private static void AddStatCell(Grid grid, int row, int col, string value, string label)
        {
            var cell = new StackPanel
            {
                Margin = new Thickness(0, 2),
            };
            if (!string.IsNullOrEmpty(value))
            {
                cell.Children.Add(new TextBlock
                {
                    Text = value,
                    FontSize = 11,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = new SolidColorBrush(Color.Parse("#FFD0D0D0")),
                });
            }
            if (!string.IsNullOrEmpty(label))
            {
                cell.Children.Add(new TextBlock
                {
                    Text = label,
                    FontSize = 9,
                    Foreground = new SolidColorBrush(Color.Parse("#FF707070")),
                });
            }
            Grid.SetRow(cell, row);
            Grid.SetColumn(cell, col);
            grid.Children.Add(cell);
        }



        #endregion

        #region Optimize Tab

        private void BuildOptimizePanel()
        {
            if (_viewModel == null) return;

            // ── State 1: Calculating ──
            if (_optimizerVm?.IsCalculating == true)
            {
                SidebarContent.Children.Add(new TextBlock
                {
                    Text = "\u26A1 Analyzing your plan\u2026",
                    FontSize = 13,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = GoldBrush,
                    Margin = new Thickness(0, 16, 0, 8),
                });
                SidebarContent.Children.Add(new TextBlock
                {
                    Text = "Testing attribute combinations\nto find the fastest training order.",
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.Parse("#FF909090")),
                    TextWrapping = TextWrapping.Wrap,
                });
                return;
            }

            // ── State 2: Has results ──
            if (_optimizerVm?.HasResults == true)
            {
                var savings = _optimizerVm.TimeSaved;
                bool hasSavings = savings > TimeSpan.Zero;
                var character = _viewModel.Character as Character;

                // ── Current → Optimized ──
                SidebarContent.Children.Add(new TextBlock
                {
                    Text = FormatTime(_optimizerVm.CurrentDuration),
                    FontSize = 20,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(Color.Parse("#FFF0F0F0")),
                    Margin = new Thickness(0, 2, 0, 0),
                });

                if (hasSavings)
                {
                    var resultRow = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 6,
                        Margin = new Thickness(0, 2, 0, 8),
                    };
                    resultRow.Children.Add(new TextBlock
                    {
                        Text = $"\u2192 {FormatTime(_optimizerVm.OptimalDuration)}",
                        FontSize = 16,
                        FontWeight = FontWeight.Bold,
                        Foreground = new SolidColorBrush(Color.Parse("#FF81C784")),
                    });
                    resultRow.Children.Add(new TextBlock
                    {
                        Text = $"({FormatTime(savings)} faster)",
                        FontSize = 12,
                        Foreground = new SolidColorBrush(Color.Parse("#FF81C784")),
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Margin = new Thickness(0, 0, 0, 1),
                    });
                    SidebarContent.Children.Add(resultRow);

                    // ── What to change ──
                    AddThinDivider("Change your attributes to");
                    BuildAttributeComparisonGrid();

                    // Remap status
                    if (character != null)
                    {
                        int bonusRemaps = character.AvailableReMaps;
                        bool canRemapNow = bonusRemaps > 0
                            || character.LastReMapTimed == DateTime.MinValue
                            || DateTime.UtcNow >= character.LastReMapTimed.AddDays(365);

                        var remapStatus = new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Spacing = 6,
                            Margin = new Thickness(0, 4, 0, 0),
                        };

                        if (canRemapNow)
                        {
                            remapStatus.Children.Add(new TextBlock
                            {
                                Text = "\u2713 Remap available",
                                FontSize = 10,
                                Foreground = new SolidColorBrush(Color.Parse("#FF81C784")),
                            });
                        }
                        else
                        {
                            int daysUntil = (int)(character.LastReMapTimed.AddDays(365) - DateTime.UtcNow).TotalDays;
                            remapStatus.Children.Add(new TextBlock
                            {
                                Text = $"\u23F0 Next remap in {daysUntil} days",
                                FontSize = 10,
                                Foreground = new SolidColorBrush(Color.Parse("#FFFFD54F")),
                            });
                        }

                        if (bonusRemaps > 0)
                        {
                            remapStatus.Children.Add(new TextBlock
                            {
                                Text = $"\u00B7 {bonusRemaps} bonus",
                                FontSize = 10,
                                Foreground = GoldBrush,
                            });
                        }
                        SidebarContent.Children.Add(remapStatus);
                    }

                    // ── Implant recommendations ──
                    BuildImplantRecommendations();

                    // ── Most improved skills ──
                    if (_optimizerVm.SkillImpacts.Count > 0)
                    {
                        AddThinDivider("Most improved skills");

                        var topSkills = _optimizerVm.SkillImpacts
                            .Where(s => s.TimeSaved > TimeSpan.Zero)
                            .Take(5)
                            .ToList();

                        foreach (var skill in topSkills)
                        {
                            var row = new Grid
                            {
                                ColumnDefinitions = ColumnDefinitions.Parse("*,Auto"),
                                Margin = new Thickness(0, 2),
                            };

                            row.Children.Add(new TextBlock
                            {
                                Text = $"{skill.SkillName} {RomanNumeral(skill.Level)}",
                                FontSize = 11,
                                Foreground = new SolidColorBrush(Color.Parse("#FFD0D0D0")),
                                TextTrimming = TextTrimming.CharacterEllipsis,
                            });

                            var fasterTb = new TextBlock
                            {
                                Text = $"{FormatTimeCompact(skill.TimeSaved)} faster",
                                FontSize = 10,
                                FontWeight = FontWeight.SemiBold,
                                Foreground = new SolidColorBrush(Color.Parse("#FF81C784")),
                                Margin = new Thickness(6, 0, 0, 0),
                            };
                            Grid.SetColumn(fasterTb, 1);
                            row.Children.Add(fasterTb);

                            SidebarContent.Children.Add(row);
                        }
                    }
                }
                else
                {
                    SidebarContent.Children.Add(new TextBlock
                    {
                        Text = "\u2713 Your attributes are already optimal for this plan.",
                        FontSize = 11,
                        Foreground = GoldBrush,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 4, 0, 6),
                    });
                }

                // ── Re-analyze ──
                var rerunBtn = new Button
                {
                    Content = "\u21BA Re-analyze",
                    FontSize = 10,
                    Padding = new Thickness(6, 3),
                    CornerRadius = new CornerRadius(10),
                    Margin = new Thickness(0, 8, 0, 0),
                };
                rerunBtn.Click += OnRerunOptimization;
                SidebarContent.Children.Add(rerunBtn);

                return;
            }

            // ── State 3: Error ──
            if (_optimizerVm != null && !string.IsNullOrEmpty(_optimizerVm.ErrorMessage))
            {
                SidebarContent.Children.Add(new TextBlock
                {
                    Text = $"Optimization failed:\n{_optimizerVm.ErrorMessage}",
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.Parse("#FFCF6679")),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 8),
                });

                var retryBtn = new Button
                {
                    Content = "Try Again",
                    FontSize = 11,
                    Padding = new Thickness(8, 3),
                    CornerRadius = new CornerRadius(10),
                };
                retryBtn.Click += OnRerunOptimization;
                SidebarContent.Children.Add(retryBtn);
                return;
            }

            // ── State 4: Not yet run ──
            SidebarContent.Children.Add(new TextBlock
            {
                Text = "Preparing optimization\u2026",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.Parse("#FF909090")),
                Margin = new Thickness(0, 8),
            });
        }

        private static void AddIconTimeRow(Grid grid, int row, string icon, string label,
            string value, IBrush valueBrush, bool bold = false)
        {
            var iconTb = new TextBlock
            {
                Text = icon,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.Parse("#FF909090")),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 4, 0),
            };
            Grid.SetColumn(iconTb, 0);
            Grid.SetRow(iconTb, row);
            grid.Children.Add(iconTb);

            var labelTb = new TextBlock
            {
                Text = label,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.Parse("#FF909090")),
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(labelTb, 1);
            Grid.SetRow(labelTb, row);
            grid.Children.Add(labelTb);

            var valueTb = new TextBlock
            {
                Text = value,
                FontSize = 11,
                FontWeight = bold ? FontWeight.Bold : FontWeight.SemiBold,
                Foreground = valueBrush,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
            };
            Grid.SetColumn(valueTb, 2);
            Grid.SetRow(valueTb, row);
            grid.Children.Add(valueTb);
        }

        private void BuildPlanMixBars()
        {
            if (_viewModel?.DisplayPlan == null) return;

            var entries = _viewModel.DisplayPlan.ToArray();
            if (entries.Length == 0) return;

            // Group skills by (primary, secondary) attribute pair, weighted by SP
            var groups = new Dictionary<(EveAttribute pri, EveAttribute sec), long>();
            long totalSp = 0;

            foreach (var entry in entries)
            {
                var key = (entry.Skill.PrimaryAttribute, entry.Skill.SecondaryAttribute);
                groups.TryGetValue(key, out long sp);
                long entrySp = entry.SkillPointsRequired > 0 ? entry.SkillPointsRequired : 1;
                groups[key] = sp + entrySp;
                totalSp += entrySp;
            }

            if (totalSp <= 0) return;

            // Sort by SP descending
            var sorted = groups.OrderByDescending(kv => kv.Value).ToList();

            // Show top groups (combine small ones into "Other")
            double barMaxWidth = 240;

            foreach (var (pair, sp) in sorted)
            {
                double pct = (double)sp / totalSp * 100;
                if (pct < 3) break; // Skip tiny groups

                string label = $"{GetAttributeShortName(pair.pri)}/{GetAttributeShortName(pair.sec)}";
                IBrush barColor = GetAttributeBrush(pair.pri);

                var row = new Grid
                {
                    ColumnDefinitions = ColumnDefinitions.Parse("50,*,Auto"),
                    Margin = new Thickness(0, 2),
                };

                var labelTb = new TextBlock
                {
                    Text = label,
                    FontSize = 10,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = barColor,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                Grid.SetColumn(labelTb, 0);
                row.Children.Add(labelTb);

                var barContainer = new Panel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 6, 0),
                };
                barContainer.Children.Add(new Border
                {
                    Width = barMaxWidth - 80,
                    Height = 8,
                    CornerRadius = new CornerRadius(4),
                    Background = new SolidColorBrush(Color.Parse("#FF252535")),
                    HorizontalAlignment = HorizontalAlignment.Left,
                });
                barContainer.Children.Add(new Border
                {
                    Width = Math.Max(2, (pct / 100) * (barMaxWidth - 80)),
                    Height = 8,
                    CornerRadius = new CornerRadius(4),
                    Background = barColor,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Opacity = 0.7,
                });
                Grid.SetColumn(barContainer, 1);
                row.Children.Add(barContainer);

                var pctTb = new TextBlock
                {
                    Text = $"{pct:F0}%",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.Parse("#FF909090")),
                    VerticalAlignment = VerticalAlignment.Center,
                };
                Grid.SetColumn(pctTb, 2);
                row.Children.Add(pctTb);

                SidebarContent.Children.Add(row);
            }

            // Mix description
            if (sorted.Count > 0)
            {
                double dominantPct = (double)sorted[0].Value / totalSp * 100;
                string mixDesc;
                if (dominantPct > 85)
                    mixDesc = $"Almost entirely {GetAttributeShortName(sorted[0].Key.pri)}/{GetAttributeShortName(sorted[0].Key.sec)}. Specialize.";
                else if (dominantPct > 70)
                    mixDesc = $"Leans {GetAttributeShortName(sorted[0].Key.pri)}/{GetAttributeShortName(sorted[0].Key.sec)}. Specializing saves most.";
                else if (dominantPct > 50)
                    mixDesc = "Mixed plan. Balanced remap recommended.";
                else
                    mixDesc = "Evenly split. Balance is clearly best.";

                SidebarContent.Children.Add(new TextBlock
                {
                    Text = mixDesc,
                    FontSize = 10,
                    FontStyle = FontStyle.Italic,
                    Foreground = new SolidColorBrush(Color.Parse("#FF808080")),
                    Margin = new Thickness(0, 3, 0, 0),
                    TextWrapping = TextWrapping.Wrap,
                });
            }
        }

        private static string FormatTimeCompact(TimeSpan time)
        {
            if (time.TotalDays >= 1) return $"{(int)time.TotalDays}d {time.Hours}h";
            if (time.TotalHours >= 1) return $"{(int)time.TotalHours}h {time.Minutes}m";
            return $"{(int)time.TotalMinutes}m";
        }

        private void BuildAttributeComparisonGrid()
        {
            if (_optimizerVm == null) return;

            var attrs = new[]
            {
                EveAttribute.Intelligence, EveAttribute.Perception,
                EveAttribute.Charisma, EveAttribute.Willpower, EveAttribute.Memory
            };

            // Container with subtle background
            var container = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#10FFFFFF")),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 8),
                Margin = new Thickness(0, 2, 0, 0),
            };
            var stack = new StackPanel { Spacing = 6 };

            // Column headers
            var headerRow = new Grid
            {
                ColumnDefinitions = ColumnDefinitions.Parse("40,28,20,28,*"),
                Margin = new Thickness(0, 0, 0, 2),
            };
            var inGameHeader = new TextBlock
            {
                Text = "In-game",
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.Parse("#FF606060")),
                HorizontalAlignment = HorizontalAlignment.Right,
            };
            Grid.SetColumn(inGameHeader, 1);
            headerRow.Children.Add(inGameHeader);
            var changeToHeader = new TextBlock
            {
                Text = "Set to",
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.Parse("#FF606060")),
            };
            Grid.SetColumn(changeToHeader, 3);
            headerRow.Children.Add(changeToHeader);
            stack.Children.Add(headerRow);

            foreach (var attr in attrs)
            {
                int current = _optimizerVm.GetCurrent(attr);
                int optimal = _optimizerVm.GetOptimal(attr);
                int delta = optimal - current;
                bool changed = delta != 0;

                var row = new Grid
                {
                    ColumnDefinitions = ColumnDefinitions.Parse("40,28,20,28,*"),
                };

                // Attribute name — colored, prominent
                row.Children.Add(new TextBlock
                {
                    Text = GetAttributeShortName(attr),
                    FontSize = 13,
                    FontWeight = FontWeight.Bold,
                    Foreground = GetAttributeBrush(attr),
                    VerticalAlignment = VerticalAlignment.Center,
                });

                // Current value
                var currentTb = new TextBlock
                {
                    Text = current.ToString(),
                    FontSize = 13,
                    Foreground = new SolidColorBrush(Color.Parse("#FF909090")),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Right,
                };
                Grid.SetColumn(currentTb, 1);
                row.Children.Add(currentTb);

                // Arrow
                var arrowTb = new TextBlock
                {
                    Text = "\u2192",
                    FontSize = 12,
                    Foreground = changed
                        ? new SolidColorBrush(Color.Parse("#FF808080"))
                        : new SolidColorBrush(Color.Parse("#FF404040")),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                };
                Grid.SetColumn(arrowTb, 2);
                row.Children.Add(arrowTb);

                // Target value — big and bold if changed
                var targetTb = new TextBlock
                {
                    Text = optimal.ToString(),
                    FontSize = 13,
                    FontWeight = changed ? FontWeight.Bold : FontWeight.Regular,
                    Foreground = changed
                        ? new SolidColorBrush(Color.Parse("#FFF0F0F0"))
                        : new SolidColorBrush(Color.Parse("#FF505050")),
                    VerticalAlignment = VerticalAlignment.Center,
                };
                Grid.SetColumn(targetTb, 3);
                row.Children.Add(targetTb);

                // Delta badge
                if (changed)
                {
                    string sign = delta > 0 ? "+" : "";
                    var deltaBadge = new Border
                    {
                        Background = delta > 0
                            ? new SolidColorBrush(Color.Parse("#2081C784"))
                            : new SolidColorBrush(Color.Parse("#20CF6679")),
                        CornerRadius = new CornerRadius(6),
                        Padding = new Thickness(5, 1),
                        Margin = new Thickness(4, 0, 0, 0),
                        VerticalAlignment = VerticalAlignment.Center,
                        Child = new TextBlock
                        {
                            Text = $"{sign}{delta}",
                            FontSize = 12,
                            FontWeight = FontWeight.SemiBold,
                            Foreground = delta > 0
                                ? new SolidColorBrush(Color.Parse("#FF81C784"))
                                : new SolidColorBrush(Color.Parse("#FFCF6679")),
                        }
                    };
                    Grid.SetColumn(deltaBadge, 4);
                    row.Children.Add(deltaBadge);
                }

                stack.Children.Add(row);
            }

            container.Child = stack;
            SidebarContent.Children.Add(container);

            // Manual adjustment toggle for power users
            var fineTuneBtn = new ToggleButton
            {
                Content = _showFineTune
                    ? "Hide manual adjustment \u25B4"
                    : "Adjust attributes manually \u25BE",
                FontSize = 10,
                Padding = new Thickness(8, 3),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(0, 6, 0, 0),
            };
            ToolTip.SetTip(fineTuneBtn, "Override the optimizer\u2019s recommendation with your own attribute distribution");
            fineTuneBtn.Click += (_, _) =>
            {
                _showFineTune = fineTuneBtn.IsChecked == true;
                BuildSidebarContent();
            };
            fineTuneBtn.IsChecked = _showFineTune;
            SidebarContent.Children.Add(fineTuneBtn);

            if (_showFineTune)
            {
                BuildInteractiveAttributeRows();

                int unassigned = _optimizerVm.UnassignedPoints;
                if (unassigned > 0)
                {
                    SidebarContent.Children.Add(new TextBlock
                    {
                        Text = $"\u26A0 {unassigned} point{(unassigned != 1 ? "s" : "")} to use",
                        FontSize = 11,
                        FontWeight = FontWeight.SemiBold,
                        Foreground = new SolidColorBrush(Color.Parse("#FFFFD54F")),
                        Margin = new Thickness(0, 4, 0, 0),
                    });
                }
            }
        }

        private void BuildImplantRecommendations()
        {
            if (_optimizerVm == null || _viewModel?.DisplayPlan == null) return;

            var allAttrs = new[]
            {
                EveAttribute.Intelligence, EveAttribute.Perception,
                EveAttribute.Charisma, EveAttribute.Willpower, EveAttribute.Memory
            };

            // Calculate per-attribute SP weight from the plan
            var entries = _viewModel.DisplayPlan.ToArray();
            var attrSpWeight = new Dictionary<EveAttribute, long>();
            foreach (var attr in allAttrs) attrSpWeight[attr] = 0;

            foreach (var entry in entries)
            {
                long sp = entry.SkillPointsRequired > 0 ? entry.SkillPointsRequired : 1;
                var pri = entry.Skill.PrimaryAttribute;
                var sec = entry.Skill.SecondaryAttribute;
                attrSpWeight[pri] += sp;       // primary contributes full weight
                attrSpWeight[sec] += sp / 2;   // secondary contributes half
            }

            // Find missing implants sorted by plan relevance
            var recommendations = new List<(EveAttribute attr, long weight, int currentBonus)>();
            foreach (var attr in allAttrs)
            {
                int bonus = _optimizerVm.GetImplantBonus(attr);
                long weight = attrSpWeight[attr];
                if (bonus < 5 && weight > 0) // missing or below +5
                {
                    recommendations.Add((attr, weight, bonus));
                }
            }

            if (recommendations.Count == 0) return; // all slots filled with +5

            recommendations.Sort((a, b) => b.weight.CompareTo(a.weight));

            SidebarContent.Children.Add(new TextBlock
            {
                Text = "2. Consider these implants",
                FontSize = 11,
                FontWeight = FontWeight.SemiBold,
                Foreground = new SolidColorBrush(Color.Parse("#FFF0F0F0")),
                Margin = new Thickness(0, 10, 0, 4),
            });

            long maxWeight = recommendations.Max(r => r.weight);

            foreach (var (attr, weight, currentBonus) in recommendations)
            {
                double relevance = maxWeight > 0 ? (double)weight / maxWeight : 0;
                if (relevance < 0.1) continue; // skip irrelevant attributes

                string impact;
                IBrush impactBrush;
                if (relevance > 0.7)
                {
                    impact = "high impact";
                    impactBrush = new SolidColorBrush(Color.Parse("#FF81C784"));
                }
                else if (relevance > 0.3)
                {
                    impact = "moderate impact";
                    impactBrush = new SolidColorBrush(Color.Parse("#FFFFD54F"));
                }
                else
                {
                    impact = "low impact";
                    impactBrush = new SolidColorBrush(Color.Parse("#FF707070"));
                }

                string slotText = currentBonus > 0
                    ? $"+5 {GetAttributeFullName(attr)} (upgrade from +{currentBonus})"
                    : $"+5 {GetAttributeFullName(attr)}";

                var row = new StackPanel { Margin = new Thickness(8, 2, 0, 0) };
                var topLine = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 6,
                };
                topLine.Children.Add(new TextBlock
                {
                    Text = slotText,
                    FontSize = 11,
                    Foreground = GetAttributeBrush(attr),
                    VerticalAlignment = VerticalAlignment.Center,
                });
                topLine.Children.Add(new TextBlock
                {
                    Text = $"\u2014 {impact}",
                    FontSize = 10,
                    Foreground = impactBrush,
                    VerticalAlignment = VerticalAlignment.Center,
                });
                row.Children.Add(topLine);

                SidebarContent.Children.Add(row);
            }
        }

        private void BuildInteractiveAttributeRows()
        {
            if (_optimizerVm == null) return;

            var attrs = new[]
            {
                EveAttribute.Intelligence, EveAttribute.Perception,
                EveAttribute.Charisma, EveAttribute.Willpower, EveAttribute.Memory
            };

            foreach (var attr in attrs)
            {
                int currentBase = _optimizerVm.GetCurrent(attr);
                int optimalVal = _optimizerVm.GetOptimal(attr);
                int remappable = _optimizerVm.GetRemappable(attr);
                int delta = optimalVal - currentBase;

                // Line 1: "INT  19 → 24" with delta coloring
                var infoPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 4,
                    Margin = new Thickness(0, 4, 0, 0),
                };

                infoPanel.Children.Add(new TextBlock
                {
                    Text = GetAttributeShortName(attr),
                    FontSize = 11,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = GetAttributeBrush(attr),
                    Width = 30,
                });

                infoPanel.Children.Add(new TextBlock
                {
                    Text = $"{currentBase} \u2192 {optimalVal}",
                    FontSize = 11,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = delta > 0
                        ? new SolidColorBrush(Color.Parse("#FF81C784"))
                        : delta < 0
                            ? new SolidColorBrush(Color.Parse("#FFCF6679"))
                            : new SolidColorBrush(Color.Parse("#FF808080")),
                });

                SidebarContent.Children.Add(infoPanel);

                // Line 2: [−] [colored bar] [+]
                var barPanel = new Grid
                {
                    ColumnDefinitions = ColumnDefinitions.Parse("Auto,*,Auto"),
                    Margin = new Thickness(0, 1, 0, 2),
                };

                var decBtn = new Button
                {
                    Content = "\u2212",
                    FontSize = 11,
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
                barPanel.Children.Add(decBtn);

                double maxBarWidth = 180;
                double barFraction = remappable / 10.0;

                var barContainer = new Panel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(4, 0),
                };
                barContainer.Children.Add(new Border
                {
                    Width = maxBarWidth,
                    Height = 8,
                    CornerRadius = new CornerRadius(4),
                    Background = new SolidColorBrush(Color.Parse("#FF252535")),
                });
                barContainer.Children.Add(new Border
                {
                    Width = Math.Max(0, barFraction * maxBarWidth),
                    Height = 8,
                    CornerRadius = new CornerRadius(4),
                    Background = GetAttributeBrush(attr),
                    HorizontalAlignment = HorizontalAlignment.Left,
                });
                Grid.SetColumn(barContainer, 1);
                barPanel.Children.Add(barContainer);

                var incBtn = new Button
                {
                    Content = "+",
                    FontSize = 11,
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
                Grid.SetColumn(incBtn, 2);
                barPanel.Children.Add(incBtn);

                SidebarContent.Children.Add(barPanel);
            }
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
            // Placeholder — keep existing browser tabs for now
            SidebarContent.Children.Add(new TextBlock
            {
                Text = "Use the Skills, Ships, Items, or Blueprints tabs to add skills to your plan.",
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.Parse("#FF909090")),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 8),
            });

            var addBtn = new Button
            {
                Content = "+ Add Skills",
                FontSize = 11,
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
                    if (_activeSidebarTab is "Optimize" or "Summary")
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

        private void OnPlanToLevel(object? sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem) return;
            if (menuItem.Tag is not string tagStr || !int.TryParse(tagStr, out int level)) return;

            // Find the context menu's associated item
            var contextMenu = menuItem.Parent as MenuItem;
            var parentMenu = contextMenu?.Parent as ContextMenu;
            var row = parentMenu?.PlacementTarget as Control;
            var item = FindDisplayItemFromControl(row);
            if (item == null || _viewModel?.Plan == null) return;

            _viewModel.Plan.PlanTo(item.Entry.Skill, level);
            _viewModel.UpdateDisplayPlan();
            Refresh();
            BuildSidebarContent();
            UpdateParentStatusBar();
        }

        private PlanEntryDisplayItem? FindDisplayItemFromControl(Control? control)
        {
            // Walk up to find a Border with a ContextMenu whose items lead to a PlanEntryDisplayItem
            // In our current design, the context menu is on the border, and we pass the item via DataContext
            // Actually, in BuildSkillRow we set context menu on border. The border's child grid has column 0
            // with name panel. Let's find from _currentEntryItems based on the border's position.
            if (control == null || _currentDisplayItems == null) return null;

            // The control is the border that hosts the context menu
            int index = PlanItemsControl.ItemsSource?.Cast<Control>().ToList().IndexOf(control) ?? -1;
            if (index >= 0 && index < _currentDisplayItems.Count)
            {
                return _currentDisplayItems[index] as PlanEntryDisplayItem;
            }
            return null;
        }

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
