// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using EveLens.Avalonia.Converters;
using EveLens.Common;
using EveLens.Common.Enumerations;
using EveLens.Common.Events;
using EveLens.Common.Helpers;
using EveLens.Common.Models;
using EveLens.Common.Service;
using EveLens.Common.Services;
using EveLens.Common.SettingsObjects;
using EveLens.Avalonia.Views.Dialogs;
using EveLens.Core.Events;

namespace EveLens.Avalonia.Views.CharacterMonitor
{
    public partial class CharacterOverviewView : UserControl
    {
        private IDisposable? _fetchCompletedSub;
        private IDisposable? _secondTickSub;
        private IDisposable? _collectionChangedSub;
        private IDisposable? _privacyModeSub;

        // Track previous ISK/SP values per character for flash-on-change
        private readonly Dictionary<long, decimal> _prevBalances = new();
        private readonly Dictionary<long, long> _prevSkillPoints = new();

        // Track animation timers so we can stop them on detach/rebuild
        private readonly List<DispatcherTimer> _animationTimers = new();

        // Toggle for fetching dot pulse (alternates each second tick)
        private bool _fetchingDotBright;

        // Only animate the staggered fade-in on first application load
        private bool _initialLoadDone;


        public CharacterOverviewView()
        {
            InitializeComponent();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            _fetchCompletedSub ??= AppServices.EventAggregator?.Subscribe<MonitorFetchCompletedEvent>(
                evt => Dispatcher.UIThread.Post(() => OnFetchCompleted(evt)));

            _secondTickSub ??= AppServices.EventAggregator?.Subscribe<SecondTickEvent>(
                _ => Dispatcher.UIThread.Post(OnSecondTick));

            _collectionChangedSub ??= AppServices.EventAggregator?.Subscribe<MonitoredCharacterCollectionChangedEvent>(
                _ => Dispatcher.UIThread.Post(OnCollectionChanged));

            _privacyModeSub ??= AppServices.EventAggregator?.Subscribe<PrivacyModeChangedEvent>(
                _ => Dispatcher.UIThread.Post(LoadData));

            LoadData();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            foreach (var t in _animationTimers)
                t.Stop();
            _animationTimers.Clear();

            _fetchCompletedSub?.Dispose();
            _fetchCompletedSub = null;
            _secondTickSub?.Dispose();
            _secondTickSub = null;
            _collectionChangedSub?.Dispose();
            _collectionChangedSub = null;
            _privacyModeSub?.Dispose();
            _privacyModeSub = null;

            base.OnDetachedFromVisualTree(e);
        }

        /// <summary>
        /// Public method to refresh the view (called after group changes).
        /// </summary>
        public void RefreshView()
        {
            LoadData();
        }

        #region Initial Load

        private void LoadData()
        {
            try
            {
                foreach (var t in _animationTimers)
                    t.Stop();
                _animationTimers.Clear();

                OverviewPanel.Children.Clear();
                _prevBalances.Clear();
                _prevSkillPoints.Clear();

                var characters = AppServices.Characters.Where(c => c.Monitored).ToList();

                if (characters.Count == 0)
                {
                    BuildWelcomeState();
                    _initialLoadDone = true;
                    return;
                }

                var groups = Settings.CharacterGroups;
                int cardIndex = 0;

                if (groups.Count > 0)
                {
                    var assignedGuids = new HashSet<Guid>();

                    foreach (var group in groups)
                    {
                        var groupChars = characters
                            .Where(c => group.CharacterGuids.Contains(c.Guid))
                            .ToList();

                        if (groupChars.Count == 0) continue;

                        foreach (var guid in group.CharacterGuids)
                            assignedGuids.Add(guid);

                        BuildGroupSection(group.Name, groupChars, ref cardIndex);
                    }

                    var ungrouped = characters.Where(c => !assignedGuids.Contains(c.Guid)).ToList();
                    if (ungrouped.Count > 0)
                    {
                        BuildGroupSection("Ungrouped", ungrouped, ref cardIndex);
                    }
                }
                else
                {
                    var wrap = BuildCardWrapPanel(characters, ref cardIndex);
                    OverviewPanel.Children.Add(wrap);
                }

                // Seed balance/SP tracking
                foreach (var c in characters)
                {
                    _prevBalances[c.CharacterID] = c.Balance;
                    _prevSkillPoints[c.CharacterID] = c.SkillPoints;
                }

                // Load portraits after items are rendered
                Dispatcher.UIThread.Post(() => LoadPortraitsAndTraining(),
                    DispatcherPriority.Background);

                _initialLoadDone = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Overview load error: {ex}");
            }
        }

        private void BuildGroupSection(string groupName, List<Character> characters, ref int cardIndex)
        {
            var divider = new DockPanel { Margin = new Thickness(0, 4, 0, 2) };

            var label = new TextBlock
            {
                Text = groupName,
                FontSize = 11,
                FontWeight = FontWeight.SemiBold,
                Foreground = FindBrush("EveAccentPrimaryBrush", Brushes.Gold),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0),
                [DockPanel.DockProperty] = Dock.Left
            };

            var count = new TextBlock
            {
                Text = $"{characters.Count}",
                FontSize = 10,
                Foreground = FindBrush("EveTextDisabledBrush", Brushes.Gray),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0),
                [DockPanel.DockProperty] = Dock.Left
            };

            var line = new Border
            {
                Height = 1,
                Background = FindBrush("EveAccentPrimaryBrush", Brushes.Gold),
                Opacity = 0.3,
                VerticalAlignment = VerticalAlignment.Center
            };

            divider.Children.Add(label);
            divider.Children.Add(count);
            divider.Children.Add(line);

            OverviewPanel.Children.Add(divider);
            OverviewPanel.Children.Add(BuildCardWrapPanel(characters, ref cardIndex));
        }

        private WrapPanel BuildCardWrapPanel(List<Character> characters, ref int cardIndex)
        {
            var wrap = new WrapPanel { Orientation = Orientation.Horizontal };

            foreach (var character in characters)
            {
                var card = BuildCharacterCard(character, cardIndex);
                wrap.Children.Add(card);
                cardIndex++;
            }

            return wrap;
        }

        private void BuildWelcomeState()
        {
            var container = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Spacing = 12,
                Margin = new Thickness(0, 80, 0, 0)
            };

            container.Children.Add(new TextBlock
            {
                Text = "Welcome to EveLens",
                FontSize = 15,
                FontWeight = FontWeight.Bold,
                Foreground = FindBrush("EveAccentPrimaryBrush", Brushes.Gold),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center
            });

            container.Children.Add(new TextBlock
            {
                Text = "Add your first character to start monitoring\nskills, wallet, assets, and more.",
                FontSize = 11,
                Foreground = FindBrush("EveTextSecondaryBrush", Brushes.Gray),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 360
            });

            var addButton = new Button
            {
                Content = "Add Character",
                FontSize = 11,
                Padding = new Thickness(16, 6),
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = FindBrush("EveAccentPrimaryBrush", Brushes.Gold),
                Background = Brushes.Transparent,
                BorderBrush = FindBrush("EveAccentPrimaryBrush", Brushes.Gold),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Cursor = new global::Avalonia.Input.Cursor(global::Avalonia.Input.StandardCursorType.Hand)
            };
            addButton.Click += OnAddCharacterFromWelcome;
            container.Children.Add(addButton);

            container.Children.Add(new TextBlock
            {
                Text = "You can also add characters from the File menu.",
                FontSize = 10,
                Foreground = FindBrush("EveTextDisabledBrush", Brushes.Gray),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 4, 0, 0)
            });

            OverviewPanel.Children.Add(container);
        }

        private async void OnAddCharacterFromWelcome(object? sender, RoutedEventArgs e)
        {
            try
            {
                var mainWindow = this.FindAncestorOfType<MainWindow>();
                if (mainWindow == null) return;

                var dialog = new AddCharacterWindow();
                await dialog.ShowDialog(mainWindow);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Welcome add character error: {ex}");
            }
        }

        #endregion

        #region Card Builder

        private Button BuildCharacterCard(Character character, int staggerIndex)
        {
            // Portrait with ESI status badge overlay (Teams-style)
            var portraitImage = new Image { Width = 56, Height = 56, Stretch = Stretch.UniformToFill };
            portraitImage.Tag = character.CharacterID;

            var portraitBorder = new Border
            {
                Width = 56, Height = 56,
                Background = FindBrush("EveBackgroundDarkestBrush", Brushes.Black),
                BorderBrush = FindBrush("EveBorderBrush", Brushes.Gray),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Child = portraitImage
            };

            var (dotColor, isFetching) = GetEsiStatus(character);
            var esiDot = new Border
            {
                Width = 12, Height = 12,
                CornerRadius = new CornerRadius(6),
                Background = dotColor,
                BorderBrush = FindBrush("EveBackgroundDarkBrush", Brushes.Black),
                BorderThickness = new Thickness(2),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Tag = "EsiDot"
            };

            // Grid overlays the dot on the portrait
            var portraitContainer = new Grid
            {
                Width = 56, Height = 56,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 0, 12, 0),
                Children = { portraitBorder, esiDot }
            };

            // Info panel
            var infoPanel = new StackPanel { Spacing = 1 };

            infoPanel.Children.Add(new TextBlock
            {
                Text = PrivacyHelper.IsNameHidden ? PrivacyHelper.Mask : character.Name,
                FontSize = 13, FontWeight = FontWeight.Bold,
                Foreground = FindBrush("EveAccentPrimaryBrush", Brushes.Gold),
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            var iskText = new TextBlock
            {
                Text = PrivacyHelper.IsBalanceHidden ? $"{PrivacyHelper.Mask} ISK" : $"{character.Balance:N2} ISK",
                FontSize = 11,
                Foreground = FindBrush("EveSuccessGreenBrush", Brushes.LimeGreen),
                Tag = "IskText",
                Transitions = new global::Avalonia.Animation.Transitions
                {
                    new global::Avalonia.Animation.DoubleTransition
                    {
                        Property = OpacityProperty,
                        Duration = TimeSpan.FromMilliseconds(300),
                        Easing = new QuadraticEaseOut()
                    }
                }
            };
            infoPanel.Children.Add(iskText);

            var spText = new TextBlock
            {
                Text = PrivacyHelper.IsSkillPointsHidden ? $"{PrivacyHelper.Mask} SP" : $"{character.SkillPoints:N0} SP",
                FontSize = 11,
                Foreground = FindBrush("EveTextSecondaryBrush", Brushes.Gray),
                Tag = "SpText",
                Transitions = new global::Avalonia.Animation.Transitions
                {
                    new global::Avalonia.Animation.DoubleTransition
                    {
                        Property = OpacityProperty,
                        Duration = TimeSpan.FromMilliseconds(300),
                        Easing = new QuadraticEaseOut()
                    }
                }
            };
            infoPanel.Children.Add(spText);

            // Account status badge
            bool isOmega = character.EffectiveCharacterStatus == AccountStatus.Omega;
            var badgeText = new TextBlock
            {
                Text = isOmega ? "\u03A9 Omega" : "\u03B1 Alpha",
                FontSize = 9, FontWeight = FontWeight.SemiBold,
                Foreground = isOmega
                    ? new SolidColorBrush(Color.Parse("#FF00C853"))
                    : new SolidColorBrush(Color.Parse("#FFFF6D00"))
            };
            var badge = new Border
            {
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(6, 1),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 1, 0, 0),
                Background = isOmega
                    ? new SolidColorBrush(Color.Parse("#2200C853"))
                    : new SolidColorBrush(Color.Parse("#22FF6D00")),
                Child = badgeText
            };
            infoPanel.Children.Add(badge);

            // Training status
            var trainingText = new TextBlock
            {
                FontSize = 10,
                Foreground = FindBrush("EveWarningYellowBrush", Brushes.Yellow),
                TextTrimming = TextTrimming.CharacterEllipsis,
                Tag = "TrainingText"
            };
            UpdateTrainingText(trainingText, character);
            infoPanel.Children.Add(trainingText);

            var cardGrid = new Grid { ColumnDefinitions = ColumnDefinitions.Parse("68,*") };
            Grid.SetColumn(portraitContainer, 0);
            Grid.SetColumn(infoPanel, 1);
            cardGrid.Children.Add(portraitContainer);
            cardGrid.Children.Add(infoPanel);

            var cardBorder = new Border
            {
                Padding = new Thickness(12, 10),
                Width = 300,
                MinHeight = 90,
                Child = cardGrid
            };
            cardBorder.Classes.Add("card");

            bool animate = !_initialLoadDone;

            var btn = new Button
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                Margin = new Thickness(4),
                Cursor = new global::Avalonia.Input.Cursor(global::Avalonia.Input.StandardCursorType.Hand),
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Content = cardBorder,
                DataContext = character,
                Opacity = animate ? 0 : 1
            };

            if (animate)
            {
                btn.Transitions = new global::Avalonia.Animation.Transitions
                {
                    new global::Avalonia.Animation.DoubleTransition
                    {
                        Property = OpacityProperty,
                        Duration = TimeSpan.FromMilliseconds(300),
                        Easing = new QuadraticEaseOut()
                    }
                };

                int delayMs = staggerIndex * 50;
                if (delayMs > 0)
                {
                    var timer = new DispatcherTimer(DispatcherPriority.Render)
                    {
                        Interval = TimeSpan.FromMilliseconds(delayMs)
                    };
                    timer.Tick += (_, _) =>
                    {
                        timer.Stop();
                        _animationTimers.Remove(timer);
                        btn.Opacity = 1;
                    };
                    _animationTimers.Add(timer);
                    timer.Start();
                }
                else
                {
                    Dispatcher.UIThread.Post(() => btn.Opacity = 1, DispatcherPriority.Render);
                }
            }

            // Context menu
            var deleteItem = new MenuItem { Header = "Delete Character...", Tag = character };
            deleteItem.Click += OnDeleteCharacter;
            btn.ContextMenu = new ContextMenu { Items = { deleteItem } };

            btn.Click += OnCharacterCardClick;

            return btn;
        }

        private (IBrush dotColor, bool isFetching) GetEsiStatus(Character character)
        {
            var yellowBrush = FindBrush("EveWarningYellowBrush", Brushes.Yellow);
            var greenBrush = FindBrush("EveSuccessGreenBrush", Brushes.LimeGreen);
            var redBrush = FindBrush("EveErrorRedBrush", Brushes.Red);
            var grayBrush = FindBrush("EveTextDisabledBrush", Brushes.Gray);

            if (character is not CCPCharacter ccp)
                return (grayBrush, false);

            bool anyUpdating = false;
            bool anyNoKey = false;
            bool hasErrors = false;

            try
            {
                foreach (var monitor in ccp.QueryMonitors)
                {
                    if (monitor.IsUpdating)
                        anyUpdating = true;
                    if (monitor.Status == QueryStatus.NoESIKey)
                        anyNoKey = true;
                }

                hasErrors = ccp.QueryMonitors.HasErrors;
            }
            catch
            {
                return (grayBrush, false);
            }

            if (anyUpdating)
                return (yellowBrush, true);

            if (anyNoKey)
                return (redBrush, false);

            if (hasErrors)
                return (redBrush, false);

            if (ccp.HasCompletedFirstUpdate)
                return (greenBrush, false);

            return (grayBrush, false);
        }

        #endregion

        #region Event Handlers — Incremental Updates

        private void OnFetchCompleted(MonitorFetchCompletedEvent evt)
        {
            try
            {
                var card = FindCardByCharacterId(evt.CharacterId);
                if (card == null) return;

                var character = card.DataContext as Character;
                if (character == null) return;

                // Update ISK
                var iskBlock = FindTaggedDescendant<TextBlock>(card, "IskText");
                if (iskBlock != null)
                {
                    string newIsk = PrivacyHelper.IsBalanceHidden
                        ? $"{PrivacyHelper.Mask} ISK"
                        : $"{character.Balance:N2} ISK";
                    if (iskBlock.Text != newIsk)
                    {
                        iskBlock.Text = newIsk;
                        if (!PrivacyHelper.IsBalanceHidden) FlashTextBlock(iskBlock);
                    }
                    _prevBalances[character.CharacterID] = character.Balance;
                }

                // Update SP
                var spBlock = FindTaggedDescendant<TextBlock>(card, "SpText");
                if (spBlock != null)
                {
                    string newSp = PrivacyHelper.IsSkillPointsHidden
                        ? $"{PrivacyHelper.Mask} SP"
                        : $"{character.SkillPoints:N0} SP";
                    if (spBlock.Text != newSp)
                    {
                        spBlock.Text = newSp;
                        if (!PrivacyHelper.IsSkillPointsHidden) FlashTextBlock(spBlock);
                    }
                    _prevSkillPoints[character.CharacterID] = character.SkillPoints;
                }

                // Update training text
                var trainingBlock = FindTaggedDescendant<TextBlock>(card, "TrainingText");
                if (trainingBlock != null)
                    UpdateTrainingText(trainingBlock, character);

                // Update ESI status
                UpdateEsiStatus(card, character);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Overview fetch update error: {ex}");
            }
        }

        private void OnSecondTick()
        {
            try
            {
                _fetchingDotBright = !_fetchingDotBright;

                foreach (var card in GetAllCardButtons())
                {
                    var character = card.DataContext as Character;
                    if (character == null) continue;

                    // Tick training countdown every second
                    var trainingBlock = FindTaggedDescendant<TextBlock>(card, "TrainingText");
                    if (trainingBlock != null)
                        UpdateTrainingText(trainingBlock, character);

                    // Pulse the dot for actively fetching characters
                    var dot = FindTaggedDescendant<Border>(card, "EsiDot");
                    if (dot != null && character is CCPCharacter ccp)
                    {
                        bool anyUpdating = false;
                        try
                        {
                            foreach (var monitor in ccp.QueryMonitors)
                            {
                                if (monitor.IsUpdating) { anyUpdating = true; break; }
                            }
                        }
                        catch { /* ignore */ }

                        if (anyUpdating)
                            dot.Opacity = _fetchingDotBright ? 1.0 : 0.4;
                        else if (dot.Opacity < 1.0)
                            dot.Opacity = 1.0; // restore after fetching ends
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Overview tick error: {ex}");
            }
        }

        private void OnCollectionChanged()
        {
            try
            {
                // If groups are active, fall back to full rebuild (group membership may have changed)
                if (Settings.CharacterGroups.Count > 0)
                {
                    LoadData();
                    return;
                }

                var currentChars = AppServices.Characters.Where(c => c.Monitored).ToList();
                var existingCards = GetAllCardButtons().ToList();
                var existingIds = new HashSet<long>(
                    existingCards
                        .Select(b => (b.DataContext as Character)?.CharacterID ?? 0)
                        .Where(id => id != 0));

                var currentIds = new HashSet<long>(currentChars.Select(c => c.CharacterID));

                // Find the WrapPanel (should be the only/first one in ungrouped mode)
                var wrap = OverviewPanel.Children.OfType<WrapPanel>().FirstOrDefault();
                if (wrap == null)
                {
                    // No wrap panel yet — full rebuild
                    LoadData();
                    return;
                }

                // Remove deleted characters
                var toRemove = existingCards
                    .Where(b => b.DataContext is Character c && !currentIds.Contains(c.CharacterID))
                    .ToList();

                foreach (var card in toRemove)
                {
                    // Fade out then remove
                    card.Opacity = 0;
                    var timer = new DispatcherTimer(DispatcherPriority.Render)
                    {
                        Interval = TimeSpan.FromMilliseconds(350)
                    };
                    var capturedCard = card;
                    var capturedWrap = wrap;
                    timer.Tick += (_, _) =>
                    {
                        timer.Stop();
                        _animationTimers.Remove(timer);
                        capturedWrap.Children.Remove(capturedCard);
                    };
                    _animationTimers.Add(timer);
                    timer.Start();

                    if (card.DataContext is Character removed)
                    {
                        _prevBalances.Remove(removed.CharacterID);
                        _prevSkillPoints.Remove(removed.CharacterID);
                    }
                }

                // Add new characters
                var newChars = currentChars.Where(c => !existingIds.Contains(c.CharacterID)).ToList();
                int staggerBase = wrap.Children.Count;
                foreach (var character in newChars)
                {
                    var card = BuildCharacterCard(character, staggerBase);
                    wrap.Children.Add(card);
                    staggerBase++;

                    _prevBalances[character.CharacterID] = character.Balance;
                    _prevSkillPoints[character.CharacterID] = character.SkillPoints;
                }

                // Load portraits for any new cards
                if (newChars.Count > 0)
                {
                    Dispatcher.UIThread.Post(() => LoadPortraitsForCharacters(
                        newChars.Select(c => c.CharacterID).ToHashSet()),
                        DispatcherPriority.Background);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Overview collection change error: {ex}");
            }
        }

        #endregion

        #region Helpers

        private static void UpdateTrainingText(TextBlock textBlock, Character character)
        {
            if (PrivacyHelper.IsTrainingHidden)
            {
                textBlock.Text = $"Training: {PrivacyHelper.Mask}";
                textBlock.Foreground = (IBrush?)Application.Current?.FindResource("EveWarningYellowBrush") ?? Brushes.Yellow;
                return;
            }

            if (character is CCPCharacter ccp && ccp.IsTraining && ccp.CurrentlyTrainingSkill != null)
            {
                var skill = ccp.CurrentlyTrainingSkill;
                var remaining = skill.RemainingTime;
                string timeStr = remaining.TotalHours >= 24
                    ? $"{(int)remaining.TotalDays}d {remaining.Hours}h"
                    : remaining.TotalHours >= 1
                        ? $"{(int)remaining.TotalHours}h {remaining.Minutes}m"
                        : $"{remaining.Minutes}m {remaining.Seconds}s";
                textBlock.Text = $"Training: {skill.SkillName} {skill.Level} ({timeStr})";
                textBlock.Foreground = (IBrush?)Application.Current?.FindResource("EveWarningYellowBrush") ?? Brushes.Yellow;
            }
            else
            {
                textBlock.Text = "Paused";
                textBlock.Foreground = Brushes.Gray;
            }
        }

        private void UpdateEsiStatus(Button card, Character character)
        {
            var (dotColor, isFetching) = GetEsiStatus(character);

            var dot = FindTaggedDescendant<Border>(card, "EsiDot");
            if (dot != null)
            {
                dot.Background = dotColor;
                if (isFetching)
                    dot.Opacity = _fetchingDotBright ? 1.0 : 0.4;
                else
                    dot.Opacity = 1.0;
            }
        }

        private static void FlashTextBlock(TextBlock tb)
        {
            tb.Opacity = 0.5;
            // The DoubleTransition on Opacity handles the smooth fade back to 1.0
            Dispatcher.UIThread.Post(() => tb.Opacity = 1.0, DispatcherPriority.Render);
        }

        private Button? FindCardByCharacterId(long characterId)
        {
            foreach (var card in GetAllCardButtons())
            {
                if (card.DataContext is Character c && c.CharacterID == characterId)
                    return card;
            }
            return null;
        }

        private IEnumerable<Button> GetAllCardButtons()
        {
            foreach (var child in OverviewPanel.Children)
            {
                if (child is WrapPanel wrap)
                {
                    foreach (var item in wrap.Children)
                    {
                        if (item is Button btn && btn.DataContext is Character)
                            yield return btn;
                    }
                }
            }
        }

        private static T? FindTaggedDescendant<T>(Control root, string tag) where T : Control
        {
            foreach (var descendant in root.GetVisualDescendants())
            {
                if (descendant is T typed && typed.Tag is string s && s == tag)
                    return typed;
            }
            return null;
        }

        private static IBrush FindBrush(string resourceKey, IBrush fallback)
        {
            return (IBrush?)Application.Current?.FindResource(resourceKey) ?? fallback;
        }

        #endregion

        #region Portrait Loading

        private async void LoadPortraitsAndTraining()
        {
            try
            {
                var images = this.GetVisualDescendants().OfType<Image>()
                    .Where(img => img.Tag is long).ToList();

                foreach (var img in images)
                {
                    long charId = (long)img.Tag!;
                    try
                    {
                        var drawingImage = await ImageService.GetCharacterImageAsync(charId);
                        if (drawingImage != null)
                        {
                            var converted = DrawingImageToAvaloniaConverter.Instance.Convert(
                                drawingImage, typeof(Bitmap), null, CultureInfo.InvariantCulture);
                            if (converted is Bitmap bitmap)
                            {
                                (img.Source as IDisposable)?.Dispose();
                                img.Source = bitmap;
                            }
                        }
                    }
                    catch { /* portrait load failure is non-fatal */ }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Portrait load error: {ex}");
            }
        }

        private async void LoadPortraitsForCharacters(HashSet<long> characterIds)
        {
            try
            {
                var images = this.GetVisualDescendants().OfType<Image>()
                    .Where(img => img.Tag is long id && characterIds.Contains(id)).ToList();

                foreach (var img in images)
                {
                    long charId = (long)img.Tag!;
                    try
                    {
                        var drawingImage = await ImageService.GetCharacterImageAsync(charId);
                        if (drawingImage != null)
                        {
                            var converted = DrawingImageToAvaloniaConverter.Instance.Convert(
                                drawingImage, typeof(Bitmap), null, CultureInfo.InvariantCulture);
                            if (converted is Bitmap bitmap)
                            {
                                (img.Source as IDisposable)?.Dispose();
                                img.Source = bitmap;
                            }
                        }
                    }
                    catch { /* portrait load failure is non-fatal */ }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Portrait load error: {ex}");
            }
        }

        #endregion

        #region Click Handlers

        private void OnDeleteCharacter(object? sender, RoutedEventArgs e)
        {
            try
            {
                Character? character = null;
                if (sender is MenuItem { Tag: Character c })
                    character = c;

                if (character == null) return;

                var mainWindow = this.FindAncestorOfType<MainWindow>();
                mainWindow?.DeleteCharacterWithConfirmation(character);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error: {ex}");
            }
        }

        private void OnCharacterCardClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button btn && btn.DataContext is Character character)
                {
                    var mainWindow = this.FindAncestorOfType<MainWindow>();
                    mainWindow?.NavigateToCharacter(character);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Card click error: {ex}");
            }
        }

        #endregion
    }
}
