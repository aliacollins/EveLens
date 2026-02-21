// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using Avalonia.Controls;

namespace EVEMon.Avalonia.Views.PlanEditor
{
    public partial class PlanTimeCard : UserControl
    {
        public PlanTimeCard()
        {
            InitializeComponent();
        }

        public void Update(TimeSpan trainingTime)
        {
            TotalTimeText.Text = FormatTime(trainingTime);

            if (trainingTime > TimeSpan.Zero)
            {
                DateTime completionDate = DateTime.UtcNow + trainingTime;
                CompletionText.Text = $"Finishes {completionDate:yyyy-MM-dd HH:mm} EVE";
            }
            else
            {
                CompletionText.Text = "All skills trained";
            }
        }

        private static string FormatTime(TimeSpan time)
        {
            if (time <= TimeSpan.Zero) return "Done";
            if (time.TotalDays >= 1) return $"{(int)time.TotalDays}d {time.Hours}h {time.Minutes}m";
            if (time.TotalHours >= 1) return $"{(int)time.TotalHours}h {time.Minutes}m";
            return $"{(int)time.TotalMinutes}m";
        }
    }
}
