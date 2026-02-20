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

        // Strongly-typed references to all settings pages for apply
        private GeneralSettingsPage? _generalPage;
        private UpdatesSettingsPage? _updatesPage;
        private NetworkSettingsPage? _networkPage;
        private MainWindowSettingsPage? _mainWindowPage;
        private SkillPlannerSettingsPage? _skillPlannerPage;
        private NotificationsSettingsPage? _notificationsPage;
        private TrayIconSettingsPage? _trayIconPage;
        private MarketPriceProvidersSettingsPage? _marketPricePage;
        private G15SettingsPage? _g15Page;
        private PortableEveClientsSettingsPage? _portableEvePage;
        private IconsSettingsPage? _iconsPage;
        private MessagesSettingsPage? _messagesPage;
        private SchedulerSettingsPage? _schedulerPage;
        private ExternalCalendarSettingsPage? _externalCalendarPage;
        private EmailNotificationsSettingsPage? _emailNotificationsPage;
        private EsiScopeSettingsPage? _esiScopePage;

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
            general.Items.Add(CreateItem("ESI Scopes", "esiScopePage"));

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

            CategoryTree.Items.Add(general);
            CategoryTree.Items.Add(mainWindow);
            CategoryTree.Items.Add(skillPlanner);
            CategoryTree.Items.Add(trayIcon);
            CategoryTree.Items.Add(scheduler);
            CategoryTree.Items.Add(notifications);
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
            // All settings pages
            _generalPage = new GeneralSettingsPage(_settings);
            _updatesPage = new UpdatesSettingsPage(_settings);
            _networkPage = new NetworkSettingsPage(_settings);
            _mainWindowPage = new MainWindowSettingsPage(_settings);
            _skillPlannerPage = new SkillPlannerSettingsPage(_settings);
            _notificationsPage = new NotificationsSettingsPage(_settings);
            _trayIconPage = new TrayIconSettingsPage(_settings);
            _marketPricePage = new MarketPriceProvidersSettingsPage(_settings);
            _g15Page = new G15SettingsPage(_settings);
            _portableEvePage = new PortableEveClientsSettingsPage(_settings);
            _iconsPage = new IconsSettingsPage(_settings);
            _messagesPage = new MessagesSettingsPage(_settings);
            _schedulerPage = new SchedulerSettingsPage(_settings);
            _externalCalendarPage = new ExternalCalendarSettingsPage(_settings);
            _emailNotificationsPage = new EmailNotificationsSettingsPage(_settings);
            _esiScopePage = new EsiScopeSettingsPage(_settings);

            _pages["generalPage"] = _generalPage;
            _pages["updatesPage"] = _updatesPage;
            _pages["networkPage"] = _networkPage;
            _pages["mainWindowPage"] = _mainWindowPage;
            _pages["skillPlannerPage"] = _skillPlannerPage;
            _pages["notificationsPage"] = _notificationsPage;
            _pages["trayIconPage"] = _trayIconPage;
            _pages["marketPriceProvidersPage"] = _marketPricePage;
            _pages["g15Page"] = _g15Page;
            _pages["portableEveClientsPage"] = _portableEvePage;
            _pages["iconsPage"] = _iconsPage;
            _pages["messagesPage"] = _messagesPage;
            _pages["schedulerUIPage"] = _schedulerPage;
            _pages["externalCalendarPage"] = _externalCalendarPage;
            _pages["emailNotificationsPage"] = _emailNotificationsPage;
            _pages["esiScopePage"] = _esiScopePage;
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
            _trayIconPage?.ApplyToSettings();
            _marketPricePage?.ApplyToSettings();
            _g15Page?.ApplyToSettings();
            _portableEvePage?.ApplyToSettings();
            _iconsPage?.ApplyToSettings();
            _messagesPage?.ApplyToSettings();
            _schedulerPage?.ApplyToSettings();
            _externalCalendarPage?.ApplyToSettings();
            _emailNotificationsPage?.ApplyToSettings();
            _esiScopePage?.ApplyToSettings();
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
