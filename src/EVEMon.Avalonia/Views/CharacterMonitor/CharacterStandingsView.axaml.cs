using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using EVEMon.Common.Models;
using EVEMon.Common.ViewModels;

namespace EVEMon.Avalonia.Views.CharacterMonitor
{
    public partial class CharacterStandingsView : UserControl
    {
        public CharacterStandingsView()
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
            Character? character = DataContext as Character
                ?? (DataContext as ObservableCharacter)?.Character;
            if (character == null)
            {
                var parent = this.FindAncestorOfType<CharacterMonitorView>();
                character = (parent?.DataContext as ObservableCharacter)?.Character
                    ?? parent?.DataContext as Character;
            }
            if (character is not CCPCharacter ccp) return;

            var items = ccp.Standings.ToList();
            var grid = this.FindControl<DataGrid>("ItemsGrid");
            if (grid != null)
                grid.ItemsSource = items;
            var status = this.FindControl<TextBlock>("StatusText");
            if (status != null)
                status.Text = $"Standings: {items.Count}";
        }
    }
}
