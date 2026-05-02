// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using EveLens.Avalonia.Converters;
using EveLens.Avalonia.Services;
using EveLens.Common.Data;
using EveLens.Common.Models;
using EveLens.Common.Service;
using EveLens.Common.Services;
using EveLens.Common.SettingsObjects;
using EveLens.Common.ViewModels;

namespace EveLens.Avalonia.Views.Dialogs
{
    public partial class GlobalPlanDashboardWindow : Window
    {
        private readonly GlobalPlanDashboardViewModel _vm = new();

        public GlobalPlanDashboardWindow()
        {
            InitializeComponent();

            try
            {
                var uri = new Uri("avares://EveLens/Properties/EveLens.ico");
                Icon = new WindowIcon(global::Avalonia.Platform.AssetLoader.Open(uri));
            }
            catch { }

            _vm.Refresh();

            LocalizeUI();

            NewTemplateBtn.Click += OnNewTemplate;
            ImportFromPlanBtn.Click += OnImportFromPlan;
            AddSkillBtn.Click += OnAddSkill;
            AddCharBtn.Click += OnAddCharacter;
            ApplyAllBtn.Click += OnApplyAll;

            RebuildUI();
        }

        private void LocalizeUI()
        {
            Title = Loc.Get("Doctrine.Title");
            DoctrinesSidebarLabel.Text = Loc.Get("Doctrine.Doctrines");
            NewTemplateBtn.Content = Loc.Get("Doctrine.NewDoctrine");
            ImportFromPlanBtn.Content = Loc.Get("Doctrine.ImportFromPlan");
            AddSkillBtn.Content = Loc.Get("Action.AddSkill");
            AddCharBtn.Content = Loc.Get("Action.AddCharacter");
            ApplyAllBtn.Content = Loc.Get("Doctrine.CreatePlansForAll");
        }

        private void RebuildUI()
        {
            BuildSidebar();
            BuildComparisonPanel();
            UpdateStatus();
        }

        #region Sidebar

        private void BuildSidebar()
        {
            TemplateList.Children.Clear();

            foreach (var template in _vm.Templates)
            {
                bool isSelected = template == _vm.SelectedTemplate;

                var row = new Border
                {
                    Background = isSelected
                        ? FindBrush("EveBackgroundMediumBrush") ?? Brushes.Transparent
                        : Brushes.Transparent,
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(10, 7),
                    Cursor = new Cursor(StandardCursorType.Hand),
                };

                var stack = new DockPanel();

                var deleteBtn = new Button
                {
                    Content = "✕",
                    FontSize = FontScaleService.Caption,
                    Padding = new Thickness(4, 2),
                    Background = Brushes.Transparent,
                    Foreground = FindBrush("EveTextDisabledBrush") ?? Brushes.Gray,
                    CornerRadius = new CornerRadius(8),
                    Cursor = new Cursor(StandardCursorType.Hand),
                };
                DockPanel.SetDock(deleteBtn, Dock.Right);
                var capturedTemplate = template;
                deleteBtn.Click += (_, _) =>
                {
                    _vm.DeleteTemplate(capturedTemplate);
                    _vm.Refresh();
                    RebuildUI();
                };

                var nameBlock = new TextBlock
                {
                    Text = template.Name,
                    FontSize = FontScaleService.Body,
                    FontWeight = isSelected ? FontWeight.SemiBold : FontWeight.Normal,
                    Foreground = isSelected
                        ? (FindBrush("EveAccentPrimaryBrush") ?? Brushes.Gold)
                        : (FindBrush("EveTextPrimaryBrush") ?? Brushes.White),
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                };

                var countBlock = new TextBlock
                {
                    Text = $"{template.Entries.Count} skills · {template.SubscribedCharacterGuids.Count} chars",
                    FontSize = FontScaleService.Caption,
                    Foreground = FindBrush("EveTextDisabledBrush") ?? Brushes.Gray,
                };

                var textStack = new StackPanel { Spacing = 1 };
                textStack.Children.Add(nameBlock);
                textStack.Children.Add(countBlock);

                stack.Children.Add(deleteBtn);
                stack.Children.Add(textStack);

                row.Child = stack;
                row.PointerPressed += (_, _) =>
                {
                    _vm.SelectTemplate(capturedTemplate);
                    RebuildUI();
                };

                TemplateList.Children.Add(row);
            }

            if (_vm.Templates.Count == 0)
            {
                TemplateList.Children.Add(new TextBlock
                {
                    Text = Loc.Get("Doctrine.NoDoctrines"),
                    FontSize = FontScaleService.Body,
                    Foreground = FindBrush("EveTextDisabledBrush") ?? Brushes.Gray,
                    Margin = new Thickness(8, 20),
                    TextWrapping = TextWrapping.Wrap,
                });
            }
        }

        #endregion

        #region Comparison Panel

        private void BuildComparisonPanel()
        {
            ComparisonGrid.Children.Clear();
            SummaryCards.Children.Clear();

            if (_vm.SelectedTemplate == null)
            {
                TemplateTitle.Text = "No template selected";
                ComparisonGrid.Children.Add(new TextBlock
                {
                    Text = Loc.Get("Doctrine.SelectDoctrine"),
                    FontSize = FontScaleService.Body,
                    Foreground = FindBrush("EveTextDisabledBrush") ?? Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 60),
                });
                return;
            }

            TemplateTitle.Text = _vm.SelectedTemplate.Name;

            if (_vm.SubscribedCharacters.Count == 0)
            {
                ComparisonGrid.Children.Add(new TextBlock
                {
                    Text = Loc.Get("Doctrine.NoCharacters"),
                    FontSize = FontScaleService.Body,
                    Foreground = FindBrush("EveTextDisabledBrush") ?? Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 60),
                    TextWrapping = TextWrapping.Wrap,
                });
                return;
            }

            BuildSummaryCards();
            BuildComparisonHeader();

            foreach (var row in _vm.ComparisonRows)
                ComparisonGrid.Children.Add(BuildComparisonRow(row));
        }

        private void BuildSummaryCards()
        {
            for (int i = 0; i < _vm.SubscribedCharacters.Count; i++)
            {
                var character = _vm.SubscribedCharacters[i];
                var totalTime = _vm.GetCharacterTotalTime(i);
                int trainedCount = _vm.GetCharacterTrainedCount(i);
                int totalSkills = _vm.TotalSkillsInTemplate;
                int pct = totalSkills > 0 ? (int)(trainedCount * 100.0 / totalSkills) : 0;

                var card = new Border
                {
                    Background = FindBrush("EveBackgroundMediumBrush") ?? Brushes.Transparent,
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(12, 8),
                    MinWidth = 140,
                    Margin = new Thickness(0, 0, 6, 6),
                };

                var portrait = new Image { Width = 24, Height = 24, Stretch = Stretch.UniformToFill };
                var portraitBorder = new Border
                {
                    Width = 24, Height = 24,
                    CornerRadius = new CornerRadius(3),
                    ClipToBounds = true,
                    Background = FindBrush("EveBackgroundDarkestBrush") ?? Brushes.Black,
                    Child = portrait,
                    Margin = new Thickness(0, 0, 8, 0),
                };
                LoadPortraitAsync(portrait, character);

                var timeStr = FormatTime(totalTime);
                var cardStack = new StackPanel { Spacing = 1 };
                cardStack.Children.Add(new TextBlock
                {
                    Text = character.Name,
                    FontSize = FontScaleService.Body,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = FindBrush("EveAccentPrimaryBrush") ?? Brushes.Gold,
                });
                cardStack.Children.Add(new TextBlock
                {
                    Text = pct == 100 ? Loc.Get("Doctrine.AllTrained") : $"{timeStr} {Loc.Get("Status.Remaining")}",
                    FontSize = FontScaleService.Caption,
                    Foreground = pct == 100
                        ? (FindBrush("EveSuccessGreenBrush") ?? Brushes.LimeGreen)
                        : (FindBrush("EveTextSecondaryBrush") ?? Brushes.Gray),
                });
                cardStack.Children.Add(new TextBlock
                {
                    Text = $"{trainedCount}/{totalSkills} skills ({pct}%)",
                    FontSize = FontScaleService.Caption,
                    Foreground = FindBrush("EveTextDisabledBrush") ?? Brushes.Gray,
                });

                if (pct < 100)
                {
                    var capturedChar = character;
                    var createPlanBtn = new Button
                    {
                        Content = Loc.Get("Doctrine.CreatePlan"),
                        FontSize = FontScaleService.Caption,
                        Padding = new Thickness(8, 2),
                        CornerRadius = new CornerRadius(10),
                        Margin = new Thickness(0, 3, 0, 0),
                        Cursor = new Cursor(StandardCursorType.Hand),
                    };
                    createPlanBtn.Click += async (_, _) =>
                    {
                        _vm.GeneratePersonalPlan(capturedChar);
                        createPlanBtn.Content = Loc.Get("Doctrine.PlanCreated");
                        createPlanBtn.Foreground = FindBrush("EveSuccessGreenBrush") ?? Brushes.LimeGreen;
                        createPlanBtn.IsEnabled = false;
                        ShowToast($"{Loc.Get("Doctrine.PlanCreated")} — {capturedChar.Name}");
                        await System.Threading.Tasks.Task.Delay(2000);
                        createPlanBtn.Content = Loc.Get("Doctrine.CreatePlan");
                        createPlanBtn.Foreground = null;
                        createPlanBtn.IsEnabled = true;
                    };
                    cardStack.Children.Add(createPlanBtn);
                }

                var row = new StackPanel { Orientation = Orientation.Horizontal };
                row.Children.Add(portraitBorder);
                row.Children.Add(cardStack);
                card.Child = row;

                SummaryCards.Children.Add(card);
            }
        }

        private void BuildComparisonHeader()
        {
            int charCount = _vm.SubscribedCharacters.Count;
            string colDefs = "200,50,50";
            for (int i = 0; i < charCount; i++)
                colDefs += ",120";

            var header = new Grid
            {
                ColumnDefinitions = ColumnDefinitions.Parse(colDefs),
                Height = 28,
            };

            header.Children.Add(MakeHeaderCell("SKILL", 0, HorizontalAlignment.Left));
            header.Children.Add(MakeHeaderCell("LVL", 1, HorizontalAlignment.Center));
            header.Children.Add(MakeHeaderCell("R", 2, HorizontalAlignment.Center));

            for (int i = 0; i < charCount; i++)
            {
                string name = _vm.SubscribedCharacters[i].Name;
                if (name.Contains(' '))
                    name = name.Split(' ')[0];
                header.Children.Add(MakeHeaderCell(name, 3 + i, HorizontalAlignment.Center));
            }

            ComparisonGrid.Children.Add(header);

            ComparisonGrid.Children.Add(new Border
            {
                Height = 1,
                Background = FindBrush("EveBorderBrush") ?? Brushes.Gray,
            });
        }

        private Control BuildComparisonRow(SkillComparisonRow row)
        {
            int charCount = _vm.SubscribedCharacters.Count;
            string colDefs = "200,50,50";
            for (int i = 0; i < charCount; i++)
                colDefs += ",120";

            var grid = new Grid
            {
                ColumnDefinitions = ColumnDefinitions.Parse(colDefs),
                Height = 30,
            };

            var nameText = new TextBlock
            {
                Text = $"{row.SkillName}",
                FontSize = FontScaleService.Body,
                Foreground = FindBrush("EveTextPrimaryBrush") ?? Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(4, 0),
            };
            ToolTip.SetTip(nameText, $"{row.SkillGroup} · {row.PrimaryAttribute}/{row.SecondaryAttribute}");
            Grid.SetColumn(nameText, 0);
            grid.Children.Add(nameText);

            grid.Children.Add(MakeCell(row.TargetLevel.ToString(), 1, FindBrush("EveTextSecondaryBrush")));
            grid.Children.Add(MakeCell(row.Rank.ToString(), 2, FindBrush("EveTextDisabledBrush")));

            for (int i = 0; i < row.CharacterEntries.Count; i++)
            {
                var entry = row.CharacterEntries[i];
                IBrush? fg;
                string text;

                if (entry.Status == SkillTrainingStatus.AlreadyTrained)
                {
                    text = "✓";
                    fg = FindBrush("EveSuccessGreenBrush") ?? Brushes.LimeGreen;
                }
                else
                {
                    text = FormatTime(entry.TrainingTime);
                    fg = entry.TrainingTime.TotalDays > 30
                        ? (FindBrush("EveErrorRedBrush") ?? Brushes.Red)
                        : entry.TrainingTime.TotalDays > 7
                            ? (FindBrush("EveWarningYellowBrush") ?? Brushes.Yellow)
                            : (FindBrush("EveTextPrimaryBrush") ?? Brushes.White);
                }

                var cell = MakeCell(text, 3 + i, fg);
                if (entry.Status == SkillTrainingStatus.NeedsTraining)
                    ToolTip.SetTip(cell, $"Level {entry.CurrentLevel} → {entry.TargetLevel} · {entry.SpPerHour:N0} SP/hr");
                grid.Children.Add(cell);
            }

            return grid;
        }

        #endregion

        #region Actions

        private async void OnNewTemplate(object? sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new Window
                {
                    Title = "New Doctrine",
                    Width = 350, Height = 150,
                    CanResize = false,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Background = FindBrush("EveBackgroundDarkBrush") as ISolidColorBrush ?? new SolidColorBrush(Color.Parse("#1A1A2E")),
                };

                var textBox = new TextBox
                {
                    Watermark = "Doctrine name (e.g. Cerberus Fleet)...",
                    FontSize = FontScaleService.Body,
                    Margin = new Thickness(16, 16, 16, 8),
                };

                var okBtn = new Button
                {
                    Content = "Create",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Padding = new Thickness(24, 6),
                    CornerRadius = new CornerRadius(12),
                };

                string? result = null;
                okBtn.Click += (_, _) =>
                {
                    result = textBox.Text;
                    dialog.Close();
                };
                textBox.KeyDown += (_, ke) =>
                {
                    if (ke.Key == Key.Enter)
                    {
                        result = textBox.Text;
                        dialog.Close();
                    }
                };

                dialog.Content = new StackPanel
                {
                    Children = { textBox, okBtn },
                    VerticalAlignment = VerticalAlignment.Center,
                };

                await dialog.ShowDialog(this);

                if (!string.IsNullOrWhiteSpace(result))
                {
                    var template = _vm.CreateTemplate(result.Trim());
                    _vm.SelectTemplate(template);
                    RebuildUI();
                }
            }
            catch { }
        }

        private async void OnImportFromPlan(object? sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new Window
                {
                    Title = "Import from Existing Plan",
                    Width = 400, Height = 500,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Background = FindBrush("EveBackgroundDarkBrush") as ISolidColorBrush ?? new SolidColorBrush(Color.Parse("#1A1A2E")),
                };

                var planList = new StackPanel { Spacing = 2, Margin = new Thickness(12) };
                bool imported = false;

                var allChars = AppServices.Characters?.Cast<Character>().ToList() ?? new List<Character>();
                foreach (var character in allChars.OrderBy(c => c.Name))
                {
                    if (character is not CCPCharacter ccp || ccp.Plans.Count == 0) continue;

                    planList.Children.Add(new TextBlock
                    {
                        Text = character.Name,
                        FontSize = FontScaleService.Subheading,
                        FontWeight = FontWeight.SemiBold,
                        Foreground = FindBrush("EveAccentPrimaryBrush") ?? Brushes.Gold,
                        Margin = new Thickness(0, 8, 0, 4),
                    });

                    foreach (var plan in ccp.Plans)
                    {
                        var capturedPlan = plan;
                        var btn = new Button
                        {
                            Content = $"{plan.Name}  ({plan.Count} skills)",
                            FontSize = FontScaleService.Body,
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            HorizontalContentAlignment = HorizontalAlignment.Left,
                            Padding = new Thickness(10, 5),
                            Background = Brushes.Transparent,
                            CornerRadius = new CornerRadius(6),
                        };
                        btn.Click += (_, _) =>
                        {
                            var template = _vm.CreateFromPlan(capturedPlan);
                            _vm.SelectTemplate(template);
                            imported = true;
                            dialog.Close();
                        };
                        planList.Children.Add(btn);
                    }
                }

                if (planList.Children.Count == 0)
                {
                    planList.Children.Add(new TextBlock
                    {
                        Text = "No character plans found.",
                        FontSize = FontScaleService.Body,
                        Foreground = FindBrush("EveTextDisabledBrush") ?? Brushes.Gray,
                    });
                }

                dialog.Content = new ScrollViewer { Content = planList };
                await dialog.ShowDialog(this);

                if (imported)
                    RebuildUI();
            }
            catch { }
        }

        private async void OnAddSkill(object? sender, RoutedEventArgs e)
        {
            if (_vm.SelectedTemplate == null) return;

            try
            {
                var dialog = new Window
                {
                    Title = "Add Skill",
                    Width = 400, Height = 500,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Background = FindBrush("EveBackgroundDarkBrush") as ISolidColorBrush ?? new SolidColorBrush(Color.Parse("#1A1A2E")),
                };

                var searchBox = new TextBox
                {
                    Watermark = "Search skills...",
                    FontSize = FontScaleService.Body,
                    Margin = new Thickness(12, 12, 12, 6),
                };

                var levelBox = new ComboBox
                {
                    ItemsSource = new[] { "Level 1", "Level 2", "Level 3", "Level 4", "Level 5" },
                    SelectedIndex = 0,
                    Margin = new Thickness(12, 0, 12, 6),
                    FontSize = FontScaleService.Body,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                };

                var resultsList = new StackPanel { Spacing = 1 };
                var scroll = new ScrollViewer
                {
                    Content = resultsList,
                    Margin = new Thickness(12, 0, 12, 12),
                };

                bool added = false;

                void Search()
                {
                    resultsList.Children.Clear();
                    string query = searchBox.Text?.Trim() ?? "";
                    if (query.Length < 2) return;

                    var matches = StaticSkills.AllSkills
                        .Where(s => s.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(s => s.Name)
                        .Take(50);

                    foreach (var skill in matches)
                    {
                        var capturedSkill = skill;
                        var btn = new Button
                        {
                            Content = $"{skill.Name}  (R{skill.Rank} · {skill.Group?.Name})",
                            FontSize = FontScaleService.Body,
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            HorizontalContentAlignment = HorizontalAlignment.Left,
                            Padding = new Thickness(8, 4),
                            Background = Brushes.Transparent,
                            CornerRadius = new CornerRadius(4),
                        };
                        btn.Click += (_, _) =>
                        {
                            int level = levelBox.SelectedIndex + 1;
                            _vm.AddSkill(capturedSkill.ID, level);
                            added = true;
                            dialog.Close();
                        };
                        resultsList.Children.Add(btn);
                    }
                }

                searchBox.TextChanged += (_, _) => Search();

                dialog.Content = new DockPanel
                {
                    Children =
                    {
                        searchBox,
                        levelBox,
                        scroll,
                    },
                };
                DockPanel.SetDock(searchBox, Dock.Top);
                DockPanel.SetDock(levelBox, Dock.Top);

                await dialog.ShowDialog(this);

                if (added)
                    RebuildUI();
            }
            catch { }
        }

        private async void OnAddCharacter(object? sender, RoutedEventArgs e)
        {
            if (_vm.SelectedTemplate == null) return;

            try
            {
                var dialog = new Window
                {
                    Title = "Add Character",
                    Width = 350, Height = 450,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Background = FindBrush("EveBackgroundDarkBrush") as ISolidColorBrush ?? new SolidColorBrush(Color.Parse("#1A1A2E")),
                };

                var charList = new StackPanel { Spacing = 2, Margin = new Thickness(12) };

                var allChars = AppServices.Characters?.Cast<Character>().ToList() ?? new List<Character>();
                var alreadySubscribed = new HashSet<Guid>(_vm.SelectedTemplate.SubscribedCharacterGuids);
                bool anyAdded = false;

                foreach (var character in allChars.OrderBy(c => c.Name))
                {
                    if (alreadySubscribed.Contains(character.Guid)) continue;

                    var captured = character;
                    var btn = new Button
                    {
                        Content = character.Name,
                        FontSize = FontScaleService.Body,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        HorizontalContentAlignment = HorizontalAlignment.Left,
                        Padding = new Thickness(10, 6),
                        Background = Brushes.Transparent,
                        CornerRadius = new CornerRadius(6),
                    };
                    btn.Click += (_, _) =>
                    {
                        _vm.SubscribeCharacter(captured);
                        anyAdded = true;
                        dialog.Close();
                    };
                    charList.Children.Add(btn);
                }

                if (charList.Children.Count == 0)
                {
                    charList.Children.Add(new TextBlock
                    {
                        Text = "All characters are already subscribed.",
                        FontSize = FontScaleService.Body,
                        Foreground = FindBrush("EveTextDisabledBrush") ?? Brushes.Gray,
                    });
                }

                dialog.Content = new ScrollViewer { Content = charList };
                await dialog.ShowDialog(this);

                if (anyAdded)
                    RebuildUI();
            }
            catch { }
        }

        private async void OnApplyAll(object? sender, RoutedEventArgs e)
        {
            if (_vm.SelectedTemplate == null || _vm.SubscribedCharacters.Count == 0) return;

            int created = 0;
            for (int i = 0; i < _vm.SubscribedCharacters.Count; i++)
            {
                var character = _vm.SubscribedCharacters[i];
                int trainedCount = _vm.GetCharacterTrainedCount(i);
                if (trainedCount >= _vm.TotalSkillsInTemplate) continue;

                _vm.GeneratePersonalPlan(character);
                created++;
            }

            if (created > 0)
            {
                ApplyAllBtn.Content = $"{Loc.Get("Doctrine.PlanCreated")} ({created})";
                ApplyAllBtn.Foreground = FindBrush("EveSuccessGreenBrush") ?? Brushes.LimeGreen;
                ApplyAllBtn.IsEnabled = false;
                ShowToast($"{Loc.Get("Doctrine.PlanCreated")} ({created})");
                await System.Threading.Tasks.Task.Delay(3000);
                ApplyAllBtn.Content = Loc.Get("Doctrine.CreatePlansForAll");
                ApplyAllBtn.Foreground = null;
                ApplyAllBtn.IsEnabled = true;
            }
            else
            {
                ShowToast("All characters already have this doctrine trained!");
            }
        }

        #endregion

        #region Helpers

        private TextBlock MakeHeaderCell(string text, int col, HorizontalAlignment align)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontSize = FontScaleService.Caption,
                Foreground = FindBrush("EveTextDisabledBrush") ?? Brushes.Gray,
                HorizontalAlignment = align,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0),
            };
            Grid.SetColumn(tb, col);
            return tb;
        }

        private TextBlock MakeCell(string text, int col, IBrush? foreground)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontSize = FontScaleService.Body,
                Foreground = foreground ?? Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(tb, col);
            return tb;
        }

        private static string FormatTime(TimeSpan time)
        {
            if (time <= TimeSpan.Zero) return "Done";
            if (time.TotalDays >= 1) return $"{(int)time.TotalDays}d {time.Hours}h";
            if (time.TotalHours >= 1) return $"{(int)time.TotalHours}h {time.Minutes}m";
            return $"{(int)time.TotalMinutes}m";
        }

        private IBrush? FindBrush(string key)
        {
            if (this.TryFindResource(key, this.ActualThemeVariant, out var res) && res is IBrush b)
                return b;
            return null;
        }

        private async void LoadPortraitAsync(Image image, Character character)
        {
            try
            {
                if (character is CCPCharacter ccp)
                {
                    var drawingImage = await ImageService.GetCharacterImageAsync(ccp.CharacterID);
                    if (drawingImage != null)
                    {
                        var converted = DrawingImageToAvaloniaConverter.Instance.Convert(
                            drawingImage, typeof(Bitmap), null!, CultureInfo.InvariantCulture);
                        if (converted is Bitmap bitmap)
                            image.Source = bitmap;
                    }
                }
            }
            catch { }
        }

        private void UpdateStatus()
        {
            if (_vm.SelectedTemplate == null)
            {
                StatusText.Text = $"{_vm.Templates.Count} doctrine(s)";
                return;
            }

            int skills = _vm.TotalSkillsInTemplate;
            int chars = _vm.SubscribedCharacters.Count;
            string longest = FormatTime(_vm.LongestTotalTime);
            string shortest = FormatTime(_vm.ShortestTotalTime);

            StatusText.Text = chars > 0
                ? $"{skills} skills · {chars} characters · Shortest: {shortest} · Longest: {longest}"
                : $"{skills} skills · No characters subscribed";
        }

        private async void ShowToast(string message)
        {
            try
            {
                StatusText.Text = message;
                StatusText.Foreground = FindBrush("EveSuccessGreenBrush") ?? Brushes.LimeGreen;
                await System.Threading.Tasks.Task.Delay(3000);
                StatusText.Foreground = FindBrush("EveTextSecondaryBrush") ?? Brushes.Gray;
                UpdateStatus();
            }
            catch { }
        }

        #endregion
    }
}
