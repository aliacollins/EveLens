// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using Avalonia.Controls;

namespace EveLens.Avalonia.Views.Shared
{
    public partial class MailReadingWindow : Window
    {
        public MailReadingWindow()
        {
            InitializeComponent();
        }

        public void SetMail(string subject, string sender, string date, string body)
        {
            var subjectBlock = this.FindControl<TextBlock>("SubjectText");
            if (subjectBlock != null)
                subjectBlock.Text = subject;

            var senderBlock = this.FindControl<TextBlock>("SenderText");
            if (senderBlock != null)
                senderBlock.Text = $"From: {sender}";

            var dateBlock = this.FindControl<TextBlock>("DateText");
            if (dateBlock != null)
                dateBlock.Text = $"Sent: {date}";

            var bodyBlock = this.FindControl<TextBlock>("BodyBlock");
            if (bodyBlock != null)
                bodyBlock.Text = body;

            Title = !string.IsNullOrEmpty(subject) ? subject : "EVE Mail";
        }
    }
}
