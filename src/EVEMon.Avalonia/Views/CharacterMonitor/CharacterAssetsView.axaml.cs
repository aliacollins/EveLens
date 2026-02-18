using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using EVEMon.Common.Models;
using EVEMon.Common.ViewModels;

namespace EVEMon.Avalonia.Views.CharacterMonitor
{
    public partial class CharacterAssetsView : UserControl
    {
        private AssetBrowserViewModel? _viewModel;

        public CharacterAssetsView()
        {
            InitializeComponent();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            LoadData();
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            LoadData();
        }

        private void LoadData()
        {
            Character? character = DataContext as Character;
            if (character == null)
            {
                var parent = this.FindAncestorOfType<CharacterMonitorView>();
                character = parent?.DataContext as Character;
            }
            if (character is not CCPCharacter) return;

            _viewModel ??= new AssetBrowserViewModel();
            _viewModel.Character = character;

            AssetGroupsList.ItemsSource = _viewModel.Groups;

            var statusCtl = this.FindControl<TextBlock>("StatusText");
            if (statusCtl != null)
                statusCtl.Text = $"Items: {_viewModel.TotalItems:N0} in {_viewModel.GroupCount} locations  |  Est. Value: {_viewModel.TotalValue:N0} ISK";
        }

        private void OnGroupByChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_viewModel == null) return;
            _viewModel.GroupMode = GroupByCombo.SelectedIndex;
            LoadData();
        }

        private void OnFilterChanged(object? sender, TextChangedEventArgs e)
        {
            if (_viewModel == null) return;
            _viewModel.Filter = FilterBox.Text?.Trim() ?? string.Empty;
            ClearFilterBtn.IsVisible = !string.IsNullOrEmpty(_viewModel.Filter);
            LoadData();
        }

        private void OnClearFilter(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;
            FilterBox.Text = string.Empty;
            _viewModel.Filter = string.Empty;
            ClearFilterBtn.IsVisible = false;
            LoadData();
        }

        private void OnCollapseAll(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;
            _viewModel.CollapseAll();
            LoadData();
        }

        private void OnExpandAll(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;
            _viewModel.ExpandAll();
            LoadData();
        }
    }
}
