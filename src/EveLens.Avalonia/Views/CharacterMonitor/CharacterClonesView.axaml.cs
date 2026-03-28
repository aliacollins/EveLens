// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using EveLens.Avalonia.Converters;
using EveLens.Common.Events;
using EveLens.Common.Helpers;
using EveLens.Common.Models;
using EveLens.Common.Service;
using EveLens.Common.Services;
using EveLens.Common.ViewModels;

using EveLens.Common.ViewModels;
using EveLens.Avalonia.Services;
namespace EveLens.Avalonia.Views.CharacterMonitor
{
    public partial class CharacterClonesView : UserControl
    {
        private ClonesViewModel? _viewModel;
        private IDisposable? _clonesSub;
        private long _characterId;

        public CharacterClonesView()
        {
            InitializeComponent();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _clonesSub ??= AppServices.EventAggregator?.Subscribe<CharacterImplantSetCollectionChangedEvent>(OnClonesUpdated);
            LoadData();
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            LoadData();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            _clonesSub?.Dispose();
            _clonesSub = null;
            _viewModel?.Dispose();
            _viewModel = null;
        }

        private void LoadData()
        {
            if (this.GetVisualRoot() == null) return;

            Character? character = DataContext as Character
                ?? (DataContext as ObservableCharacter)?.Character;
            if (character == null)
            {
                var parent = this.FindAncestorOfType<CharacterMonitorView>();
                character = (parent?.DataContext as ObservableCharacter)?.Character
                    ?? parent?.DataContext as Character;
            }
            if (character is not CCPCharacter) return;

            _characterId = character.CharacterID;

            // Scope check — clone scope may not be authorized in custom preset
            var parentView = this.FindAncestorOfType<CharacterMonitorView>();
            var oc = parentView?.DataContext as ObservableCharacter;
            if (oc != null && !oc.HasScopeFor(Common.Enumerations.CCPAPI.ESIAPICharacterMethods.Clones))
            {
                ScopePrompt.IsVisible = true;
                DataContent.IsVisible = false;
                return;
            }
            ScopePrompt.IsVisible = false;
            DataContent.IsVisible = true;

            _viewModel ??= new ClonesViewModel();
            if (_viewModel.Character != character)
                _viewModel.Character = character;
            else
                _viewModel.ForceRefresh();

            BuildUI();
            UpdateStatus();
        }

        private void BuildUI()
        {
            if (_viewModel == null) return;

            ClonePanel.Children.Clear();

            // Load persisted expand state
            var expandState = CollapseStateHelper.LoadExpandState(_characterId, "Clones");
            bool hasSaved = CollapseStateHelper.HasSavedState(_characterId, "Clones");

            // Active Clone section
            if (_viewModel.ActiveClone != null)
            {
                bool expanded = !hasSaved || expandState.Contains(_viewModel.ActiveClone.Name);
                ClonePanel.Children.Add(BuildCloneSection(_viewModel.ActiveClone, _viewModel.HomeStationName, expanded));
            }

            // Jump Clones
            foreach (var clone in _viewModel.JumpClones)
            {
                bool expanded = !hasSaved || expandState.Contains(clone.Name);
                ClonePanel.Children.Add(BuildCloneSection(clone, null, expanded));
            }
        }

        private Control BuildCloneSection(CloneDisplayEntry clone, string? homeStation, bool initiallyExpanded = true)
        {
            var headerGrid = new Grid
            {
                ColumnDefinitions = ColumnDefinitions.Parse("Auto,*,Auto"),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var chevron = new TextBlock
            {
                Text = "\u25BC",
                FontSize = FontScaleService.Body,
                Width = 16,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = FindBrush("EveTextSecondaryBrush")
            };
            Grid.SetColumn(chevron, 0);
            headerGrid.Children.Add(chevron);

            var nameText = new TextBlock
            {
                Text = clone.Name,
                FontSize = FontScaleService.Body,
                FontWeight = FontWeight.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Foreground = FindBrush("EveAccentPrimaryBrush")
            };
            Grid.SetColumn(nameText, 1);
            headerGrid.Children.Add(nameText);

            var countText = new TextBlock
            {
                Text = clone.TotalImplantCount > 0
                    ? $"{clone.TotalImplantCount} implant{(clone.TotalImplantCount != 1 ? "s" : "")}"
                    : "No implants",
                FontSize = FontScaleService.Small,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0),
                Foreground = FindBrush("EveTextSecondaryBrush")
            };
            Grid.SetColumn(countText, 2);
            headerGrid.Children.Add(countText);

            var headerBorder = new Border
            {
                Background = FindBrush("EveBackgroundMediumBrush"),
                Padding = new Thickness(8, 6),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Child = headerGrid
            };

            // Content panel
            var contentPanel = new StackPanel { Spacing = 0 };

            // Home station line (only for active clone)
            if (homeStation != null)
            {
                contentPanel.Children.Add(BuildInfoRow("Home Station", homeStation));
            }

            // Implant summary line
            contentPanel.Children.Add(BuildInfoRow("Implants", clone.ImplantSummary));

            // Individual implant lines with icons
            foreach (var implant in clone.Implants)
            {
                var implantRow = new Grid
                {
                    ColumnDefinitions = ColumnDefinitions.Parse("Auto,Auto,*"),
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };

                // Implant icon (loaded async from EVE image server)
                var iconImage = new Image
                {
                    Width = 24, Height = 24,
                    Margin = new Thickness(0, 0, 6, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(iconImage, 0);
                implantRow.Children.Add(iconImage);
                LoadImplantIconAsync(iconImage, implant.TypeId);

                var slotText = new TextBlock
                {
                    Text = implant.Bonus > 0 ? $"+{implant.Bonus} {implant.SlotLabel}" : implant.SlotLabel,
                    FontSize = FontScaleService.Small,
                    MinWidth = 60,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = FindBrush("EveAccentPrimaryBrush")
                };
                Grid.SetColumn(slotText, 1);
                implantRow.Children.Add(slotText);

                var implantName = new TextBlock
                {
                    Text = implant.Name,
                    FontSize = FontScaleService.Small,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    Foreground = FindBrush("EveTextPrimaryBrush")
                };
                Grid.SetColumn(implantName, 2);
                implantRow.Children.Add(implantName);

                contentPanel.Children.Add(new Border
                {
                    Padding = new Thickness(28, 2, 8, 2),
                    Background = FindBrush("EveBackgroundDarkBrush"),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Child = implantRow
                });
            }

            // If no implants, show a message
            if (clone.TotalImplantCount == 0)
            {
                contentPanel.Children.Add(new Border
                {
                    Padding = new Thickness(28, 4, 8, 4),
                    Background = FindBrush("EveBackgroundDarkBrush"),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Child = new TextBlock
                    {
                        Text = "No implants installed",
                        FontSize = FontScaleService.Small,
                        Foreground = FindBrush("EveTextDisabledBrush"),
                        FontStyle = FontStyle.Italic
                    }
                });
            }

            var expander = new StackPanel { Spacing = 0 };
            expander.Children.Add(headerBorder);
            expander.Children.Add(contentPanel);

            // Apply initial state
            contentPanel.IsVisible = initiallyExpanded;
            chevron.Text = initiallyExpanded ? "\u25BC" : "\u25B6";

            // Click to toggle
            headerBorder.Tag = contentPanel;
            headerBorder.PointerPressed += (s, e) =>
            {
                if (s is Border b && b.Tag is StackPanel content)
                {
                    content.IsVisible = !content.IsVisible;
                    // Update chevron
                    if (b.Child is Grid g && g.Children[0] is TextBlock tb)
                        tb.Text = content.IsVisible ? "\u25BC" : "\u25B6";
                    SaveCollapseState();
                }
            };

            return new Border
            {
                Margin = new Thickness(0, 0, 0, 1),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Child = expander
            };
        }

        private Border BuildInfoRow(string label, string value)
        {
            var grid = new Grid
            {
                ColumnDefinitions = ColumnDefinitions.Parse("Auto,*"),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var labelText = new TextBlock
            {
                Text = label + ":",
                FontSize = FontScaleService.Small,
                MinWidth = 90,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = FindBrush("EveTextSecondaryBrush")
            };
            Grid.SetColumn(labelText, 0);
            grid.Children.Add(labelText);

            var valueText = new TextBlock
            {
                Text = value,
                FontSize = FontScaleService.Small,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Foreground = FindBrush("EveTextPrimaryBrush")
            };
            Grid.SetColumn(valueText, 1);
            grid.Children.Add(valueText);

            return new Border
            {
                Padding = new Thickness(20, 3, 8, 3),
                Background = FindBrush("EveBackgroundDarkBrush"),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Child = grid
            };
        }

        private void UpdateStatus()
        {
            if (_viewModel == null) return;

            string jumpStatus = _viewModel.CloneJumpAvailable
                ? "Clone Jump: Ready"
                : $"Clone Jump: {_viewModel.CloneJumpStatusText}";

            StatusText.Text = $"{jumpStatus} | {_viewModel.JumpCloneCount} jump clone{(_viewModel.JumpCloneCount != 1 ? "s" : "")} | Last jump: {_viewModel.LastCloneJumpText}";
        }

        private IBrush? FindBrush(string key)
        {
            if (this.TryFindResource(key, this.ActualThemeVariant, out var resource) && resource is IBrush brush)
                return brush;
            return null;
        }

        private void OnCollapseAll(object? sender, RoutedEventArgs e)
        {
            foreach (var child in ClonePanel.Children)
            {
                if (child is Border b && b.Child is StackPanel sp && sp.Children.Count >= 2
                    && sp.Children[1] is StackPanel content)
                {
                    content.IsVisible = false;
                    if (sp.Children[0] is Border header && header.Child is Grid g
                        && g.Children[0] is TextBlock tb)
                        tb.Text = "\u25B6";
                }
            }
            SaveCollapseState();
        }

        private void OnExpandAll(object? sender, RoutedEventArgs e)
        {
            foreach (var child in ClonePanel.Children)
            {
                if (child is Border b && b.Child is StackPanel sp && sp.Children.Count >= 2
                    && sp.Children[1] is StackPanel content)
                {
                    content.IsVisible = true;
                    if (sp.Children[0] is Border header && header.Child is Grid g
                        && g.Children[0] is TextBlock tb)
                        tb.Text = "\u25BC";
                }
            }
            SaveCollapseState();
        }

        private void SaveCollapseState()
        {
            if (_characterId == 0 || _viewModel == null) return;
            var expanded = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);

            if (_viewModel.ActiveClone != null)
            {
                // Check if first child's content panel is visible
                if (ClonePanel.Children.Count > 0 && ClonePanel.Children[0] is Border b
                    && b.Child is StackPanel sp && sp.Children.Count >= 2
                    && sp.Children[1] is StackPanel content && content.IsVisible)
                    expanded.Add(_viewModel.ActiveClone.Name);
            }

            int offset = _viewModel.ActiveClone != null ? 1 : 0;
            for (int i = 0; i < _viewModel.JumpClones.Count; i++)
            {
                int childIdx = i + offset;
                if (childIdx < ClonePanel.Children.Count
                    && ClonePanel.Children[childIdx] is Border jb
                    && jb.Child is StackPanel jsp && jsp.Children.Count >= 2
                    && jsp.Children[1] is StackPanel jcontent && jcontent.IsVisible)
                    expanded.Add(_viewModel.JumpClones[i].Name);
            }

            CollapseStateHelper.SaveExpandState(_characterId, "Clones", expanded);
        }

        private static async void LoadImplantIconAsync(Image target, int typeId)
        {
            if (typeId <= 0) return;
            try
            {
                var url = ImageHelper.GetTypeImageURL(typeId, 32);
                var skBitmap = await ImageService.GetImageAsync(url).ConfigureAwait(false);
                if (skBitmap != null)
                {
                    var converted = DrawingImageToAvaloniaConverter.Instance.Convert(
                        skBitmap, typeof(Bitmap), null, System.Globalization.CultureInfo.InvariantCulture);
                    if (converted is Bitmap bitmap)
                    {
                        global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            target.Source = bitmap;
                        });
                    }
                }
            }
            catch
            {
                // Icon loading is best-effort — don't crash the view
            }
        }

        private void OnClonesUpdated(CharacterImplantSetCollectionChangedEvent evt)
        {
            if (evt.Character?.CharacterID == _characterId)
                global::Avalonia.Threading.Dispatcher.UIThread.Post(LoadData);
        }
    }
}
