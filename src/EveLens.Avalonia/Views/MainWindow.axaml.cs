// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using EveLens.Avalonia.Converters;
using EveLens.Common.Helpers;
using EveLens.Avalonia.ViewModels;
using EveLens.Avalonia.Views.CharacterMonitor;
using EveLens.Avalonia.Views.Dialogs;
using EveLens.Avalonia.Views.PlanEditor;
using EveLens.Common;
using EveLens.Common.Enumerations;
using EveLens.Common.Models;
using EveLens.Common.Service;
using EveLens.Common.Services;
using EveLens.Common.ViewModels;
using EveLens.Core.Events;
using EveLens.Avalonia.Services;
using EveLens.Common.CustomEventArgs;
using EveLens.Common.Events;
using EveLens.Common.Notifications;
using EveLens.Common.SettingsObjects;

namespace EveLens.Avalonia.Views
{
    public partial class MainWindow : Window
    {
        private const string WindowLocationKey = "MainWindow";
        private PixelPoint _lastPosition;
        private double _lastWidth;
        private double _lastHeight;

        private readonly MainWindowViewModel _viewModel;
        private readonly List<ObservableCharacter> _observableCharacters = new();
        private readonly Dictionary<long, CharacterMonitorView> _cachedViews = new();
        private CharacterOverviewView? _overviewView;
        private Character? _selectedCharacter;
        private int _charSwitchCount;
        private Button? _selectedSlotButton;
        private bool _isPanning;
        private Point _panStart;
        private double _panStartOffset;
        private IDisposable? _tickSubscription;
        private IDisposable? _collectionChangedSub;
        private IDisposable? _monitoredChangedSub;
        private NotificationCenterViewModel? _notificationVm;
        private IDisposable? _notificationSentSub;
        private IDisposable? _privacyModeSub;

        public MainWindow()
        {
            InitializeComponent();
            RestoreWindowLocation();

            // On Linux/macOS, use PNG icon instead of .ico for better WM compatibility
            if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                    System.Runtime.InteropServices.OSPlatform.Windows))
            {
                try
                {
                    var uri = new Uri("avares://EveLens/Properties/EveLens.png");
                    Icon = new WindowIcon(global::Avalonia.Platform.AssetLoader.Open(uri));
                }
                catch { /* Fall back to .ico from AXAML */ }
            }

            if (AppServices.IsDebugBuild)
                Title += " [DEBUG]";

            _viewModel = new MainWindowViewModel();
            DataContext = _viewModel;

            _viewModel.RefreshCharacters();

            // Wire overview button and scroll handlers (once, before strip build)
            OverviewBtn.Click += (_, _) => SelectCharacter(null);
            WireScrollHandlers();

            BuildCharacterStrip();
            WireMenuItems();

            _tickSubscription = AppServices.EventAggregator?.Subscribe<SecondTickEvent>(
                e => Dispatcher.UIThread.Post(() => OnSecondTick(e)));

            _collectionChangedSub = AppServices.EventAggregator?.Subscribe<Common.Events.CharacterCollectionChangedEvent>(
                _ => Dispatcher.UIThread.Post(OnCharacterCollectionChanged));
            _monitoredChangedSub = AppServices.EventAggregator?.Subscribe<Common.Events.MonitoredCharacterCollectionChangedEvent>(
                _ => Dispatcher.UIThread.Post(OnCharacterCollectionChanged));

            // Wire notification center
            _notificationVm = new NotificationCenterViewModel();
            MarkReadBtn.Click += (_, _) => { _notificationVm.MarkAllRead(); RefreshNotificationUI(); };
            ClearAllBtn.Click += (_, _) => { _notificationVm.ClearAll(); RefreshNotificationUI(); NotificationBellBtn.Flyout?.Hide(); };
            _notificationVm.PropertyChanged += (_, _) =>
                Dispatcher.UIThread.Post(RefreshNotificationUI);
            RefreshNotificationUI();

            // Native OS toast notifications (cross-platform)
            _notificationSentSub = AppServices.EventAggregator?.Subscribe<NotificationSentEvent>(
                e => OnNotificationSent(e));

            // Wire privacy mode flyout checkboxes
            WirePrivacyCheckbox(PrivacyNameCb, PrivacyCategories.Name);
            WirePrivacyCheckbox(PrivacyCorpCb, PrivacyCategories.CorpAlliance);
            WirePrivacyCheckbox(PrivacyBalanceCb, PrivacyCategories.Balance);
            WirePrivacyCheckbox(PrivacySkillPointsCb, PrivacyCategories.SkillPoints);
            WirePrivacyCheckbox(PrivacyTrainingCb, PrivacyCategories.Training);
            WirePrivacyCheckbox(PrivacyRemapsCb, PrivacyCategories.Remaps);
            PrivacyToggleAllBtn.Click += (_, _) =>
            {
                AppServices.TogglePrivacyMode();
                SyncPrivacyCheckboxes();
                UpdatePrivacyIcon();
            };
            _privacyModeSub = AppServices.EventAggregator?.Subscribe<Common.Events.PrivacyModeChangedEvent>(
                _ => Dispatcher.UIThread.Post(RebuildCharacterStrip));

            // Clean up backup files from previous auto-update
            // Velopack handles auto-updates now — old UpdateManager disabled.
            // VelopackUpdateService starts background checks in AppServices.Bootstrap().
        }

        #region Portrait Strip

        private void BuildCharacterStrip()
        {
            try
            {
                _overviewView ??= new CharacterOverviewView();

                foreach (Character character in _viewModel.Characters)
                {
                    var observable = new ObservableCharacter(character);
                    _observableCharacters.Add(observable);

                    var slot = BuildCharacterSlot(character);
                    CharStrip.Children.Add(slot);
                }

                SelectCharacter(null);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error building character strip: {ex}");
            }
        }

        private Button BuildCharacterSlot(Character character)
        {
            // 32×32 portrait
            var portraitImage = new Image
            {
                Width = 32, Height = 32,
                Stretch = Stretch.UniformToFill
            };
            portraitImage.Tag = character.CharacterID;

            var portraitRing = new Border
            {
                Width = 36, Height = 36,
                ClipToBounds = true,
                Child = portraitImage
            };
            portraitRing.Classes.Add("portrait-ring");

            // ESI status dot (bottom-right)
            var (dotColor, _) = GetCharacterEsiStatus(character);
            var esiDot = new Border
            {
                Width = 10, Height = 10,
                CornerRadius = new CornerRadius(5),
                Background = dotColor,
                BorderBrush = FindStripBrush("EveBackgroundDarkBrush", Brushes.Black),
                BorderThickness = new Thickness(1.5),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, -1, -1),
                Tag = "EsiDot"
            };

            var portraitGrid = new Grid
            {
                Width = 36, Height = 36,
                Children = { portraitRing, esiDot }
            };

            // First name below portrait
            var nameText = new TextBlock
            {
                Text = PrivacyHelper.IsNameHidden ? PrivacyHelper.Mask : character.Name.Split(' ')[0],
                FontSize = 9,
                MaxWidth = 50,
                TextAlignment = TextAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Foreground = FindStripBrush("EveTextSecondaryBrush", Brushes.Gray),
                HorizontalAlignment = HorizontalAlignment.Center,
                Tag = "SlotName"
            };

            var slotPanel = new StackPanel
            {
                Spacing = 2,
                HorizontalAlignment = HorizontalAlignment.Center,
                Children = { portraitGrid, nameText }
            };

            var btn = new Button
            {
                Content = slotPanel,
                DataContext = character,
                Width = 56,
                [ToolTip.TipProperty] = BuildCharacterTooltip(character)
            };
            btn.Classes.Add("char-slot");
            btn.Click += (_, _) => SelectCharacter(character);

            // Load portrait async
            LoadSlotPortrait(portraitImage, character.CharacterID);

            return btn;
        }

        /// <summary>
        /// Selects a character (or overview if null) and swaps the main content area.
        /// </summary>
        public void SelectCharacter(Character? character)
        {
            // Remove highlight from previous selection
            if (_selectedSlotButton != null)
            {
                _selectedSlotButton.Classes.Remove("selected");
                var prevRing = FindPortraitRing(_selectedSlotButton);
                prevRing?.Classes.Remove("selected");
                var prevName = FindSlotName(_selectedSlotButton);
                if (prevName != null)
                    prevName.Foreground = FindStripBrush("EveTextSecondaryBrush", Brushes.Gray);
            }

            _selectedCharacter = character;

            if (character == null)
            {
                // Show Overview
                MainContent.Content = _overviewView;
                OverviewBtn.Classes.Add("selected");
                _selectedSlotButton = OverviewBtn;

                var ring = FindPortraitRing(OverviewBtn);
                ring?.Classes.Add("selected");
                var name = FindSlotName(OverviewBtn);
                if (name != null)
                    name.Foreground = FindStripBrush("EveAccentPrimaryBrush", Brushes.Gold);
            }
            else
            {
                // Find and highlight the slot button
                var slotBtn = CharStrip.Children.OfType<Button>()
                    .FirstOrDefault(b => b.DataContext is Character c && c.CharacterID == character.CharacterID);

                if (slotBtn != null)
                {
                    slotBtn.Classes.Add("selected");
                    _selectedSlotButton = slotBtn;

                    var ring = FindPortraitRing(slotBtn);
                    ring?.Classes.Add("selected");
                    var nameText = FindSlotName(slotBtn);
                    if (nameText != null)
                        nameText.Foreground = FindStripBrush("EveAccentPrimaryBrush", Brushes.Gold);

                    // Scroll the strip so the selected slot is visible
                    ScrollSlotIntoView(slotBtn);
                }

                // Get or create CharacterMonitorView
                if (!_cachedViews.TryGetValue(character.CharacterID, out var monitorView))
                {
                    var observable = _observableCharacters.FirstOrDefault(
                        oc => oc.Character.CharacterID == character.CharacterID);
                    if (observable == null)
                    {
                        observable = new ObservableCharacter(character);
                        _observableCharacters.Add(observable);
                    }

                    monitorView = new CharacterMonitorView { DataContext = observable };
                    _cachedViews[character.CharacterID] = monitorView;
                }

                MainContent.Content = monitorView;

                // Hint GC to collect after switching through several characters.
                // On high-memory machines, .NET GC won't collect unless nudged.
                _charSwitchCount++;
                if (_charSwitchCount >= 6)
                {
                    _charSwitchCount = 0;
                    GC.Collect(2, GCCollectionMode.Optimized, false, true);
                }
            }

            // Update plan menu state
            bool hasChar = _selectedCharacter != null;
            NewPlanMenuItem.IsEnabled = hasChar;
            ManagePlansMenuItem.IsEnabled = hasChar;
            ImportPlanMenuItem.IsEnabled = hasChar;
            CreateFromQueueMenuItem.IsEnabled = hasChar;
        }

        /// <summary>
        /// Public navigation entry point — called from overview card clicks.
        /// </summary>
        public void NavigateToCharacter(Character character)
        {
            SelectCharacter(character);
        }

        private void RebuildCharacterStrip()
        {
            foreach (var oc in _observableCharacters) oc.Dispose();
            _observableCharacters.Clear();
            _cachedViews.Clear();
            CharStrip.Children.Clear();
            _selectedSlotButton = null;
            _selectedCharacter = null;

            _viewModel.RefreshCharacters();
            BuildCharacterStrip();
        }

        private void WireScrollHandlers()
        {
            // Arrow buttons
            ScrollLeftBtn.Click += (_, _) =>
            {
                AnimateStripScroll(CharStripScroll.Offset.X - 200);
            };
            ScrollRightBtn.Click += (_, _) =>
            {
                AnimateStripScroll(CharStripScroll.Offset.X + 200);
            };

            // Mouse wheel → horizontal scroll
            CharStripScroll.PointerWheelChanged += (_, e) =>
            {
                double delta = e.Delta.Y * 50;
                AnimateStripScroll(CharStripScroll.Offset.X - delta);
                e.Handled = true;
            };

            // Drag-to-scroll (same pattern as Employment History timeline)
            CharStripScroll.PointerPressed += OnStripPointerPressed;
            CharStripScroll.PointerMoved += OnStripPointerMoved;
            CharStripScroll.PointerReleased += OnStripPointerReleased;
        }

        private void OnStripPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var props = e.GetCurrentPoint(CharStripScroll).Properties;
            if (props.IsLeftButtonPressed || props.IsMiddleButtonPressed)
            {
                _isPanning = true;
                _panStart = e.GetPosition(CharStripScroll);
                _panStartOffset = CharStripScroll.Offset.X;
                e.Pointer.Capture(CharStripScroll);
            }
        }

        private void OnStripPointerMoved(object? sender, PointerEventArgs e)
        {
            if (!_isPanning) return;
            var current = e.GetPosition(CharStripScroll);
            double delta = _panStart.X - current.X;
            double maxOffset = Math.Max(0, CharStripScroll.Extent.Width - CharStripScroll.Viewport.Width);
            CharStripScroll.Offset = new Vector(
                Math.Max(0, Math.Min(_panStartOffset + delta, maxOffset)), 0);
        }

        private void OnStripPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (!_isPanning) return;
            _isPanning = false;
            e.Pointer.Capture(null);
        }

        private void AnimateStripScroll(double targetX)
        {
            double maxOffset = Math.Max(0, CharStripScroll.Extent.Width - CharStripScroll.Viewport.Width);
            CharStripScroll.Offset = new Vector(
                Math.Max(0, Math.Min(targetX, maxOffset)), 0);
        }

        /// <summary>
        /// Scrolls the portrait strip so the given slot button is visible.
        /// </summary>
        private void ScrollSlotIntoView(Button slotBtn)
        {
            // Get the slot's position relative to the StackPanel
            int index = CharStrip.Children.IndexOf(slotBtn);
            if (index < 0) return;

            // Each slot is ~56px wide + 2px spacing
            double slotLeft = index * 58;
            double slotRight = slotLeft + 56;
            double viewLeft = CharStripScroll.Offset.X;
            double viewRight = viewLeft + CharStripScroll.Viewport.Width;

            if (slotLeft < viewLeft)
            {
                // Slot is off the left edge — scroll left with some padding
                AnimateStripScroll(slotLeft - 10);
            }
            else if (slotRight > viewRight)
            {
                // Slot is off the right edge — scroll right with some padding
                AnimateStripScroll(slotRight - CharStripScroll.Viewport.Width + 10);
            }
        }

        private async void LoadSlotPortrait(Image portraitImage, long characterId)
        {
            try
            {
                var drawingImage = await ImageService.GetCharacterImageAsync(characterId);
                if (drawingImage != null)
                {
                    var converted = DrawingImageToAvaloniaConverter.Instance.Convert(
                        drawingImage, typeof(Bitmap), null, CultureInfo.InvariantCulture);
                    if (converted is Bitmap bitmap)
                    {
                        (portraitImage.Source as IDisposable)?.Dispose();
                        portraitImage.Source = bitmap;
                    }
                }
            }
            catch
            {
                // Portrait load failure is non-fatal
            }
        }

        private static string BuildCharacterTooltip(Character character)
        {
            var parts = new List<string>();
            parts.Add(PrivacyHelper.IsNameHidden ? PrivacyHelper.Mask : character.Name);
            parts.Add(PrivacyHelper.IsBalanceHidden ? $"ISK: {PrivacyHelper.Mask}" : $"ISK: {character.Balance:N2}");
            parts.Add(PrivacyHelper.IsSkillPointsHidden ? $"SP: {PrivacyHelper.Mask}" : $"SP: {character.SkillPoints:N0}");

            if (PrivacyHelper.IsTrainingHidden)
            {
                parts.Add($"Training: {PrivacyHelper.Mask}");
            }
            else if (character is CCPCharacter ccp && ccp.IsTraining && ccp.CurrentlyTrainingSkill != null)
            {
                var skill = ccp.CurrentlyTrainingSkill;
                var remaining = skill.RemainingTime;
                string timeStr = remaining.TotalHours >= 24
                    ? $"{(int)remaining.TotalDays}d {remaining.Hours}h"
                    : remaining.TotalHours >= 1
                        ? $"{(int)remaining.TotalHours}h {remaining.Minutes}m"
                        : $"{remaining.Minutes}m {remaining.Seconds}s";
                parts.Add($"Training: {skill.SkillName} {skill.Level} ({timeStr})");
            }
            else
            {
                parts.Add("Training: Paused");
            }

            return string.Join("\n", parts);
        }

        private static (IBrush dotColor, bool isFetching) GetCharacterEsiStatus(Character character)
        {
            IBrush yellowBrush = FindStripBrush("EveWarningYellowBrush", Brushes.Yellow);
            IBrush greenBrush = FindStripBrush("EveSuccessGreenBrush", Brushes.LimeGreen);
            IBrush redBrush = FindStripBrush("EveErrorRedBrush", Brushes.Red);
            IBrush grayBrush = FindStripBrush("EveTextDisabledBrush", Brushes.Gray);

            if (character is not CCPCharacter ccp)
                return (grayBrush, false);

            try
            {
                bool anyUpdating = false;
                bool anyNoKey = false;
                foreach (var monitor in ccp.QueryMonitors)
                {
                    if (monitor.IsUpdating) anyUpdating = true;
                    if (monitor.Status == QueryStatus.NoESIKey) anyNoKey = true;
                }

                if (anyUpdating) return (yellowBrush, true);
                if (anyNoKey || ccp.QueryMonitors.HasErrors) return (redBrush, false);
                if (ccp.HasCompletedFirstUpdate) return (greenBrush, false);
            }
            catch { /* non-fatal */ }

            return (grayBrush, false);
        }

        private static IBrush FindStripBrush(string resourceKey, IBrush fallback)
        {
            return (IBrush?)Application.Current?.FindResource(resourceKey) ?? fallback;
        }

        private static Border? FindPortraitRing(Button btn)
        {
            return btn.GetVisualDescendants().OfType<Border>()
                .FirstOrDefault(b => b.Classes.Contains("portrait-ring"));
        }

        private static TextBlock? FindSlotName(Button btn)
        {
            return btn.GetVisualDescendants().OfType<TextBlock>()
                .FirstOrDefault(t => t.Tag is string s && s == "SlotName");
        }

        #endregion

        private void WireMenuItems()
        {
            // File menu
            AddCharMenuItem.Click += OnAddCharacterClick;
            CreateBlankCharMenuItem.Click += OnCreateBlankCharacterClick;
            ManageCharsMenuItem.Click += OnManageCharactersClick;
            ManageGroupsMenuItem.Click += OnManageGroupsClick;
            RestoreSettingsMenuItem.Click += OnRestoreSettingsClick;
            SaveSettingsMenuItem.Click += OnSaveSettingsClick;
            ResetSettingsMenuItem.Click += OnResetSettingsClick;
            SettingsMenuItem.Click += OnSettingsClick;
            ExitMenuItem.Click += OnExitClick;

            // Plans menu
            NewPlanMenuItem.Click += OnNewPlanClick;
            ManagePlansMenuItem.Click += OnManagePlansClick;
            ImportPlanMenuItem.Click += OnImportPlanClick;
            CreateFromQueueMenuItem.Click += OnCreateFromQueueClick;

            // Tools menu
            CharCompMenuItem.Click += OnCharCompClick;
            SkillConstellationMenuItem.Click += OnSkillConstellationClick;
            ClearCacheMenuItem.Click += OnClearCacheClick;

            // Help menu
            CheckUpdatesMenuItem.Click += OnCheckUpdatesClick;
            UserGuideMenuItem.Click += OnUserGuideClick;
            ReportIssueMenuItem.Click += OnReportIssueClick;
            AboutMenuItem.Click += OnAboutClick;

            // Debug menu (only in debug builds)
            if (AppServices.IsDebugBuild)
                BuildDebugMenu();
        }

        private void BuildDebugMenu()
        {
            var debugMenu = new MenuItem { Header = "_Debug" };

            // ── Velopack Update Test ──
            var updateTestItem = new MenuItem { Header = "Check for Updates (Velopack)" };
            updateTestItem.Click += async (_, _) =>
            {
                try
                {
                    var svc = AppServices.VelopackUpdate;
                    string info = $"Installed: {svc?.IsInstalled}\n"
                        + $"Version: {svc?.CurrentVersion ?? "dev"}\n"
                        + $"Channel: {svc?.Channel}\n"
                        + $"Update ready: {svc?.IsUpdateReady}\n"
                        + $"Pending: {svc?.PendingVersion ?? "none"}";
                    await ShowMessageDialog("Velopack Status", info);
                }
                catch (Exception ex) { Debug.WriteLine($"Error: {ex}"); }
            };

            // ── Notification Events ──
            var notifySubMenu = new MenuItem { Header = "Fire Notification Events" };

            var fireSkillComplete = new MenuItem { Header = "Skill Completed" };
            fireSkillComplete.Click += (_, _) =>
            {
                try
                {
                    var character = GetSelectedCharacter() ?? _viewModel.Characters.FirstOrDefault();
                    string charName = character?.Name ?? "Test Pilot";
                    var sender = (object?)character ?? AppServices.EVEServer ?? new object();
                    var args = new Common.Notifications.NotificationEventArgs(sender, Common.Notifications.NotificationCategory.SkillCompletion)
                    {
                        Description = $"{charName} has completed training Caldari Battleship V.",
                        Priority = Common.Notifications.NotificationPriority.Information
                    };
                    AppServices.EventAggregator?.Publish(new NotificationSentEvent(args));
                }
                catch (Exception ex) { Debug.WriteLine($"Error: {ex}"); }
            };

            var fireAccountExpiry = new MenuItem { Header = "Account Expiring" };
            fireAccountExpiry.Click += (_, _) =>
            {
                try
                {
                    var character = GetSelectedCharacter() ?? _viewModel.Characters.FirstOrDefault();
                    if (character == null) return;
                    var args = new Common.Notifications.NotificationEventArgs(character, Common.Notifications.NotificationCategory.AccountExpiration)
                    {
                        Description = $"{character.Name}'s Omega status expires in 3 days.",
                        Priority = Common.Notifications.NotificationPriority.Warning
                    };
                    AppServices.EventAggregator?.Publish(new NotificationSentEvent(args));
                }
                catch (Exception ex) { Debug.WriteLine($"Error: {ex}"); }
            };

            var fireServerStatus = new MenuItem { Header = "Server Status Changed" };
            fireServerStatus.Click += (_, _) =>
            {
                try
                {
                    AppServices.EventAggregator?.Publish(Common.Events.ServerStatusUpdatedEvent.Instance);
                }
                catch (Exception ex) { Debug.WriteLine($"Error: {ex}"); }
            };

            var fireSettingsChanged = new MenuItem { Header = "Settings Changed" };
            fireSettingsChanged.Click += (_, _) =>
            {
                try
                {
                    AppServices.EventAggregator?.Publish(Common.Events.SettingsChangedEvent.Instance);
                }
                catch (Exception ex) { Debug.WriteLine($"Error: {ex}"); }
            };

            notifySubMenu.Items.Add(fireSkillComplete);
            notifySubMenu.Items.Add(fireAccountExpiry);
            notifySubMenu.Items.Add(fireServerStatus);
            notifySubMenu.Items.Add(fireSettingsChanged);

            // ── Dialogs ──
            var dialogSubMenu = new MenuItem { Header = "Open Dialogs" };

            var openSettings = new MenuItem { Header = "Settings Window" };
            openSettings.Click += OnSettingsClick;

            var openAbout = new MenuItem { Header = "About Window" };
            openAbout.Click += OnAboutClick;

            var openSkillConst = new MenuItem { Header = "Skill Constellation" };
            openSkillConst.Click += OnSkillConstellationClick;

            dialogSubMenu.Items.Add(openSettings);
            dialogSubMenu.Items.Add(openAbout);
            dialogSubMenu.Items.Add(openSkillConst);

            // ── Character Events ──
            var charSubMenu = new MenuItem { Header = "Fire Character Events" };

            var fireCharUpdated = new MenuItem { Header = "Character Updated" };
            fireCharUpdated.Click += (_, _) =>
            {
                try
                {
                    var character = GetSelectedCharacter() ?? _viewModel.Characters.FirstOrDefault();
                    if (character == null) return;
                    AppServices.EventAggregator?.Publish(new Common.Events.CharacterUpdatedEvent(character));
                }
                catch (Exception ex) { Debug.WriteLine($"Error: {ex}"); }
            };

            var fireCollectionChanged = new MenuItem { Header = "Collection Changed" };
            fireCollectionChanged.Click += (_, _) =>
            {
                try
                {
                    AppServices.EventAggregator?.Publish(Common.Events.CharacterCollectionChangedEvent.Instance);
                }
                catch (Exception ex) { Debug.WriteLine($"Error: {ex}"); }
            };

            var fireQueueUpdated = new MenuItem { Header = "Skill Queue Updated" };
            fireQueueUpdated.Click += (_, _) =>
            {
                try
                {
                    var character = GetSelectedCharacter() ?? _viewModel.Characters.FirstOrDefault();
                    if (character == null) return;
                    AppServices.EventAggregator?.Publish(new Common.Events.CharacterSkillQueueUpdatedEvent(character));
                }
                catch (Exception ex) { Debug.WriteLine($"Error: {ex}"); }
            };

            charSubMenu.Items.Add(fireCharUpdated);
            charSubMenu.Items.Add(fireCollectionChanged);
            charSubMenu.Items.Add(fireQueueUpdated);

            // ── Diagnostic Stream ──
            var diagStreamItem = new MenuItem { Header = "Diagnostic Stream" };
            diagStreamItem.Click += (_, _) =>
            {
                try
                {
                    var window = new Dialogs.DiagnosticStreamWindow();
                    window.Show(this);
                }
                catch (Exception ex) { Debug.WriteLine($"Diag stream error: {ex}"); }
            };

            // ── Utilities ──
            var utilSubMenu = new MenuItem { Header = "Utilities" };

            var openDataFolder = new MenuItem { Header = "Open Data Folder" };
            openDataFolder.Click += (_, _) =>
            {
                try
                {
                    var path = AppServices.ApplicationPaths.DataDirectory;
                    if (!string.IsNullOrEmpty(path) && System.IO.Directory.Exists(path))
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
                }
                catch (Exception ex) { Debug.WriteLine($"Error opening data folder: {ex}"); }
            };

            var healthStatus = new MenuItem { Header = "Health Tracker Status" };
            healthStatus.Click += (_, _) =>
            {
                try
                {
                    var tracker = AppServices.HealthTracker;
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine("=== Endpoint Health Tracker ===\n");
                    foreach (var character in _viewModel.Characters)
                    {
                        if (character is not CCPCharacter ccp) continue;
                        var summary = tracker.GetCharacterHealth(ccp.CharacterID);
                        sb.AppendLine($"{ccp.Name}: {summary.OverallHealth}");
                        sb.AppendLine($"  Healthy: {summary.HealthyCount}  Degraded: {summary.DegradedCount}  Failing: {summary.FailingCount}");
                        if (summary.FailingSince.HasValue)
                            sb.AppendLine($"  Failing since: {summary.FailingSince:HH:mm:ss}");
                        sb.AppendLine();
                    }
                    sb.AppendLine($"Scheduler queue depth: {AppServices.EsiScheduler?.QueueDepth}");
                    sb.AppendLine($"Active fetches: {AppServices.EsiScheduler?.ActiveFetches}");
                    var nextFetch = AppServices.EsiScheduler?.GetNextFetchTime();
                    if (nextFetch.HasValue)
                        sb.AppendLine($"Next fetch: {nextFetch:HH:mm:ss} ({(nextFetch.Value - DateTime.UtcNow).TotalSeconds:F0}s)");

                    Debug.WriteLine(sb.ToString());
                    AppServices.TraceService?.Trace(sb.ToString());
                    // Show in a simple dialog
                    _ = ShowMessageDialog("Health Tracker Status", sb.ToString());
                }
                catch (Exception ex) { Debug.WriteLine($"Error: {ex}"); }
            };

            utilSubMenu.Items.Add(openDataFolder);
            utilSubMenu.Items.Add(healthStatus);

            // ── Assemble ──
            debugMenu.Items.Add(diagStreamItem);
            debugMenu.Items.Add(new Separator());
            debugMenu.Items.Add(updateTestItem);
            debugMenu.Items.Add(notifySubMenu);
            debugMenu.Items.Add(charSubMenu);
            debugMenu.Items.Add(new Separator());
            debugMenu.Items.Add(dialogSubMenu);
            debugMenu.Items.Add(utilSubMenu);
            MainMenu.Items.Add(debugMenu);
        }

        private void OnSecondTick(SecondTickEvent _)
        {
            try
            {
                EveTimeText.Text = $"EVE Time: {DateTime.UtcNow:HH:mm}";
                var server = AppServices.EVEServer;
                ServerStatusText.Text = server?.IsOnline == true ? "Server: Online" : "Server: Offline";
                UpdateEsiCountdown();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating status: {ex}");
            }
        }

        /// <summary>
        /// Rebuilds the portrait strip when the character collection changes via any path
        /// (delete from overview, manage characters dialog, ESI key revocation, etc.).
        /// </summary>
        private void OnCharacterCollectionChanged()
        {
            try
            {
                RebuildCharacterStrip();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error handling collection change: {ex}");
            }
        }

        private void UpdateEsiCountdown()
        {
            try
            {
                var now = DateTime.UtcNow;
                var scheduler = AppServices.EsiScheduler;
                int fetchingCount = scheduler?.ActiveFetches ?? 0;

                // Read next fetch time directly from the scheduler's priority queue.
                // This is always accurate — no stale QueryMonitor reconstruction.
                DateTime? nextFetch = scheduler?.GetNextFetchTime();

                // Left indicator: what's happening now
                if (fetchingCount > 0)
                {
                    NextUpdateText.Text = $"ESI: {fetchingCount} fetching...";
                    NextUpdateText.Foreground = Brushes.LimeGreen;
                }
                else if (nextFetch.HasValue && nextFetch.Value > now)
                {
                    var remaining = nextFetch.Value - now;
                    string timeStr = remaining.TotalMinutes >= 1
                        ? $"{(int)remaining.TotalMinutes}m {remaining.Seconds}s"
                        : $"{remaining.Seconds}s";
                    NextUpdateText.Text = $"Next refresh in {timeStr}";
                    NextUpdateText.Foreground = Brushes.Gold;
                }
                else if (nextFetch.HasValue)
                {
                    // Next fetch is overdue (in the past) — scheduler will dispatch soon
                    NextUpdateText.Text = "ESI: refreshing...";
                    NextUpdateText.Foreground = Brushes.LimeGreen;
                }
                else
                {
                    NextUpdateText.Text = "ESI: idle";
                    NextUpdateText.Foreground = Brushes.Gray;
                }

                // Right indicator: last refresh time (track from MonitorFetchCompletedEvent)
                DateTime lastCompleted = DateTime.MinValue;
                foreach (var character in _viewModel.Characters)
                {
                    if (character is not CCPCharacter ccp) continue;
                    foreach (var monitor in ccp.QueryMonitors)
                    {
                        if (monitor.LastUpdate > lastCompleted && monitor.LastUpdate.Year > 2000)
                            lastCompleted = monitor.LastUpdate;
                    }
                }

                if (lastCompleted > DateTime.MinValue)
                {
                    var ago = now - lastCompleted;
                    string agoStr = ago.TotalSeconds < 60 ? $"{(int)ago.TotalSeconds}s ago"
                        : ago.TotalMinutes < 60 ? $"{(int)ago.TotalMinutes}m ago"
                        : $"{(int)ago.TotalHours}h ago";
                    EsiActivityText.Text = $"Last refresh: {agoStr}";
                }
                else
                {
                    EsiActivityText.Text = "";
                }
            }
            catch
            {
                // Non-critical — don't let countdown crash the app
            }
        }

        private async void OnRestoreSettingsClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                var fileTypes = new[]
                {
                    new FilePickerFileType("JSON Settings") { Patterns = new[] { "*.json" } },
                    new FilePickerFileType("XML Settings") { Patterns = new[] { "*.xml" } },
                    new FilePickerFileType("Backup Files") { Patterns = new[] { "*.bak" } },
                    new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
                };

                var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Restore Settings",
                    AllowMultiple = false,
                    FileTypeFilter = fileTypes
                });

                if (files.Count == 0) return;

                string path = files[0].Path.LocalPath;
                await Common.Settings.RestoreAsync(path);
                RebuildCharacterStrip();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error restoring settings: {ex}");
            }
        }

        private async void OnSaveSettingsClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                var fileTypes = new[]
                {
                    new FilePickerFileType("JSON Settings") { Patterns = new[] { "*.json" } },
                    new FilePickerFileType("XML Settings") { Patterns = new[] { "*.xml" } }
                };

                var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Save Settings",
                    DefaultExtension = "json",
                    SuggestedFileName = "EveLens-settings-backup",
                    FileTypeChoices = fileTypes
                });

                if (file == null) return;

                string path = file.Path.LocalPath;
                await Common.Settings.CopySettingsAsync(path);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving settings: {ex}");
            }
        }

        private async void OnResetSettingsClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                bool confirmed = await ShowConfirmationDialog(
                    "Reset Settings",
                    "Reset all settings to their defaults? This cannot be undone.");

                if (confirmed)
                {
                    await Common.Settings.ResetAsync();
                    RebuildCharacterStrip();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error resetting settings: {ex}");
            }
        }

        private void OnExitClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                AppServices.ApplicationLifecycle.Exit();
            }
            catch
            {
                Close();
            }
        }

        private async void OnSettingsClick(object? sender, RoutedEventArgs e)
        {
            Window? errDialog = null;
            try
            {
                // Step 1: prove the click fires
                Debug.WriteLine("Settings click fired");

                // Step 2: try creating the window
                SettingsWindow? settingsWindow = null;
                try
                {
                    settingsWindow = new SettingsWindow();
                }
                catch (Exception ctorEx)
                {
                    errDialog = new Window
                    {
                        Title = "Settings Constructor Error",
                        Width = 600, Height = 400,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Content = new ScrollViewer
                        {
                            Content = new TextBlock
                            {
                                Text = $"SettingsWindow constructor failed:\n\n{ctorEx}",
                                TextWrapping = TextWrapping.Wrap,
                                FontSize = 11,
                                Margin = new Thickness(16)
                            }
                        }
                    };
                    await errDialog.ShowDialog(this);
                    return;
                }

                // Step 3: show it
                await settingsWindow.ShowDialog(this);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error opening settings: {ex}");
                try
                {
                    errDialog = new Window
                    {
                        Title = "Settings Error",
                        Width = 600, Height = 400,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Content = new ScrollViewer
                        {
                            Content = new TextBlock
                            {
                                Text = $"Settings failed:\n\n{ex}",
                                TextWrapping = TextWrapping.Wrap,
                                FontSize = 11,
                                Margin = new Thickness(16)
                            }
                        }
                    };
                    await errDialog.ShowDialog(this);
                }
                catch { }
            }
        }

        private async void OnAboutClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                var aboutWindow = new AboutWindow();
                await aboutWindow.ShowDialog(this);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error opening about: {ex}");
            }
        }

        private async void OnAddCharacterClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new AddCharacterWindow();
                await dialog.ShowDialog(this);
                if (dialog.CharacterImported)
                    RebuildCharacterStrip();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex}");
            }
        }

        private async void OnCreateBlankCharacterClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new CreateBlankCharacterWindow();
                await dialog.ShowDialog(this);
                if (dialog.CharacterCreated)
                    RebuildCharacterStrip();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating blank character: {ex}");
            }
        }

        private async void OnManageCharactersClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                var characters = AppServices.Characters.Where(c => c.Monitored).ToList();
                if (characters.Count == 0)
                {
                    var emptyDialog = new Window
                    {
                        Title = "Manage Characters",
                        Width = 320, Height = 130,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Content = new TextBlock
                        {
                            Text = "No characters to manage. Use Add Character to get started.",
                            VerticalAlignment = VerticalAlignment.Center,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            TextWrapping = TextWrapping.Wrap,
                            Margin = new Thickness(20)
                        }
                    };
                    await emptyDialog.ShowDialog(this);
                    return;
                }

                var listBox = new ListBox
                {
                    ItemsSource = characters.Select(c => c.Name).ToList(),
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 0, 8)
                };
                listBox.SelectedIndex = 0;

                var deleteBtn = new Button
                {
                    Content = "Delete Selected",
                    FontSize = 11,
                    Padding = new Thickness(12, 5),
                    CornerRadius = new CornerRadius(12),
                    Foreground = Brushes.Red,
                    HorizontalAlignment = HorizontalAlignment.Right
                };

                var dialog = new Window
                {
                    Title = "Manage Characters",
                    Width = 360, Height = 350,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Content = new DockPanel
                    {
                        Margin = new Thickness(12),
                        Children =
                        {
                            new TextBlock
                            {
                                Text = "Select a character:",
                                FontSize = 12,
                                Margin = new Thickness(0, 0, 0, 8),
                                [DockPanel.DockProperty] = Dock.Top
                            },
                            new Panel
                            {
                                Children = { deleteBtn },
                                HorizontalAlignment = HorizontalAlignment.Right,
                                [DockPanel.DockProperty] = Dock.Bottom
                            },
                            listBox
                        }
                    }
                };

                deleteBtn.Click += async (_, _) =>
                {
                    try
                    {
                        int idx = listBox.SelectedIndex;
                        if (idx < 0 || idx >= characters.Count) return;

                        var character = characters[idx];
                        bool confirmed = await ShowConfirmationDialog(
                            "Delete Character",
                            $"Delete {character.Name}? This will remove the character and all associated ESI keys.");

                        if (confirmed)
                        {
                            try
                            {
                                AppServices.Characters.Remove(character);
                            }
                            catch (Exception rmEx)
                            {
                                Debug.WriteLine($"Error during character removal: {rmEx}");
                            }

                            // Update the dialog's local list and UI
                            characters.RemoveAt(idx);
                            listBox.ItemsSource = characters.Select(c => c.Name).ToList();
                            if (characters.Count == 0)
                                dialog.Close();
                            else
                                listBox.SelectedIndex = Math.Min(idx, characters.Count - 1);

                            // Always rebuild regardless of whether Remove threw
                            RebuildCharacterStrip();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error deleting character: {ex}");
                    }
                };

                await dialog.ShowDialog(this);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex}");
            }
        }

        /// <summary>
        /// Returns the character currently selected in the portrait strip, or null if Overview is active.
        /// </summary>
        private Character? GetSelectedCharacter()
        {
            return _selectedCharacter;
        }

        private async void OnNewPlanClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                var character = GetSelectedCharacter();
                if (character == null)
                {
                    var dialog = new Window
                    {
                        Title = "No Character",
                        Width = 320, Height = 130,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Content = new TextBlock
                        {
                            Text = _observableCharacters.Count > 0
                            ? "Select a character tab first."
                            : "Add a character before creating a plan.",
                            VerticalAlignment = VerticalAlignment.Center,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(20)
                        }
                    };
                    await dialog.ShowDialog(this);
                    return;
                }

                // Prompt for plan name
                int planCount = character.Plans.Count;
                string defaultName = character.Plans.GetUniqueName($"Plan {planCount + 1}");
                var nameBox = new TextBox
                {
                    Text = defaultName,
                    FontSize = 12,
                    Margin = new Thickness(0, 8, 0, 0),
                    Watermark = "Enter plan name..."
                };

                var errorText = new TextBlock
                {
                    Text = "A plan with this name already exists.",
                    FontSize = 10,
                    Foreground = FindStripBrush("EveErrorRedBrush", Brushes.Red),
                    Margin = new Thickness(0, 4, 0, 0),
                    IsVisible = false
                };

                var createBtn = new Button
                {
                    Content = "Create",
                    FontSize = 11,
                    Padding = new Thickness(12, 5),
                    CornerRadius = new CornerRadius(12),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 12, 0, 0)
                };

                nameBox.TextChanged += (_, _) =>
                {
                    string? current = nameBox.Text?.Trim();
                    bool isEmpty = string.IsNullOrEmpty(current);
                    bool isDuplicate = !isEmpty && character.Plans.ContainsName(current!);
                    errorText.IsVisible = isDuplicate;
                    createBtn.IsEnabled = !isEmpty && !isDuplicate;
                };

                string? planName = null;
                var nameDialog = new Window
                {
                    Title = "New Plan",
                    Width = 340, Height = 185,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Content = new StackPanel
                    {
                        Margin = new Thickness(16),
                        Children =
                        {
                            new TextBlock { Text = "Plan name:", FontSize = 12 },
                            nameBox,
                            errorText,
                            createBtn
                        }
                    }
                };

                nameBox.AttachedToVisualTree += (_, _) => nameBox.SelectAll();
                createBtn.Click += (_, _) =>
                {
                    planName = nameBox.Text?.Trim();
                    if (!string.IsNullOrEmpty(planName) && !character.Plans.ContainsName(planName))
                        nameDialog.Close();
                };

                await nameDialog.ShowDialog(this);
                if (string.IsNullOrEmpty(planName)) return;

                var plan = new Plan(character) { Name = planName };
                character.Plans.Add(plan);

                var editorWindow = new PlanEditorWindow
                {
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                editorWindow.Initialize(plan, character);
                editorWindow.Show(this);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating plan: {ex}");
            }
        }

        private async void OnManagePlansClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                var character = GetSelectedCharacter();
                if (character == null)
                {
                    var dialog = new Window
                    {
                        Title = "No Character",
                        Width = 320, Height = 130,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Content = new TextBlock
                        {
                            Text = _observableCharacters.Count > 0
                                ? "Select a character tab first."
                                : "Add a character before managing plans.",
                            VerticalAlignment = VerticalAlignment.Center,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            TextWrapping = TextWrapping.Wrap,
                            Margin = new Thickness(20)
                        }
                    };
                    await dialog.ShowDialog(this);
                    return;
                }

                var managePlansWindow = new ManagePlansWindow();
                managePlansWindow.Initialize(character);
                await managePlansWindow.ShowDialog(this);

                // If a plan was selected to open, open it in the editor
                if (managePlansWindow.SelectedPlan != null)
                {
                    var editorWindow = new PlanEditorWindow
                    {
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };
                    editorWindow.Initialize(managePlansWindow.SelectedPlan, character);
                    editorWindow.Show(this);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error managing plans: {ex}");
            }
        }

        private async void OnManageGroupsClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                var groupsWindow = new ManageGroupsWindow();
                await groupsWindow.ShowDialog(this);

                // Refresh overview to reflect group changes
                _overviewView?.RefreshView();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error managing groups: {ex}");
            }
        }

        private async void OnImportPlanClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                var character = GetSelectedCharacter();
                if (character == null) return;

                var fileTypes = new[]
                {
                    new FilePickerFileType("EveLens Plan") { Patterns = new[] { "*.emp", "*.xml" } },
                    new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
                };

                var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Import Plan",
                    AllowMultiple = false,
                    FileTypeFilter = fileTypes
                });

                if (files.Count == 0) return;

                string path = files[0].Path.LocalPath;
                string planName = System.IO.Path.GetFileNameWithoutExtension(path);
                planName = character.Plans.GetUniqueName(planName);
                var plan = new Plan(character) { Name = planName };
                character.Plans.Add(plan);

                var editorWindow = new PlanEditorWindow
                {
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                editorWindow.Initialize(plan, character);
                editorWindow.Show(this);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error importing plan: {ex}");
            }
        }

        private async void OnCreateFromQueueClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                var character = GetSelectedCharacter();
                if (character is not CCPCharacter ccp)
                {
                    var dialog = new Window
                    {
                        Title = "Create from Queue",
                        Width = 320, Height = 130,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Content = new TextBlock
                        {
                            Text = "This feature requires a CCP character with an active skill queue.",
                            VerticalAlignment = VerticalAlignment.Center,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            TextWrapping = TextWrapping.Wrap,
                            Margin = new Thickness(20)
                        }
                    };
                    await dialog.ShowDialog(this);
                    return;
                }

                string planName = character.Plans.GetUniqueName("From Skill Queue");
                var plan = new Plan(character) { Name = planName };
                foreach (var queueItem in ccp.SkillQueue)
                {
                    if (queueItem.Skill != null && queueItem.Skill.StaticData != null)
                    {
                        plan.PlanTo(queueItem.Skill.StaticData, queueItem.Level);
                    }
                }
                character.Plans.Add(plan);

                var editorWindow = new PlanEditorWindow
                {
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                editorWindow.Initialize(plan, character);
                editorWindow.Show(this);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating plan from queue: {ex}");
            }
        }

        private async void OnCharCompClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new Window
                {
                    Title = "Character Comparison",
                    Width = 300,
                    Height = 150,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Content = new TextBlock
                    {
                        Text = "Character Comparison — Coming soon",
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(20)
                    }
                };
                await dialog.ShowDialog(this);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex}");
            }
        }

        private async void OnSkillConstellationClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                var character = GetSelectedCharacter();
                if (character == null)
                {
                    // Use first character if on Overview
                    character = _viewModel.Characters.FirstOrDefault();
                }
                if (character == null)
                {
                    var dialog = new Window
                    {
                        Title = "No Character",
                        Width = 320, Height = 130,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Content = new TextBlock
                        {
                            Text = "Add a character to view the skill constellation.",
                            VerticalAlignment = VerticalAlignment.Center,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            TextWrapping = TextWrapping.Wrap,
                            Margin = new Thickness(20)
                        }
                    };
                    await dialog.ShowDialog(this);
                    return;
                }

                var window = new SkillConstellationWindow();
                window.Initialize(character);
                window.Show(this);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error opening skill constellation: {ex}");
                try
                {
                    var errDialog = new Window
                    {
                        Title = "Error",
                        Width = 500, Height = 200,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Content = new TextBlock
                        {
                            Text = $"Failed to open skill constellation:\n{ex.Message}",
                            VerticalAlignment = VerticalAlignment.Center,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            TextWrapping = TextWrapping.Wrap,
                            Margin = new Thickness(20),
                            FontSize = 11
                        }
                    };
                    await errDialog.ShowDialog(this);
                }
                catch { }
            }
        }

        private async void OnCheckUpdatesClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                string currentVersion = AppServices.VelopackUpdate?.CurrentVersion
                    ?? AppServices.FileVersionInfo.FileVersion ?? "Unknown";

                bool hasUpdate = await (AppServices.VelopackUpdate?.CheckNowAsync() ?? Task.FromResult(false));

                if (hasUpdate)
                {
                    string pendingVersion = AppServices.VelopackUpdate?.PendingVersion ?? "newer version";
                    var updateDialog = new Window
                    {
                        Title = "Update Available",
                        Width = 400, Height = 200,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    };

                    var downloadBtn = new Button
                    {
                        Content = "Download & Restart",
                        FontSize = 11,
                        Padding = new Thickness(12, 5),
                        CornerRadius = new CornerRadius(12),
                        Foreground = FindStripBrush("EveSuccessGreenBrush", Brushes.LimeGreen),
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    var laterBtn = new Button
                    {
                        Content = "Later",
                        FontSize = 11,
                        Padding = new Thickness(12, 5),
                        CornerRadius = new CornerRadius(12),
                        HorizontalAlignment = HorizontalAlignment.Center
                    };

                    downloadBtn.Click += async (_, _) =>
                    {
                        try
                        {
                            downloadBtn.IsEnabled = false;
                            downloadBtn.Content = "Downloading...";
                            bool downloaded = await (AppServices.VelopackUpdate?.DownloadUpdateAsync() ?? Task.FromResult(false));
                            if (downloaded)
                                AppServices.VelopackUpdate?.ApplyAndRestart();
                        }
                        catch (Exception ex) { Debug.WriteLine($"Download error: {ex}"); }
                    };
                    laterBtn.Click += (_, _) => updateDialog.Close();

                    updateDialog.Content = new StackPanel
                    {
                        Margin = new Thickness(20),
                        VerticalAlignment = VerticalAlignment.Center,
                        Spacing = 10,
                        Children =
                        {
                            new TextBlock
                            {
                                Text = $"EveLens {pendingVersion} is available",
                                FontSize = 14,
                                FontWeight = FontWeight.SemiBold,
                                Foreground = FindStripBrush("EveAccentPrimaryBrush", Brushes.Gold),
                                HorizontalAlignment = HorizontalAlignment.Center
                            },
                            new TextBlock
                            {
                                Text = $"You are running v{currentVersion}",
                                FontSize = 11,
                                Foreground = FindStripBrush("EveTextSecondaryBrush", Brushes.Gray),
                                HorizontalAlignment = HorizontalAlignment.Center
                            },
                            new StackPanel
                            {
                                Orientation = global::Avalonia.Layout.Orientation.Horizontal,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                Spacing = 8,
                                Children = { downloadBtn, laterBtn }
                            }
                        }
                    };
                    await updateDialog.ShowDialog(this);
                }
                else
                {
                    var upToDateDialog = new Window
                    {
                        Title = "Check for Updates",
                        Width = 380, Height = 160,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Content = new StackPanel
                        {
                            Margin = new Thickness(20),
                            VerticalAlignment = VerticalAlignment.Center,
                            Spacing = 8,
                            Children =
                            {
                                new TextBlock
                                {
                                    Text = $"Current version: v{currentVersion}",
                                    FontSize = 12,
                                    Foreground = FindStripBrush("EveTextPrimaryBrush", Brushes.White)
                                },
                                new TextBlock
                                {
                                    Text = "You are running the latest version.",
                                    FontSize = 11,
                                    Foreground = FindStripBrush("EveSuccessGreenBrush", Brushes.LimeGreen)
                                }
                            }
                        }
                    };
                    await upToDateDialog.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking for updates: {ex}");
            }
        }

        private void OnUserGuideClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                Util.OpenURL(new Uri("https://evelens.dev"));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex}");
            }
        }

        private void OnReportIssueClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                Util.OpenURL(new Uri("https://github.com/aliacollins/evelens/issues"));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex}");
            }
        }

        private void RefreshNotificationUI()
        {
            if (_notificationVm == null) return;
            var entries = _notificationVm.Entries;
            ActivityItems.ItemsSource = entries.Select(e => new ActivityDisplayEntry(e)).ToList();
            UnreadBadge.IsVisible = _notificationVm.HasUnread;
            UnreadCountText.Text = _notificationVm.UnreadCount > 99
                ? "99+"
                : _notificationVm.UnreadCount.ToString();
            EmptyActivityText.IsVisible = entries.Count == 0;
        }

        internal async void DeleteCharacterWithConfirmation(Character character)
        {
            try
            {
                bool confirmed = await ShowConfirmationDialog(
                    "Delete Character",
                    $"Delete {character.Name}? This will remove the character and all associated ESI keys.");

                if (!confirmed) return;

                try
                {
                    AppServices.Characters.Remove(character);
                }
                catch (Exception ex)
                {
                    // character.Dispose() inside Remove can throw —
                    // character is already removed from collections at this point
                    Debug.WriteLine($"Error during character removal: {ex}");
                }

                // Always rebuild regardless of whether Remove threw
                RebuildCharacterStrip();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting character: {ex}");
            }
        }

        private async Task<bool> ShowConfirmationDialog(string title, string message)
        {
            bool result = false;

            var okBtn = new Button
            {
                Content = "OK",
                FontSize = 11,
                Padding = new Thickness(12, 5),
                CornerRadius = new CornerRadius(12),
                Foreground = Brushes.Red
            };
            var cancelBtn = new Button
            {
                Content = "Cancel",
                FontSize = 11,
                Padding = new Thickness(12, 5),
                CornerRadius = new CornerRadius(12)
            };

            var dialog = new Window
            {
                Title = title,
                Width = 380, Height = 160,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new DockPanel
                {
                    Margin = new Thickness(16),
                    Children =
                    {
                        new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Spacing = 8,
                            HorizontalAlignment = HorizontalAlignment.Right,
                            Margin = new Thickness(0, 12, 0, 0),
                            [DockPanel.DockProperty] = Dock.Bottom,
                            Children = { okBtn, cancelBtn }
                        },
                        new TextBlock
                        {
                            Text = message,
                            TextWrapping = TextWrapping.Wrap,
                            FontSize = 12,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    }
                }
            };

            okBtn.Click += (_, _) => { result = true; dialog.Close(); };
            cancelBtn.Click += (_, _) => { dialog.Close(); };

            await dialog.ShowDialog(this);
            return result;
        }

        private async Task ShowMessageDialog(string title, string text)
        {
            var closeBtn = new Button
            {
                Content = "Close",
                FontSize = 11,
                Padding = new Thickness(12, 5),
                CornerRadius = new CornerRadius(12),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            DockPanel.SetDock(closeBtn, global::Avalonia.Controls.Dock.Bottom);

            var dialog = new Window
            {
                Title = title,
                Width = 480, Height = 340,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new DockPanel
                {
                    Margin = new Thickness(16),
                    Children =
                    {
                        closeBtn,
                        new ScrollViewer
                        {
                            Content = new TextBlock
                            {
                                Text = text,
                                FontSize = 11,
                                FontFamily = new global::Avalonia.Media.FontFamily("Consolas, Courier New, monospace"),
                                TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
                                Foreground = FindStripBrush("EveTextPrimaryBrush", Brushes.White)
                            }
                        }
                    }
                }
            };

            closeBtn.Click += (_, _) => dialog.Close();
            await dialog.ShowDialog(this);
        }

        private async void OnClearCacheClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                bool confirmed = await ShowConfirmationDialog(
                    "Clear Cache",
                    "Clear all cached images, portraits, and data files? This cannot be undone.");

                if (confirmed)
                {
                    AppServices.ClearCache();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error clearing cache: {ex}");
            }
        }

        private void OnNotificationSent(NotificationSentEvent e)
        {
            try
            {
                if (!Common.Settings.Notifications.ShowOSNotifications)
                    return;

                var args = e.Args;
                string title = args.SenderCharacter?.Name ?? "EveLens";
                string message = args.Description ?? args.Category.ToString();
                NativeNotificationService.Show(title, message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing native notification: {ex}");
            }
        }


        private void WirePrivacyCheckbox(CheckBox cb, PrivacyCategories category)
        {
            cb.IsCheckedChanged += (_, _) =>
            {
                bool wantChecked = cb.IsChecked == true;
                bool isSet = AppServices.IsPrivate(category);
                if (wantChecked != isSet)
                    AppServices.TogglePrivacyCategory(category);
                UpdatePrivacyIcon();
            };
        }

        private void SyncPrivacyCheckboxes()
        {
            PrivacyNameCb.IsChecked = AppServices.IsPrivate(PrivacyCategories.Name);
            PrivacyCorpCb.IsChecked = AppServices.IsPrivate(PrivacyCategories.CorpAlliance);
            PrivacyBalanceCb.IsChecked = AppServices.IsPrivate(PrivacyCategories.Balance);
            PrivacySkillPointsCb.IsChecked = AppServices.IsPrivate(PrivacyCategories.SkillPoints);
            PrivacyTrainingCb.IsChecked = AppServices.IsPrivate(PrivacyCategories.Training);
            PrivacyRemapsCb.IsChecked = AppServices.IsPrivate(PrivacyCategories.Remaps);
        }

        private void UpdatePrivacyIcon()
        {
            EyeOpenIcon.IsVisible = !AppServices.PrivacyModeEnabled;
            EyeSlashIcon.IsVisible = AppServices.PrivacyModeEnabled;
        }

        private void RestoreWindowLocation()
        {
            try
            {
                if (!Settings.UI.WindowLocations.TryGetValue(WindowLocationKey, out var loc)
                    || loc.Width < 100 || loc.Height < 100)
                    return;

                WindowStartupLocation = WindowStartupLocation.Manual;
                Position = new PixelPoint(loc.Left, loc.Top);
                Width = loc.Width;
                Height = loc.Height;
            }
            catch
            {
                // If settings are corrupt, use defaults
            }
        }

        internal void SaveWindowLocationNow()
        {
            try
            {
                // Use last tracked values — Avalonia may zero Position during close
                if (_lastWidth < 100 || _lastHeight < 100)
                    return;

                Settings.UI.WindowLocations[WindowLocationKey] = new WindowLocationSettings
                {
                    Left = _lastPosition.X,
                    Top = _lastPosition.Y,
                    Width = (int)_lastWidth,
                    Height = (int)_lastHeight
                };
            }
            catch
            {
                // Don't let window save failures prevent shutdown
            }
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);

            // Seed with current values — PositionChanged may not fire for the initial position
            if (WindowState == WindowState.Normal)
            {
                _lastPosition = Position;
                _lastWidth = Width;
                _lastHeight = Height;
            }

            // Track position/size in real-time — Avalonia may zero Position during close
            PositionChanged += (_, args) =>
            {
                if (WindowState == WindowState.Normal)
                    _lastPosition = args.Point;
            };
            SizeChanged += (_, args) =>
            {
                if (WindowState == WindowState.Normal)
                {
                    if (args.NewSize.Width > 0)
                        _lastWidth = args.NewSize.Width;
                    if (args.NewSize.Height > 0)
                        _lastHeight = args.NewSize.Height;
                }
            };
        }

        protected override void OnClosed(EventArgs e)
        {
            SaveWindowLocationNow();
            _notificationVm?.Save();
            _notificationVm?.Dispose();
            _tickSubscription?.Dispose();
            _collectionChangedSub?.Dispose();
            _monitoredChangedSub?.Dispose();
            _notificationSentSub?.Dispose();
            _privacyModeSub?.Dispose();
            foreach (var oc in _observableCharacters) oc.Dispose();
            _observableCharacters.Clear();
            _cachedViews.Clear();
            _viewModel.Dispose();
            base.OnClosed(e);
        }
    }

}
