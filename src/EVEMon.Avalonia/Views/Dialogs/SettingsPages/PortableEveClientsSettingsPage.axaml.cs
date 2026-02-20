using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using EVEMon.Common.Serialization.Settings;
using EVEMon.Common.Services;

namespace EVEMon.Avalonia.Views.Dialogs.SettingsPages
{
    public partial class PortableEveClientsSettingsPage : UserControl
    {
        private readonly SerializableSettings _settings = null!;
        private readonly List<TextBox> _pathTextBoxes = new();

        public PortableEveClientsSettingsPage()
        {
            InitializeComponent();
        }

        public PortableEveClientsSettingsPage(SerializableSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            LoadFromSettings();

            AddButton.Click += OnAddClick;
        }

        private void LoadFromSettings()
        {
            ClientsPanel.Children.Clear();
            _pathTextBoxes.Clear();

            foreach (var client in _settings.PortableEveInstallations.EVEClients)
            {
                AddClientRow(client.Path ?? string.Empty);
            }

            // Disable if EVE app data exists in default location
            bool disabled = AppServices.EveAppDataFoldersExistInDefaultLocation;
            ClientsPanel.IsEnabled = !disabled;
            AddButton.IsEnabled = !disabled;
            DisabledNote.IsVisible = disabled;
        }

        private void AddClientRow(string path)
        {
            var textBox = new TextBox
            {
                Text = path,
                IsReadOnly = true,
                Width = 300,
                FontSize = 11
            };
            var browseBtn = new Button
            {
                Content = "Browse...",
                Padding = new Thickness(8, 4),
                CornerRadius = new CornerRadius(12),
                FontSize = 11
            };
            var deleteBtn = new Button
            {
                Content = "Delete",
                Padding = new Thickness(8, 4),
                CornerRadius = new CornerRadius(12),
                FontSize = 11,
                Foreground = global::Avalonia.Media.Brushes.Red
            };

            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6
            };
            row.Children.Add(textBox);
            row.Children.Add(browseBtn);
            row.Children.Add(deleteBtn);

            _pathTextBoxes.Add(textBox);
            ClientsPanel.Children.Add(row);

            browseBtn.Click += async (_, _) =>
            {
                try
                {
                    var topLevel = TopLevel.GetTopLevel(this);
                    if (topLevel == null) return;

                    var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
                        new FolderPickerOpenOptions { Title = "Select EVE Client Folder" });

                    if (folders.Count > 0)
                    {
                        string newPath = folders[0].Path.LocalPath;
                        // Prevent duplicates
                        if (!_pathTextBoxes.Any(tb => tb.Text == newPath))
                            textBox.Text = newPath;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error browsing folder: {ex}");
                }
            };

            deleteBtn.Click += (_, _) =>
            {
                _pathTextBoxes.Remove(textBox);
                ClientsPanel.Children.Remove(row);
            };
        }

        private void OnAddClick(object? sender, RoutedEventArgs e)
        {
            AddClientRow(string.Empty);
        }

        public void ApplyToSettings()
        {
            _settings.PortableEveInstallations.EVEClients.Clear();
            foreach (var textBox in _pathTextBoxes)
            {
                if (!string.IsNullOrWhiteSpace(textBox.Text))
                {
                    _settings.PortableEveInstallations.EVEClients.Add(
                        new SerializablePortableEveInstallation { Path = textBox.Text });
                }
            }
        }
    }
}
