using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using EVEMon.Common.Attributes;
using EVEMon.Common.Notifications;
using EVEMon.Common.Serialization.Settings;
using EVEMon.Common.SettingsObjects;

namespace EVEMon.Avalonia.Views.Dialogs.SettingsPages
{
    public partial class NotificationsSettingsPage : UserControl
    {
        private readonly SerializableSettings _settings = null!;
        private readonly List<CategoryRow> _categoryRows = new();

        public NotificationsSettingsPage()
        {
            InitializeComponent();
        }

        public NotificationsSettingsPage(SerializableSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            LoadFromSettings();
        }

        private void LoadFromSettings()
        {
            PlaySoundOnSkillCompleteCheckBox.IsChecked = _settings.Notifications.PlaySoundOnSkillCompletion;
            BuildCategoryRows();
        }

        private void BuildCategoryRows()
        {
            CategoriesPanel.Children.Clear();
            _categoryRows.Clear();

            foreach (NotificationCategory category in Enum.GetValues(typeof(NotificationCategory)))
            {
                // Skip obsolete categories
                var field = typeof(NotificationCategory).GetField(category.ToString());
                if (field == null || field.GetCustomAttribute<ObsoleteAttribute>() != null)
                    continue;

                // Get display name from Header attribute
                string displayName = field.GetCustomAttribute<HeaderAttribute>()?.Header
                    ?? category.ToString();

                // Get current settings for this category
                _settings.Notifications.Categories.TryGetValue(category, out var catSettings);
                catSettings ??= new NotificationCategorySettings();

                // Build row
                var behaviourCombo = new ComboBox
                {
                    Width = 130,
                    FontSize = 11,
                    Items = { "Never", "Once", "Repeat Until Clicked" }
                };
                behaviourCombo.SelectedIndex = (int)catSettings.ToolTipBehaviour;

                var showOnMainCheckBox = new CheckBox
                {
                    IsChecked = catSettings.ShowOnMainWindow,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                var row = new Grid
                {
                    ColumnDefinitions = ColumnDefinitions.Parse("*,140,100")
                };
                row.Children.Add(new TextBlock
                {
                    Text = displayName,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 11
                });
                Grid.SetColumn(behaviourCombo, 1);
                row.Children.Add(behaviourCombo);
                Grid.SetColumn(showOnMainCheckBox, 2);
                row.Children.Add(showOnMainCheckBox);

                CategoriesPanel.Children.Add(row);
                _categoryRows.Add(new CategoryRow(category, behaviourCombo, showOnMainCheckBox));
            }
        }

        public void ApplyToSettings()
        {
            _settings.Notifications.PlaySoundOnSkillCompletion = PlaySoundOnSkillCompleteCheckBox.IsChecked == true;

            foreach (var row in _categoryRows)
            {
                var catSettings = new NotificationCategorySettings
                {
                    ToolTipBehaviour = (ToolTipNotificationBehaviour)Math.Max(0, row.BehaviourCombo.SelectedIndex),
                    ShowOnMainWindow = row.ShowOnMainCheckBox.IsChecked == true
                };
                _settings.Notifications.Categories[row.Category] = catSettings;
            }
        }

        private sealed class CategoryRow
        {
            public NotificationCategory Category { get; }
            public ComboBox BehaviourCombo { get; }
            public CheckBox ShowOnMainCheckBox { get; }

            public CategoryRow(NotificationCategory category, ComboBox behaviourCombo, CheckBox showOnMainCheckBox)
            {
                Category = category;
                BehaviourCombo = behaviourCombo;
                ShowOnMainCheckBox = showOnMainCheckBox;
            }
        }
    }
}
