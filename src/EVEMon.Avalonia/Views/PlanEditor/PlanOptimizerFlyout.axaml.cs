using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using EVEMon.Common.Enumerations;
using EVEMon.Common.Models;
using EVEMon.Common.ViewModels;

namespace EVEMon.Avalonia.Views.PlanEditor
{
    public partial class PlanOptimizerFlyout : UserControl
    {
        private PlanOptimizerViewModel? _viewModel;

        // Attribute color brushes
        private static readonly IBrush AttrIntBrush = new SolidColorBrush(Color.Parse("#FF4FC3F7"));
        private static readonly IBrush AttrPerBrush = new SolidColorBrush(Color.Parse("#FFEF5350"));
        private static readonly IBrush AttrChaBrush = new SolidColorBrush(Color.Parse("#FF66BB6A"));
        private static readonly IBrush AttrWilBrush = new SolidColorBrush(Color.Parse("#FFAB47BC"));
        private static readonly IBrush AttrMemBrush = new SolidColorBrush(Color.Parse("#FFFFA726"));

        public PlanOptimizerFlyout()
        {
            InitializeComponent();
        }

        public void SetViewModel(PlanOptimizerViewModel viewModel)
        {
            if (_viewModel != null)
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

            _viewModel = viewModel;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            UpdateDisplay();
        }

        public void RunOptimization(BasePlan plan, Character character)
        {
            if (_viewModel == null) return;
            NoPlanText.IsVisible = false;
            _viewModel.RunOptimization(plan, character);
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(PlanOptimizerViewModel.IsCalculating)
                or nameof(PlanOptimizerViewModel.HasResults))
            {
                global::Avalonia.Threading.Dispatcher.UIThread.Post(UpdateDisplay);
            }
        }

        private void UpdateDisplay()
        {
            if (_viewModel == null) return;

            CalculatingText.IsVisible = _viewModel.IsCalculating;
            ResultsPanel.IsVisible = _viewModel.HasResults;

            if (_viewModel.HasResults)
            {
                CurrentTimeText.Text = _viewModel.CurrentDurationText;
                OptimalTimeText.Text = _viewModel.OptimalDurationText;
                TimeSavedText.Text = _viewModel.TimeSavedText;
                BuildAttributeRows();
            }
        }

        private void BuildAttributeRows()
        {
            if (_viewModel == null) return;
            AttributeRows.Children.Clear();

            var attributes = new[]
            {
                (EveAttribute.Intelligence, "Intelligence", AttrIntBrush),
                (EveAttribute.Perception, "Perception", AttrPerBrush),
                (EveAttribute.Charisma, "Charisma", AttrChaBrush),
                (EveAttribute.Willpower, "Willpower", AttrWilBrush),
                (EveAttribute.Memory, "Memory", AttrMemBrush),
            };

            foreach (var (attr, name, brush) in attributes)
            {
                int current = _viewModel.GetCurrent(attr);
                int optimal = _viewModel.GetOptimal(attr);
                int delta = optimal - current;

                var row = new Border
                {
                    Background = new SolidColorBrush(Color.Parse("#FF16213E")),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(10, 6),
                };

                var grid = new Grid
                {
                    ColumnDefinitions = ColumnDefinitions.Parse("120,50,30,50,*,Auto"),
                };

                // Attribute name
                grid.Children.Add(MakeText(name, 11, brush, 0));
                // Current value
                grid.Children.Add(MakeText(current.ToString(), 11,
                    new SolidColorBrush(Color.Parse("#FFF0F0F0")), 1, HorizontalAlignment.Center));
                // Arrow
                grid.Children.Add(MakeText("\u2192", 11,
                    new SolidColorBrush(Color.Parse("#FF707070")), 2, HorizontalAlignment.Center));
                // Optimal value
                grid.Children.Add(MakeText(optimal.ToString(), 11,
                    new SolidColorBrush(Color.Parse("#FFF0F0F0")), 3, HorizontalAlignment.Center));

                // Delta badge
                if (delta != 0)
                {
                    string deltaText = delta > 0 ? $"+{delta}" : delta.ToString();
                    var deltaBrush = delta > 0
                        ? new SolidColorBrush(Color.Parse("#FF81C784"))
                        : new SolidColorBrush(Color.Parse("#FFCF6679"));
                    var deltaBg = delta > 0
                        ? new SolidColorBrush(Color.Parse("#2581C784"))
                        : new SolidColorBrush(Color.Parse("#25CF6679"));

                    var badge = new Border
                    {
                        Background = deltaBg,
                        CornerRadius = new CornerRadius(8),
                        Padding = new Thickness(6, 1),
                        Margin = new Thickness(8, 0, 0, 0),
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center,
                        Child = new TextBlock
                        {
                            Text = deltaText,
                            FontSize = 10,
                            Foreground = deltaBrush,
                        }
                    };
                    Grid.SetColumn(badge, 5);
                    grid.Children.Add(badge);
                }

                row.Child = grid;
                AttributeRows.Children.Add(row);
            }
        }

        private static TextBlock MakeText(string text, double fontSize, IBrush foreground, int column,
            HorizontalAlignment hAlign = HorizontalAlignment.Left)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontSize = fontSize,
                Foreground = foreground,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = hAlign,
            };
            Grid.SetColumn(tb, column);
            return tb;
        }

        private void OnApplyRemap(object? sender, RoutedEventArgs e)
        {
            // Future: Apply the remapping to character
        }
    }
}
