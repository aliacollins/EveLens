// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using EveLens.Common.Data;
using EveLens.Common.Enumerations;
using EveLens.Common.Models;

using EveLens.Common.Models;
using EveLens.Common.Services;
using EveLens.Avalonia.Services;
namespace EveLens.Avalonia.Views.Dialogs
{
    public partial class ImplantSetEditorWindow : Window
    {
        private Character? _character;
        private ImplantSet? _selectedSet;
        private bool _isCustomSet;
        private readonly ComboBox[] _slotCombos = new ComboBox[10];
        private bool _suppressSlotChange;
        private IDisposable? _fontScaleSub;

        private static readonly ImplantSlots[] AllSlots = new[]
        {
            ImplantSlots.Perception, ImplantSlots.Memory, ImplantSlots.Willpower,
            ImplantSlots.Intelligence, ImplantSlots.Charisma,
            ImplantSlots.Slot6, ImplantSlots.Slot7, ImplantSlots.Slot8,
            ImplantSlots.Slot9, ImplantSlots.Slot10
        };

        private static readonly string[] SlotLabels = new[]
        {
            "Slot 1 (Perception)", "Slot 2 (Memory)", "Slot 3 (Willpower)",
            "Slot 4 (Intelligence)", "Slot 5 (Charisma)",
            "Slot 6", "Slot 7", "Slot 8", "Slot 9", "Slot 10"
        };

        public ImplantSetEditorWindow()
        {
            InitializeComponent();
        }

        public void Initialize(Character character)
        {
            _character = character;
            BuildSlotRows();
            RefreshSetList();

            _fontScaleSub = AppServices.EventAggregator?.Subscribe<EveLens.Common.Events.FontScaleChangedEvent>(
                _ => global::Avalonia.Threading.Dispatcher.UIThread.Post(RebuildUI));
        }

        protected override void OnClosed(EventArgs e)
        {
            _fontScaleSub?.Dispose();
            base.OnClosed(e);
        }

        private void RebuildUI()
        {
            BuildSlotRows();
            PopulateSlotCombos();
            UpdateBonusSummary();
        }

        private void BuildSlotRows()
        {
            SlotPanel.Children.Clear();

            for (int i = 0; i < 10; i++)
            {
                var row = new DockPanel { Margin = new Thickness(0, 0, 0, 0) };

                var label = new TextBlock
                {
                    Text = SlotLabels[i],
                    FontSize = FontScaleService.Body,
                    Foreground = (global::Avalonia.Media.IBrush?)Application.Current?.FindResource("EveTextSecondaryBrush")
                        ?? global::Avalonia.Media.Brushes.Gray,
                    VerticalAlignment = VerticalAlignment.Center,
                    Width = 130,
                    [DockPanel.DockProperty] = Dock.Left
                };

                var combo = new ComboBox
                {
                    FontSize = FontScaleService.Small,
                    MinWidth = 250,
                    VerticalAlignment = VerticalAlignment.Center,
                    IsEnabled = false,
                    Tag = i
                };
                combo.SelectionChanged += OnSlotChanged;

                row.Children.Add(label);
                row.Children.Add(combo);
                _slotCombos[i] = combo;
                SlotPanel.Children.Add(row);
            }
        }

        private void RefreshSetList()
        {
            if (_character == null) return;

            var items = _character.ImplantSets.Select(s => s.Name).ToList();
            SetListBox.ItemsSource = items;

            if (SetListBox.SelectedIndex < 0 && items.Count > 0)
                SetListBox.SelectedIndex = 0;
        }

        private void OnSetSelected(object? sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (_character == null) return;
                int index = SetListBox.SelectedIndex;
                if (index < 0) return;

                var sets = _character.ImplantSets.ToList();
                if (index >= sets.Count) return;

                _selectedSet = sets[index];
                SlotHeader.Text = _selectedSet.Name;

                // Determine if this is a custom set (editable)
                _isCustomSet = _selectedSet != _character.ImplantSets.ActiveClone
                    && _selectedSet != _character.ImplantSets.None;
                // Jump clones are read-only, custom sets are editable
                // We check if it's in the custom sets by checking if it's not Active and not None
                // and seeing if it's after the jump clones in the enumeration
                int activeIndex = 0; // ActiveClone is always first
                var allSets = _character.ImplantSets.ToList();
                bool isJumpClone = false;
                for (int i = 0; i < allSets.Count; i++)
                {
                    if (allSets[i] == _selectedSet && i == 0)
                    {
                        _isCustomSet = false; // Active clone
                        break;
                    }
                }

                PopulateSlotCombos();
                UpdateBonusSummary();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error selecting implant set: {ex}");
            }
        }

        private void PopulateSlotCombos()
        {
            if (_selectedSet == null) return;
            _suppressSlotChange = true;

            for (int i = 0; i < 10; i++)
            {
                var slot = AllSlots[i];
                var combo = _slotCombos[i];

                try
                {
                    var implants = StaticItems.GetImplants(slot);
                    var items = new List<string> { "(None)" };
                    items.AddRange(implants.Select(imp => $"{imp.Name} (+{imp.Bonus})"));

                    combo.ItemsSource = items;
                    combo.IsEnabled = _isCustomSet;

                    // Find current implant
                    var currentImplant = _selectedSet.ElementAtOrDefault(i);
                    if (currentImplant != null && currentImplant.ID > 0)
                    {
                        int idx = 1; // Start after "(None)"
                        foreach (var imp in implants)
                        {
                            if (imp.ID == currentImplant.ID)
                            {
                                combo.SelectedIndex = idx;
                                goto next;
                            }
                            idx++;
                        }
                    }
                    combo.SelectedIndex = 0; // (None)
                }
                catch
                {
                    combo.ItemsSource = new[] { "(None)" };
                    combo.SelectedIndex = 0;
                    combo.IsEnabled = false;
                }
                next:;
            }

            _suppressSlotChange = false;
        }

        private void OnSlotChanged(object? sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (_suppressSlotChange || _selectedSet == null || !_isCustomSet) return;
                if (sender is not ComboBox combo || combo.Tag is not int slotIndex) return;

                int selectedIdx = combo.SelectedIndex;
                if (selectedIdx < 0) return;

                var slot = AllSlots[slotIndex];

                // Only attribute slots (0-4) have the public EveAttribute indexer
                var attr = Implant.SlotToAttrib(slot);
                if (attr == EveAttribute.None) return; // Slots 6-10 are read-only in this editor

                var implants = StaticItems.GetImplants(slot);

                if (selectedIdx == 0)
                {
                    _selectedSet[attr] = new Implant(slot);
                }
                else
                {
                    var implant = implants.ElementAtOrDefault(selectedIdx - 1);
                    if (implant != null)
                        _selectedSet[attr] = implant;
                }

                UpdateBonusSummary();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error changing slot: {ex}");
            }
        }

        private void UpdateBonusSummary()
        {
            if (_selectedSet == null)
            {
                BonusSummaryText.Text = "";
                return;
            }

            var bonuses = new List<string>();
            var attrSlots = new[]
            {
                (EveAttribute.Perception, "PER"),
                (EveAttribute.Memory, "MEM"),
                (EveAttribute.Willpower, "WIL"),
                (EveAttribute.Intelligence, "INT"),
                (EveAttribute.Charisma, "CHA")
            };

            foreach (var (attr, label) in attrSlots)
            {
                var implant = _selectedSet[attr];
                if (implant != null && implant.Bonus > 0)
                    bonuses.Add($"{label} +{implant.Bonus}");
            }

            BonusSummaryText.Text = bonuses.Count > 0
                ? $"Bonuses: {string.Join(", ", bonuses)}"
                : "No attribute bonuses";
        }

        private async void OnNewCustomClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (_character == null) return;

                string? name = await ShowNameInputDialog("New Custom Set",
                    $"Custom Set {_character.ImplantSets.Count()}");
                if (string.IsNullOrWhiteSpace(name)) return;

                _character.ImplantSets.Add(name);
                RefreshSetList();
                SetListBox.SelectedIndex = _character.ImplantSets.Count() - 1;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error adding custom set: {ex}");
            }
        }

        private async void OnRenameClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedSet == null) return;

                string? name = await ShowNameInputDialog("Rename Set", _selectedSet.Name);
                if (string.IsNullOrWhiteSpace(name)) return;

                _selectedSet.Name = name;
                int idx = SetListBox.SelectedIndex;
                RefreshSetList();
                SetListBox.SelectedIndex = idx;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error renaming set: {ex}");
            }
        }

        private void OnDeleteClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (_character == null || _selectedSet == null || !_isCustomSet) return;

                _character.ImplantSets.Remove(_selectedSet);
                _selectedSet = null;
                RefreshSetList();
                SlotHeader.Text = "Select an implant set";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting set: {ex}");
            }
        }

        private async System.Threading.Tasks.Task<string?> ShowNameInputDialog(string title, string defaultName)
        {
            string? result = null;
            var nameBox = new TextBox
            {
                Text = defaultName,
                FontSize = FontScaleService.Subheading,
                Margin = new Thickness(0, 8, 0, 0),
                Watermark = "Enter name..."
            };

            var okBtn = new Button
            {
                Content = "OK",
                FontSize = FontScaleService.Body,
                Padding = new Thickness(12, 5),
                CornerRadius = new CornerRadius(12),
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 12, 0, 0)
            };

            var dialog = new Window
            {
                Title = title,
                Width = 320, Height = 160,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new StackPanel
                {
                    Margin = new Thickness(16),
                    Children =
                    {
                        new TextBlock { Text = "Name:", FontSize = FontScaleService.Subheading },
                        nameBox,
                        okBtn
                    }
                }
            };

            nameBox.AttachedToVisualTree += (_, _) => { nameBox.Focus(); nameBox.SelectAll(); };
            okBtn.Click += (_, _) =>
            {
                result = nameBox.Text?.Trim();
                if (!string.IsNullOrEmpty(result))
                    dialog.Close();
            };

            await dialog.ShowDialog(this);
            return result;
        }
    }
}
