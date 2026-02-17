using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using EVEMon.Avalonia.Views.Dialogs.SettingsPages;
using EVEMon.Common;
using EVEMon.Common.Serialization.Settings;

namespace EVEMon.Avalonia.Views.Dialogs
{
    public partial class SettingsWindow : Window
    {
        private readonly SerializableSettings _settings;
        private readonly Dictionary<string, UserControl> _pages = new();
        private string _selectedPageKey = string.Empty;

        // Strongly-typed references to the 6 MUST HAVE pages for apply
        private GeneralSettingsPage? _generalPage;
        private UpdatesSettingsPage? _updatesPage;
        private NetworkSettingsPage? _networkPage;
        private MainWindowSettingsPage? _mainWindowPage;
        private SkillPlannerSettingsPage? _skillPlannerPage;
        private NotificationsSettingsPage? _notificationsPage;

        public SettingsWindow()
        {
            InitializeComponent();

            // Work on a copy of settings (same pattern as WinForms SettingsForm)
            _settings = Settings.Export();

            BuildCategoryTree();
            BuildPages();
            WireEvents();

            // Select first category
            if (CategoryTree.Items.Count > 0)
            {
                var first = (TreeViewItem)CategoryTree.Items[0]!;
                CategoryTree.SelectedItem = first;
                SelectPage((string)first.Tag!);
            }
        }

        private void BuildCategoryTree()
        {
            // General (with children)
            var general = CreateItem("General", "generalPage");
            general.Items.Add(CreateItem("Updates", "updatesPage"));
            general.Items.Add(CreateItem("Network", "networkPage"));
            general.Items.Add(CreateItem("Logitech Keyboards", "g15Page"));
            general.Items.Add(CreateItem("Portable EVE Clients", "portableEveClientsPage"));
            general.Items.Add(CreateItem("Market Price Providers", "marketPriceProvidersPage"));

            // Main Window
            var mainWindow = CreateItem("Main Window", "mainWindowPage");

            // Skill Planner (with children)
            var skillPlanner = CreateItem("Skill Planner", "skillPlannerPage");
            skillPlanner.Items.Add(CreateItem("Icons", "iconsPage"));
            skillPlanner.Items.Add(CreateItem("Messages", "messagesPage"));

            // System Tray Icon
            var trayIcon = CreateItem("System Tray Icon", "trayIconPage");

            // Scheduler (with children)
            var scheduler = CreateItem("Scheduler", "schedulerUIPage");
            scheduler.Items.Add(CreateItem("External Calendar", "externalCalendarPage"));

            // Notifications (with children)
            var notifications = CreateItem("Notifications", "notificationsPage");
            notifications.Items.Add(CreateItem("Skill Completion Mails", "emailNotificationsPage"));

            // Cloud Storage Service (disabled)
            var cloudStorage = CreateItem("Cloud Storage Service", "cloudStoragePage");

            CategoryTree.Items.Add(general);
            CategoryTree.Items.Add(mainWindow);
            CategoryTree.Items.Add(skillPlanner);
            CategoryTree.Items.Add(trayIcon);
            CategoryTree.Items.Add(scheduler);
            CategoryTree.Items.Add(notifications);
            CategoryTree.Items.Add(cloudStorage);
        }

        private static TreeViewItem CreateItem(string header, string tag)
        {
            return new TreeViewItem
            {
                Header = header,
                Tag = tag,
                IsExpanded = true
            };
        }

        private void BuildPages()
        {
            // 6 MUST HAVE pages with real settings controls
            _generalPage = new GeneralSettingsPage(_settings);
            _updatesPage = new UpdatesSettingsPage(_settings);
            _networkPage = new NetworkSettingsPage(_settings);
            _mainWindowPage = new MainWindowSettingsPage(_settings);
            _skillPlannerPage = new SkillPlannerSettingsPage(_settings);
            _notificationsPage = new NotificationsSettingsPage(_settings);

            _pages["generalPage"] = _generalPage;
            _pages["updatesPage"] = _updatesPage;
            _pages["networkPage"] = _networkPage;
            _pages["mainWindowPage"] = _mainWindowPage;
            _pages["skillPlannerPage"] = _skillPlannerPage;
            _pages["notificationsPage"] = _notificationsPage;

            // Deferred pages with placeholder content
            string[] deferredPages = new[]
            {
                "g15Page", "portableEveClientsPage", "marketPriceProvidersPage",
                "iconsPage", "messagesPage", "trayIconPage",
                "schedulerUIPage", "externalCalendarPage",
                "emailNotificationsPage", "cloudStoragePage"
            };

            foreach (string key in deferredPages)
            {
                _pages[key] = CreatePlaceholderPage();
            }
        }

        private static UserControl CreatePlaceholderPage()
        {
            var page = new UserControl();
            var text = new TextBlock
            {
                Text = "This settings page will be available in a future update.",
                FontSize = 13,
                Foreground = global::Avalonia.Media.Brushes.Gray,
                Margin = new global::Avalonia.Thickness(0, 8, 0, 0)
            };
            page.Content = text;
            return page;
        }

        private void WireEvents()
        {
            CategoryTree.SelectionChanged += OnCategorySelectionChanged;
            OkButton.Click += OnOkClick;
            CancelButton.Click += OnCancelClick;
            ApplyButton.Click += OnApplyClick;
        }

        private void OnCategorySelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (CategoryTree.SelectedItem is TreeViewItem item && item.Tag is string pageKey)
            {
                SelectPage(pageKey);
            }
        }

        private void SelectPage(string pageKey)
        {
            if (_selectedPageKey == pageKey)
                return;

            _selectedPageKey = pageKey;

            if (_pages.TryGetValue(pageKey, out var page))
            {
                PageContent.Content = page;
            }
        }

        private void CollectSettingsFromPages()
        {
            _generalPage?.ApplyToSettings();
            _updatesPage?.ApplyToSettings();
            _networkPage?.ApplyToSettings();
            _mainWindowPage?.ApplyToSettings();
            _skillPlannerPage?.ApplyToSettings();
            _notificationsPage?.ApplyToSettings();
        }

        private async void OnOkClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                CollectSettingsFromPages();
                await Settings.ImportAsync(_settings, true);
                Close(true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex}");
                Close(true);
            }
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            Close(false);
        }

        private async void OnApplyClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                CollectSettingsFromPages();
                await Settings.ImportAsync(_settings, true);
                ApplyButton.IsEnabled = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying settings: {ex}");
            }
        }
    }
}
