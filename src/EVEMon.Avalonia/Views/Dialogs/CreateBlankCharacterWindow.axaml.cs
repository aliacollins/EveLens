using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using EVEMon.Common.Data;
using EVEMon.Common.Enumerations;
using EVEMon.Common.Helpers;

namespace EVEMon.Avalonia.Views.Dialogs
{
    public partial class CreateBlankCharacterWindow : Window
    {
        private static readonly Race[] s_races = { Race.Amarr, Race.Caldari, Race.Gallente, Race.Minmatar };

        private static readonly Dictionary<Race, (Bloodline Bloodline, Ancestry Ancestry)> s_defaults = new()
        {
            { Race.Amarr, (Bloodline.Amarr, Ancestry.Liberal_Holders) },
            { Race.Caldari, (Bloodline.Deteis, Ancestry.Merchandisers) },
            { Race.Gallente, (Bloodline.Gallente, Ancestry.Activists) },
            { Race.Minmatar, (Bloodline.Sebiestor, Ancestry.Tinkerers) }
        };

        public bool CharacterCreated { get; private set; }

        public CreateBlankCharacterWindow()
        {
            InitializeComponent();

            RaceCombo.ItemsSource = s_races.Select(r => r.ToString()).ToList();
            RaceCombo.SelectedIndex = 0;
            RaceCombo.SelectionChanged += OnRaceChanged;

            CreateBtn.Click += OnCreateClick;
            CancelBtn.Click += OnCancelClick;

            BuildSkillSummary(Race.Amarr);
        }

        private void OnRaceChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (RaceCombo.SelectedIndex < 0) return;
            var race = s_races[RaceCombo.SelectedIndex];
            BuildSkillSummary(race);
        }

        private void BuildSkillSummary(Race race)
        {
            try
            {
                SkillSummaryHeader.Text = $"Starting Skills ({race})";

                var startingSkills = BlankCharacterUIHelper.GetStartingSkills(race);

                // Group by skill group name
                var grouped = startingSkills
                    .Select(kvp => new { Skill = StaticSkills.GetSkillByID(kvp.Key), Level = kvp.Value })
                    .Where(x => x.Skill != null)
                    .GroupBy(x => x.Skill!.Group.Name)
                    .OrderBy(g => g.Key)
                    .ToList();

                var items = new List<object>();
                foreach (var group in grouped)
                {
                    // Group header
                    items.Add(new SkillGroupHeader
                    {
                        Name = group.Key,
                        Count = group.Count()
                    });

                    foreach (var skill in group.OrderBy(s => s.Skill!.Name))
                    {
                        items.Add(new SkillEntry
                        {
                            Name = skill.Skill!.Name,
                            Level = skill.Level
                        });
                    }
                }

                SkillListPanel.ItemTemplate = new global::Avalonia.Controls.Templates.FuncDataTemplate<object>((item, _) =>
                {
                    if (item is SkillGroupHeader header)
                    {
                        return new TextBlock
                        {
                            Text = $"{header.Name} ({header.Count})",
                            FontSize = 11,
                            FontWeight = FontWeight.SemiBold,
                            Foreground = (IBrush?)Application.Current?.FindResource("EveAccentPrimaryBrush") ?? Brushes.Gold,
                            Margin = new Thickness(0, 6, 0, 2)
                        };
                    }
                    if (item is SkillEntry entry)
                    {
                        return new TextBlock
                        {
                            Text = $"  {entry.Name} {ToRoman(entry.Level)}",
                            FontSize = 11,
                            Foreground = (IBrush?)Application.Current?.FindResource("EveTextPrimaryBrush") ?? Brushes.White,
                            Margin = new Thickness(16, 1, 0, 1)
                        };
                    }
                    return new TextBlock();
                });
                SkillListPanel.ItemsSource = items;

                // Compute total SP
                long totalSP = startingSkills.Sum(kvp =>
                {
                    var skill = StaticSkills.GetSkillByID(kvp.Key);
                    return skill?.GetPointsRequiredForLevel(kvp.Value) ?? 0;
                });

                SkillPointsText.Text = $"~{totalSP / 1000}k SP \u00b7 Omega clone";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error building skill summary: {ex}");
            }
        }

        private async void OnCreateClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                string name = CharacterNameTextBox.Text?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(name))
                {
                    CharacterNameTextBox.Focus();
                    return;
                }

                if (RaceCombo.SelectedIndex < 0) return;
                var race = s_races[RaceCombo.SelectedIndex];
                var defaults = s_defaults[race];

                BlankCharacterUIHelper.CharacterName = name;
                BlankCharacterUIHelper.Race = race;
                BlankCharacterUIHelper.Gender = Gender.Female;
                BlankCharacterUIHelper.Bloodline = defaults.Bloodline;
                BlankCharacterUIHelper.Ancestry = defaults.Ancestry;
                BlankCharacterUIHelper.AddBlankCharacter();

                CharacterCreated = true;
                Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating blank character: {ex}");
            }
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        private static string ToRoman(int level) => level switch
        {
            1 => "I",
            2 => "II",
            3 => "III",
            4 => "IV",
            5 => "V",
            _ => level.ToString()
        };

        private sealed class SkillGroupHeader
        {
            public string Name { get; init; } = string.Empty;
            public int Count { get; init; }
        }

        private sealed class SkillEntry
        {
            public string Name { get; init; } = string.Empty;
            public int Level { get; init; }
        }
    }
}
