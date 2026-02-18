using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using EVEMon.Avalonia.Views.CharacterMonitor;
using EVEMon.Avalonia.Views.Dialogs;
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

        public MainWindow()
        {
            InitializeComponent();

            _viewModel = new MainWindowViewModel();
            DataContext = _viewModel;

            _viewModel.RefreshCharacters();
            BuildTabs();

            WireMenuItems();

            _tickSubscription = AppServices.EventAggregator?.Subscribe<SecondTickEvent>(
                e => Dispatcher.UIThread.Post(() => OnSecondTick(e)));
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
            ExitMenuItem.Click += OnExitClick;

            // Plans menu
            NewPlanMenuItem.Click += OnPlansClick;
            ManagePlansMenuItem.Click += OnPlansClick;

            // Tools menu
            CharCompMenuItem.Click += OnCharCompClick;
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
            try
            {
                var settingsWindow = new SettingsWindow();
                await settingsWindow.ShowDialog(this);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error opening settings: {ex}");
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
                var dialog = new Window
                {
                    Title = "Manage Characters",
                    Width = 300,
                    Height = 150,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Content = new TextBlock
                    {
                        Text = "Manage Characters — Coming soon",
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

        private async void OnPlansClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new Window
                {
                    Title = "Plans",
                    Width = 300,
                    Height = 150,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Content = new TextBlock
                    {
                        Text = "Plans — Coming soon",
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

        protected override void OnClosed(EventArgs e)
        {
            _tickSubscription?.Dispose();
            foreach (var oc in _observableCharacters) oc.Dispose();
            _observableCharacters.Clear();
            _viewModel.Dispose();
            base.OnClosed(e);
        }
    }

}
