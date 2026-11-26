// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Enumerations;
using EveLens.Common.Helpers;
using System;
using Avalonia.Controls;

namespace EveLens.Avalonia.Views.PlanEditor
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
                DateTime completionDate = PlanSchedule.CompletionDate(DateTime.UtcNow, trainingTime);
                CompletionText.Text = $"Finishes {completionDate:yyyy-MM-dd HH:mm} EVE";
            }
            else
            {
                CompletionText.Text = "All skills trained";
            }
        }

        private static string FormatTime(TimeSpan time)
            => TimeFormatHelper.Format(time, DurationStyle.Full, "Done");
    }
}
