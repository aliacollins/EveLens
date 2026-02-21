using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using EVEMon.Avalonia.ViewModels;
using EVEMon.Avalonia.Views.CharacterMonitor;
using EVEMon.Avalonia.Views.Dialogs;
using EVEMon.Avalonia.Views.PlanEditor;
using EVEMon.Common.Models;
using EVEMon.Common.Services;
using EVEMon.Common.ViewModels;
using EVEMon.Core.Events;

namespace EVEMon.Avalonia.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _viewModel;
        private readonly List<ObservableCharacter> _observableCharacters = new();
        private IDisposable? _tickSubscription;
        private NotificationCenterViewModel? _notificationVm;

        public MainWindow()
        {
            InitializeComponent();

            _viewModel = new MainWindowViewModel();
            DataContext = _viewModel;

            _viewModel.RefreshCharacters();
            BuildTabs();

            WireMenuItems();

            // Disable plan menus initially (Overview tab active)
            NewPlanMenuItem.IsEnabled = false;
            ManagePlansMenuItem.IsEnabled = false;
            ImportPlanMenuItem.IsEnabled = false;
            CreateFromQueueMenuItem.IsEnabled = false;
            MainTabControl.SelectionChanged += OnTabSelectionChanged;

            _tickSubscription = AppServices.EventAggregator?.Subscribe<SecondTickEvent>(
                e => Dispatcher.UIThread.Post(() => OnSecondTick(e)));

            // Wire notification center
            _notificationVm = new NotificationCenterViewModel();
            MarkReadBtn.Click += (_, _) => { _notificationVm.MarkAllRead(); RefreshNotificationUI(); };
            ClearAllBtn.Click += (_, _) => { _notificationVm.ClearAll(); RefreshNotificationUI(); };
            _notificationVm.PropertyChanged += (_, _) =>
                Dispatcher.UIThread.Post(RefreshNotificationUI);
            RefreshNotificationUI();
        }

        private void BuildTabs()
        {
            try
            {
                // Overview tab
                MainTabControl.Items.Add(new TabItem
                {
                    Header = "Overview",
                    Content = new CharacterOverviewView(),
                    FontSize = 12
                });

                // Character tabs — each gets an ObservableCharacter for INPC binding
                foreach (Character character in _viewModel.Characters)
                {
                    var observable = new ObservableCharacter(character);
                    _observableCharacters.Add(observable);

                    MainTabControl.Items.Add(new TabItem
                    {
                        Header = character.Name,
                        Content = new CharacterMonitorView { DataContext = observable },
                        FontSize = 12
                    });
                }

                MainTabControl.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error building tabs: {ex}");
            }
        }

        private void WireMenuItems()
        {
            // File menu
            AddCharMenuItem.Click += OnAddCharacterClick;
            ManageCharsMenuItem.Click += OnManageCharactersClick;
            ManageGroupsMenuItem.Click += OnManageGroupsClick;
            RestoreSettingsMenuItem.Click += OnRestoreSettingsClick;
            SaveSettingsMenuItem.Click += OnSaveSettingsClick;
            ResetSettingsMenuItem.Click += OnResetSettingsClick;
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
            SettingsMenuItem.Click += OnSettingsClick;

            // Help menu
            UserGuideMenuItem.Click += OnUserGuideClick;
            ReportIssueMenuItem.Click += OnReportIssueClick;
            AboutMenuItem.Click += OnAboutClick;

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

        private void UpdateEsiCountdown()
        {
            try
            {
                var now = DateTime.UtcNow;
                DateTime? soonestUpdate = null;
                string? soonestMethod = null;
                int fetchingCount = 0;
                DateTime lastCompleted = DateTime.MinValue;

                foreach (var character in _viewModel.Characters)
                {
                    if (character is not CCPCharacter ccp) continue;

                    foreach (var monitor in ccp.QueryMonitors)
                    {
                        // Count actively fetching
                        if (monitor.Status == Common.Enumerations.QueryStatus.Updating)
                            fetchingCount++;

                        // Track most recent completion
                        if (monitor.LastUpdate > lastCompleted && monitor.LastUpdate.Year > 2000)
                            lastCompleted = monitor.LastUpdate;

                        // Find soonest future update
                        var next = monitor.NextUpdate;
                        if (next > now && (soonestUpdate == null || next < soonestUpdate))
                        {
                            soonestUpdate = next;
                            soonestMethod = monitor.Method.ToString();
                        }
                    }
                }

                // Left indicator: what's happening now
                if (fetchingCount > 0)
                {
                    NextUpdateText.Text = $"ESI: {fetchingCount} fetching...";
                    NextUpdateText.Foreground = global::Avalonia.Media.Brushes.LimeGreen;
                }
                else if (soonestUpdate.HasValue)
                {
                    var remaining = soonestUpdate.Value - now;
                    string timeStr = remaining.TotalMinutes >= 1
                        ? $"{(int)remaining.TotalMinutes}m {remaining.Seconds}s"
                        : $"{remaining.Seconds}s";
                    NextUpdateText.Text = $"Next: {soonestMethod} in {timeStr}";
                    NextUpdateText.Foreground = global::Avalonia.Media.Brushes.Gold;
                }
                else
                {
                    NextUpdateText.Text = "ESI: idle";
                    NextUpdateText.Foreground = global::Avalonia.Media.Brushes.Gray;
                }

                // Right indicator: last refresh time
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
                RebuildCharacterTabs();
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
                    SuggestedFileName = "EVEMon-settings-backup",
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
                    RebuildCharacterTabs();
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
                                TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
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
                                TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
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
                var dialog = new Window
                {
                    Title = "Add Character",
                    Width = 300,
                    Height = 150,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Content = new TextBlock
                    {
                        Text = "Add Character — Coming soon",
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

        private async void OnManageCharactersClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                var characters = _viewModel.Characters.ToList();
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
                            TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
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
                    Foreground = global::Avalonia.Media.Brushes.Red,
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
                            AppServices.Characters.Remove(character);
                            dialog.Close();
                            RebuildCharacterTabs();
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
        /// Returns the character whose tab is currently selected, or null if Overview is active.
        /// </summary>
        private Character? GetSelectedCharacter()
        {
            int index = MainTabControl.SelectedIndex;
            // Index 0 is Overview; character tabs start at 1
            if (index <= 0 || index - 1 >= _observableCharacters.Count)
                return null;

            return _observableCharacters[index - 1].Character;
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
                var nameBox = new TextBox
                {
                    Text = $"Plan {planCount + 1}",
                    FontSize = 12,
                    Margin = new Thickness(0, 8, 0, 0),
                    Watermark = "Enter plan name..."
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

                string? planName = null;
                var nameDialog = new Window
                {
                    Title = "New Plan",
                    Width = 340, Height = 170,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Content = new StackPanel
                    {
                        Margin = new Thickness(16),
                        Children =
                        {
                            new TextBlock { Text = "Plan name:", FontSize = 12 },
                            nameBox,
                            createBtn
                        }
                    }
                };

                nameBox.AttachedToVisualTree += (_, _) => nameBox.SelectAll();
                createBtn.Click += (_, _) =>
                {
                    planName = nameBox.Text?.Trim();
                    if (!string.IsNullOrEmpty(planName))
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
                            TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
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
                foreach (var item in MainTabControl.Items)
                {
                    if (item is TabItem tabItem && tabItem.Content is CharacterOverviewView overview)
                    {
                        // Force re-render by detaching/attaching
                        overview.RefreshView();
                        break;
                    }
                }
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
                    new FilePickerFileType("EVEMon Plan") { Patterns = new[] { "*.emp", "*.xml" } },
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
                            TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
                            Margin = new Thickness(20)
                        }
                    };
                    await dialog.ShowDialog(this);
                    return;
                }

                var plan = new Plan(character) { Name = "From Skill Queue" };
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
                    // Use first character if on Overview tab
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
                            TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
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
                            TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
                            Margin = new Thickness(20),
                            FontSize = 11
                        }
                    };
                    await errDialog.ShowDialog(this);
                }
                catch { }
            }
        }

        private void OnUserGuideClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://evemon.dev") { UseShellExecute = true });
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
                Process.Start(new ProcessStartInfo("https://github.com/aliacollins/evemon/issues") { UseShellExecute = true });
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

        private void OnTabSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            bool hasCharacter = GetSelectedCharacter() != null;
            NewPlanMenuItem.IsEnabled = hasCharacter;
            ManagePlansMenuItem.IsEnabled = hasCharacter;
            ImportPlanMenuItem.IsEnabled = hasCharacter;
            CreateFromQueueMenuItem.IsEnabled = hasCharacter;
        }

        internal async void DeleteCharacterWithConfirmation(Character character)
        {
            try
            {
                bool confirmed = await ShowConfirmationDialog(
                    "Delete Character",
                    $"Delete {character.Name}? This will remove the character and all associated ESI keys.");

                if (confirmed)
                {
                    AppServices.Characters.Remove(character);
                    RebuildCharacterTabs();
                }
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
                Foreground = global::Avalonia.Media.Brushes.Red
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
                            TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
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

        private void RebuildCharacterTabs()
        {
            foreach (var oc in _observableCharacters) oc.Dispose();
            _observableCharacters.Clear();
            MainTabControl.Items.Clear();
            _viewModel.RefreshCharacters();
            BuildTabs();
        }

        protected override void OnClosed(EventArgs e)
        {
            _notificationVm?.Save();
            _notificationVm?.Dispose();
            _tickSubscription?.Dispose();
            foreach (var oc in _observableCharacters) oc.Dispose();
            _observableCharacters.Clear();
            _viewModel.Dispose();
            base.OnClosed(e);
        }
    }

}
