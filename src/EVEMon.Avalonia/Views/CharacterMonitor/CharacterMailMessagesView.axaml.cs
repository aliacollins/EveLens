using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using EVEMon.Common.Models;
using EVEMon.Common.ViewModels.Lists;

namespace EVEMon.Avalonia.Views.CharacterMonitor
{
    public partial class CharacterMailMessagesView : UserControl
    {
        private MailMessagesListViewModel? _viewModel;

        public CharacterMailMessagesView()
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
            if (_viewModel != null && DataContext is Character)
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
            if (character == null) return;

            _viewModel ??= new MailMessagesListViewModel();
            _viewModel.Character = character;
            DataContext = _viewModel;
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            _viewModel?.Dispose();
            _viewModel = null;
        }
    }
}
