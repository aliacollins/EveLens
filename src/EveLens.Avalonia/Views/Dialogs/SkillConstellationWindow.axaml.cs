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
using EveLens.Avalonia.Controls;
using EveLens.Common.Data;
using EveLens.Common.Models;
using SkiaSharp;

namespace EveLens.Avalonia.Views.Dialogs
{
    public partial class SkillConstellationWindow : Window
    {
        private Character? _character;
        private readonly Dictionary<string, SkillNode> _nodeMap = new();

        // Predefined colors for skill groups (cycle if more groups than colors)
        private static readonly SKColor[] GroupColors =
        {
            new(0x4A, 0x9E, 0xE8), // blue
            new(0xE8, 0xA4, 0x4A), // gold
            new(0x6D, 0xBA, 0x6D), // green
            new(0xC7, 0x5D, 0x5D), // red
            new(0xB0, 0x7D, 0xC7), // purple
            new(0x5D, 0xD5, 0xC7), // teal
            new(0xE8, 0x6D, 0xA4), // pink
            new(0xA4, 0xC7, 0x5D), // lime
            new(0x5D, 0x8B, 0xE8), // indigo
            new(0xE8, 0xC7, 0x5D), // amber
            new(0x7D, 0xC7, 0xB0), // mint
            new(0xC7, 0x8B, 0x5D), // brown
            new(0x8B, 0x5D, 0xC7), // violet
            new(0x5D, 0xC7, 0x6D), // emerald
            new(0xC7, 0x5D, 0x8B), // magenta
            new(0xE8, 0x8B, 0x5D), // orange
        };

        public SkillConstellationWindow()
        {
            InitializeComponent();
        }

        public void Initialize(Character character)
        {
            _character = character;
            Title = $"Skill Constellation — {character.Name}";

            BuildConstellationData();
            WireEvents();
        }

        private void BuildConstellationData()
        {
            if (_character == null) return;

            var groups = new List<SkillGroupInfo>();
            int groupIndex = 0;

            // Try character skill groups first; fall back to static data
            IEnumerable<IEnumerable<(int Id, string Name, int Level, long Rank, bool IsTraining, string TrainingTime, List<string> PrereqIds, string GroupName)>>? skillData = null;

            try
            {
                var skillGroups = _character.SkillGroups
                    .Where(g => g.Any())
                    .OrderBy(g => g.Name)
                    .ToList();

                foreach (var sg in skillGroups)
                {
                    var color = GroupColors[groupIndex % GroupColors.Length];
                    var groupInfo = new SkillGroupInfo
                    {
                        Name = sg.Name,
                        Color = color,
                        Index = groupIndex
                    };

                    foreach (var skill in sg)
                    {
                        var prereqIds = new List<string>();
                        try
                        {
                            if (skill.StaticData?.Prerequisites != null)
                            {
                                foreach (var prereq in skill.StaticData.Prerequisites)
                                {
                                    prereqIds.Add($"skill_{prereq.Skill?.ID ?? 0}");
                                }
                            }
                        }
                        catch
                        {
                            // Static data may not be loaded
                        }

                        string trainingTime = string.Empty;
                        try
                        {
                            if (skill.IsTraining)
                                trainingTime = skill.GetLeftTrainingTimeToNextLevel.ToString(@"d\d\ h\h");
                        }
                        catch { }

                        var node = new SkillNode
                        {
                            Id = $"skill_{skill.ID}",
                            Name = skill.Name,
                            GroupName = sg.Name,
                            GroupIndex = groupIndex,
                            Level = (int)skill.Level,
                            Rank = (int)skill.Rank,
                            IsTraining = skill.IsTraining,
                            TrainingTime = trainingTime,
                            PrereqIds = prereqIds
                        };

                        groupInfo.Nodes.Add(node);
                        _nodeMap[node.Id] = node;
                    }

                    if (groupInfo.Nodes.Count > 0)
                    {
                        groups.Add(groupInfo);
                        groupIndex++;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error building constellation data: {ex}");
            }

            // If no real data, create a demo constellation
            if (groups.Count == 0)
            {
                groups.Add(CreateDemoGroup("No Skills Loaded", 0));
            }

            // Character summary
            int totalSkills = 0;
            int knownSkills = 0;
            long sp = 0;
            try
            {
                knownSkills = _character.KnownSkillCount;
                totalSkills = knownSkills + _character.Skills.Count(s => s.Level == 0);
                sp = _character.SkillPoints;
            }
            catch { }

            Canvas.CharacterName = _character.Name;
            Canvas.SkillSummary = $"{knownSkills} of {totalSkills} skills  ·  {sp:N0} SP";
            CharInfoText.Text = $"{_character.Name}  ·  {Canvas.SkillSummary}";

            Canvas.SetData(groups);
            BuildGroupChips();
        }

        private void BuildGroupChips()
        {
            GroupChips.Items.Clear();
            foreach (var group in Canvas.Groups)
            {
                int trained = group.Nodes.Count(n => n.Level > 0);
                int total = group.Nodes.Count;
                var chipColor = Color.FromArgb(group.Color.Alpha, group.Color.Red, group.Color.Green, group.Color.Blue);

                var chip = new Button
                {
                    Content = $"{group.Name} {trained}/{total}",
                    FontSize = 10,
                    Padding = new Thickness(10, 5),
                    CornerRadius = new CornerRadius(4),
                    Background = new SolidColorBrush(Colors.Transparent),
                    Foreground = new SolidColorBrush(Color.FromRgb(0x6E, 0x76, 0x81)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(0x0D, 0xFF, 0xFF, 0xFF)),
                    BorderThickness = new Thickness(1),
                    Tag = group.Index,
                    Cursor = new Cursor(StandardCursorType.Hand)
                };

                chip.PointerEntered += (_, _) =>
                {
                    Canvas.SetHighlightGroup(group.Name);
                    chip.Foreground = new SolidColorBrush(chipColor);
                    chip.Background = new SolidColorBrush(Color.FromArgb(0x15, chipColor.R, chipColor.G, chipColor.B));
                    chip.BorderBrush = new SolidColorBrush(Color.FromArgb(0x40, chipColor.R, chipColor.G, chipColor.B));
                    chip.FontWeight = FontWeight.SemiBold;
                };
                chip.PointerExited += (_, _) =>
                {
                    Canvas.SetHighlightGroup(null);
                    chip.Foreground = new SolidColorBrush(Color.FromRgb(0x6E, 0x76, 0x81));
                    chip.Background = new SolidColorBrush(Colors.Transparent);
                    chip.BorderBrush = new SolidColorBrush(Color.FromArgb(0x0D, 0xFF, 0xFF, 0xFF));
                    chip.FontWeight = FontWeight.Normal;
                };
                chip.Click += (_, _) =>
                {
                    Canvas.FocusGroup(group.Index);
                };

                GroupChips.Items.Add(chip);
            }
        }

        private void WireEvents()
        {
            Canvas.SelectionChanged += OnSelectionChanged;
            CloseDetailBtn.Click += (_, _) =>
            {
                Canvas.ClearSelection();
                DetailPanel.IsVisible = false;
            };

            ToggleLabelsBtn.Click += (_, _) =>
            {
                Canvas.ShowAllLabels = !Canvas.ShowAllLabels;
                ToggleLabelsBtn.Content = Canvas.ShowAllLabels ? "Hide Labels" : "Show All Labels";
            };

            Opened += (_, _) => SearchBox.Focus();

            // Search
            SearchBox.TextChanged += (_, _) => OnSearchTextChanged();
            ClearSearchBtn.Click += (_, _) =>
            {
                SearchBox.Text = string.Empty;
                ClearSearch();
            };
        }

        private void OnSearchTextChanged()
        {
            string query = SearchBox.Text?.Trim() ?? string.Empty;
            ClearSearchBtn.IsVisible = query.Length > 0;

            if (query.Length < 2)
            {
                ClearSearch();
                return;
            }

            // Find matching nodes
            var matches = _nodeMap.Values
                .Where(n => n.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                .OrderBy(n => n.Name)
                .Take(15)
                .ToList();

            var matchIds = new HashSet<string>(matches.Select(n => n.Id));
            Canvas.SetSearchResults(matchIds);

            // Build results dropdown
            SearchResultsList.Children.Clear();
            if (matches.Count > 0)
            {
                SearchResultsPanel.IsVisible = true;
                foreach (var node in matches)
                {
                    var groupColor = node.GroupIndex < GroupColors.Length
                        ? GroupColors[node.GroupIndex]
                        : GroupColors[node.GroupIndex % GroupColors.Length];
                    var avColor = Color.FromRgb(groupColor.Red, groupColor.Green, groupColor.Blue);

                    var row = new Button
                    {
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        HorizontalContentAlignment = HorizontalAlignment.Left,
                        Background = Brushes.Transparent,
                        BorderThickness = new Thickness(0),
                        Padding = new Thickness(8, 4),
                        CornerRadius = new CornerRadius(4),
                        Cursor = new Cursor(StandardCursorType.Hand),
                        Content = new DockPanel
                        {
                            Children =
                            {
                                new TextBlock
                                {
                                    Text = $"Lv {node.Level}",
                                    FontSize = 9,
                                    Foreground = node.Level > 0
                                        ? new SolidColorBrush(Color.FromRgb(0x6D, 0xBA, 0x6D))
                                        : new SolidColorBrush(Color.FromRgb(0x48, 0x4F, 0x58)),
                                    VerticalAlignment = VerticalAlignment.Center,
                                    Width = 30,
                                    [DockPanel.DockProperty] = global::Avalonia.Controls.Dock.Right
                                },
                                new StackPanel
                                {
                                    Spacing = 1,
                                    Children =
                                    {
                                        new TextBlock
                                        {
                                            Text = node.Name,
                                            FontSize = 11,
                                            Foreground = new SolidColorBrush(Color.FromRgb(0xC9, 0xD1, 0xD9))
                                        },
                                        new TextBlock
                                        {
                                            Text = node.GroupName,
                                            FontSize = 9,
                                            Foreground = new SolidColorBrush(avColor)
                                        }
                                    }
                                }
                            }
                        }
                    };

                    var capturedNode = node;
                    row.Click += (_, _) =>
                    {
                        Canvas.FocusNode(capturedNode.Id);
                        SearchResultsPanel.IsVisible = false;
                    };

                    SearchResultsList.Children.Add(row);
                }
            }
            else
            {
                SearchResultsPanel.IsVisible = true;
                SearchResultsList.Children.Add(new TextBlock
                {
                    Text = "No skills found",
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x48, 0x4F, 0x58)),
                    Margin = new Thickness(8, 4)
                });
            }
        }

        private void ClearSearch()
        {
            Canvas.SetSearchResults(new HashSet<string>());
            SearchResultsPanel.IsVisible = false;
            SearchResultsList.Children.Clear();
            ClearSearchBtn.IsVisible = false;
        }

        private void OnSelectionChanged(SkillNode? node)
        {
            if (node == null)
            {
                DetailPanel.IsVisible = false;
                return;
            }

            DetailPanel.IsVisible = true;

            var groupColor = node.GroupIndex < GroupColors.Length
                ? GroupColors[node.GroupIndex]
                : GroupColors[node.GroupIndex % GroupColors.Length];
            var avColor = Color.FromRgb(groupColor.Red, groupColor.Green, groupColor.Blue);

            DetailName.Text = node.Name;
            DetailGroup.Text = $"{node.GroupName} · Rank {node.Rank}";
            DetailGroup.Foreground = new SolidColorBrush(avColor);

            // Update border color
            DetailPanel.BorderBrush = new SolidColorBrush(Color.FromArgb(0x30, avColor.R, avColor.G, avColor.B));

            // Level pips
            LevelPips.Children.Clear();
            for (int i = 0; i < 5; i++)
            {
                LevelPips.Children.Add(new Border
                {
                    Width = 36,
                    Height = 6,
                    CornerRadius = new CornerRadius(2),
                    Background = i < node.Level
                        ? new SolidColorBrush(avColor)
                        : new SolidColorBrush(Color.FromArgb(0x0F, 0xFF, 0xFF, 0xFF)),
                    BorderBrush = i < node.Level
                        ? null
                        : new SolidColorBrush(Color.FromArgb(0x0D, 0xFF, 0xFF, 0xFF)),
                    BorderThickness = i < node.Level ? new Thickness(0) : new Thickness(1)
                });
            }

            // Level status text
            if (node.Level == 5)
                LevelStatus.Text = "Fully trained";
            else if (node.IsTraining)
                LevelStatus.Text = $"Training · {node.TrainingTime} remaining";
            else if (node.Level == 0)
                LevelStatus.Text = "Not trained";
            else
                LevelStatus.Text = $"Level {node.Level} of 5";

            // Prerequisites
            PrereqList.Children.Clear();
            if (node.PrereqIds.Count > 0)
            {
                PrereqSection.IsVisible = true;
                foreach (string pid in node.PrereqIds)
                {
                    if (!_nodeMap.TryGetValue(pid, out var prereqNode)) continue;

                    var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
                    row.Children.Add(new TextBlock
                    {
                        Text = prereqNode.Level > 0 ? "✓" : "✕",
                        Foreground = prereqNode.Level > 0
                            ? new SolidColorBrush(Color.FromRgb(0x6D, 0xBA, 0x6D))
                            : new SolidColorBrush(Color.FromRgb(0xC7, 0x5D, 0x5D)),
                        FontSize = 10
                    });
                    row.Children.Add(new TextBlock
                    {
                        Text = prereqNode.Name,
                        FontSize = 11,
                        Foreground = new SolidColorBrush(Color.FromRgb(0xC9, 0xD1, 0xD9)),
                        Cursor = new Cursor(StandardCursorType.Hand)
                    });
                    row.Children.Add(new TextBlock
                    {
                        Text = $"Lv {prereqNode.Level}",
                        FontSize = 9,
                        Foreground = new SolidColorBrush(Color.FromRgb(0x48, 0x4F, 0x58)),
                        HorizontalAlignment = HorizontalAlignment.Right
                    });

                    var capturedPrereq = prereqNode;
                    row.PointerPressed += (_, _) =>
                    {
                        Canvas.ClearSelection();
                        // Simulate clicking the prereq node
                        OnSelectionChanged(capturedPrereq);
                    };

                    PrereqList.Children.Add(row);
                }
            }
            else
            {
                PrereqSection.IsVisible = false;
            }

        }

        private static SkillGroupInfo CreateDemoGroup(string name, int index)
        {
            var color = GroupColors[index % GroupColors.Length];
            var group = new SkillGroupInfo { Name = name, Color = color, Index = index };
            group.Nodes.Add(new SkillNode
            {
                Id = "demo_1", Name = "Awaiting Data", GroupName = name,
                GroupIndex = index, Level = 0, Rank = 1, PrereqIds = new()
            });
            return group;
        }
    }
}
