using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using EVEMon.Common.Models;
using EVEMon.Common.ViewModels;

namespace EVEMon.Avalonia.Views.PlanEditor
{
    public partial class PlanEditorWindow : Window
    {
        private PlanEditorViewModel? _viewModel;
        private PlanUnifiedView? _unifiedView;
        private PlanSkillBrowserView? _skillBrowserView;
        private PlanShipBrowserView? _shipBrowserView;
        private PlanItemBrowserView? _itemBrowserView;
        private PlanBlueprintBrowserView? _blueprintBrowserView;
        private PlanOptimizerFlyout? _optimizerFlyout;

        public PlanEditorWindow()
        {
            InitializeComponent();
        }

        public void Initialize(Plan plan, Character character)
        {
            _viewModel = new PlanEditorViewModel();
            _viewModel.Character = character;
            _viewModel.Plan = plan;

            PlanNameText.Text = plan.Name;
            CharacterNameText.Text = character.Name;

            _unifiedView = new PlanUnifiedView();

            ShowPlanTab();
            UpdateStatusBar();
        }

        private void ShowPlanTab()
        {
            if (_viewModel == null || _unifiedView == null) return;
            ContentPanel.Children.Clear();
            _unifiedView.SetViewModel(_viewModel);
            ContentPanel.Children.Add(_unifiedView);
            SetActiveTab(PlanTab);
        }

        private void ShowSkillsTab()
        {
            if (_viewModel == null) return;
            _skillBrowserView ??= new PlanSkillBrowserView();
            ContentPanel.Children.Clear();
            _skillBrowserView.SetViewModel(_viewModel);
            ContentPanel.Children.Add(_skillBrowserView);
            SetActiveTab(SkillsTab);
        }

        private void ShowShipsTab()
        {
            if (_viewModel == null) return;
            _shipBrowserView ??= new PlanShipBrowserView();
            ContentPanel.Children.Clear();
            _shipBrowserView.SetViewModel(_viewModel);
            ContentPanel.Children.Add(_shipBrowserView);
            SetActiveTab(ShipsTab);
        }

        private void ShowItemsTab()
        {
            if (_viewModel == null) return;
            _itemBrowserView ??= new PlanItemBrowserView();
            ContentPanel.Children.Clear();
            _itemBrowserView.SetViewModel(_viewModel);
            ContentPanel.Children.Add(_itemBrowserView);
            SetActiveTab(ItemsTab);
        }

        private void ShowBlueprintsTab()
        {
            if (_viewModel == null) return;
            _blueprintBrowserView ??= new PlanBlueprintBrowserView();
            ContentPanel.Children.Clear();
            _blueprintBrowserView.SetViewModel(_viewModel);
            ContentPanel.Children.Add(_blueprintBrowserView);
            SetActiveTab(BlueprintsTab);
        }

        /// <summary>
        /// Switches to the Skills browser tab. Called from dashboard "Add Skills" button.
        /// </summary>
        internal void SwitchToSkillBrowser()
        {
            ShowSkillsTab();
        }

        private void ShowOptimizerTab()
        {
            if (_viewModel == null) return;
            _optimizerFlyout ??= new PlanOptimizerFlyout();
            ContentPanel.Children.Clear();
            ContentPanel.Children.Add(_optimizerFlyout);
            SetActiveTab(OptimizeTab);

            // Trigger optimization if plan is available
            var plan = _viewModel.Plan;
            var character = _viewModel.Character as Character;
            if (plan != null && character != null)
            {
                var vm = new Common.ViewModels.PlanOptimizerViewModel();
                _optimizerFlyout.SetViewModel(vm);
                _optimizerFlyout.RunOptimization(plan, character);
            }
        }

        private void SetActiveTab(ToggleButton active)
        {
            PlanTab.IsChecked = active == PlanTab;
            SkillsTab.IsChecked = active == SkillsTab;
            ShipsTab.IsChecked = active == ShipsTab;
            ItemsTab.IsChecked = active == ItemsTab;
            BlueprintsTab.IsChecked = active == BlueprintsTab;
            OptimizeTab.IsChecked = active == OptimizeTab;
        }

        private void OnTabClicked(object? sender, RoutedEventArgs e)
        {
            if (sender == PlanTab) ShowPlanTab();
            else if (sender == SkillsTab) ShowSkillsTab();
            else if (sender == ShipsTab) ShowShipsTab();
            else if (sender == ItemsTab) ShowItemsTab();
            else if (sender == BlueprintsTab) ShowBlueprintsTab();
            else if (sender == OptimizeTab) ShowOptimizerTab();
        }

        private void OnSearchChanged(object? sender, TextChangedEventArgs e)
        {
            string filter = SearchBox.Text?.Trim() ?? "";
            ClearSearchBtn.IsVisible = !string.IsNullOrEmpty(filter);
            if (_unifiedView != null && ContentPanel.Children.Contains(_unifiedView))
                _unifiedView.RefreshSkillList(filter);
        }

        private void OnClearSearch(object? sender, RoutedEventArgs e)
        {
            SearchBox.Text = "";
            ClearSearchBtn.IsVisible = false;
            if (_unifiedView != null && ContentPanel.Children.Contains(_unifiedView))
                _unifiedView.RefreshSkillList("");
        }

        private void OnExport(object? sender, RoutedEventArgs e)
        {
            // TODO: Export functionality
        }

        internal void UpdateStatusBar()
        {
            if (_viewModel == null) return;
            var stats = _viewModel.PlanStats;
            StatusText.Text = $"Training time: {FormatTime(stats.TrainingTime)} | " +
                              $"Skills: {_viewModel.EntryCount} | " +
                              $"SP: {stats.TotalSkillPoints:N0}";
        }

        private static string FormatTime(TimeSpan time)
        {
            if (time <= TimeSpan.Zero) return "Done";
            if (time.TotalDays >= 1) return $"{(int)time.TotalDays}d {time.Hours}h {time.Minutes}m";
            if (time.TotalHours >= 1) return $"{(int)time.TotalHours}h {time.Minutes}m";
            return $"{(int)time.TotalMinutes}m";
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (_unifiedView == null || !ContentPanel.Children.Contains(_unifiedView))
                return;

            if (e.KeyModifiers == KeyModifiers.Alt && e.Key == Key.Up)
            {
                _unifiedView.MoveSelectionUp();
                e.Handled = true;
            }
            else if (e.KeyModifiers == KeyModifiers.Alt && e.Key == Key.Down)
            {
                _unifiedView.MoveSelectionDown();
                e.Handled = true;
            }
            else if (e.Key == Key.Delete && e.KeyModifiers == KeyModifiers.None)
            {
                _unifiedView.DeleteSelected();
                e.Handled = true;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _viewModel?.Dispose();
            base.OnClosed(e);
        }
    }
}
