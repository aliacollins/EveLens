// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using Avalonia;
using Avalonia.Controls;

namespace EVEMon.Avalonia.Views.Shared
{
    public partial class EmptyState : UserControl
    {
        public static readonly StyledProperty<string> TitleProperty =
            AvaloniaProperty.Register<EmptyState, string>(nameof(Title), string.Empty);

        public static readonly StyledProperty<string> SubtitleProperty =
            AvaloniaProperty.Register<EmptyState, string>(nameof(Subtitle), string.Empty);

        public string Title
        {
            get => GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string Subtitle
        {
            get => GetValue(SubtitleProperty);
            set => SetValue(SubtitleProperty, value);
        }

        public EmptyState()
        {
            InitializeComponent();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == TitleProperty)
            {
                var block = this.FindControl<TextBlock>("TitleBlock");
                if (block != null)
                    block.Text = change.GetNewValue<string>();
            }
            else if (change.Property == SubtitleProperty)
            {
                var block = this.FindControl<TextBlock>("SubtitleBlock");
                if (block != null)
                    block.Text = change.GetNewValue<string>();
            }
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            var titleBlock = this.FindControl<TextBlock>("TitleBlock");
            if (titleBlock != null) titleBlock.Text = Title;
            var subtitleBlock = this.FindControl<TextBlock>("SubtitleBlock");
            if (subtitleBlock != null) subtitleBlock.Text = Subtitle;
        }
    }
}
