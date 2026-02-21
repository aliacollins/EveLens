// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using Avalonia.Controls;

namespace EVEMon.Avalonia.Views.PlanEditor
{
    public partial class PlanCostCard : UserControl
    {
        public PlanCostCard()
        {
            InitializeComponent();
        }

        public void Update(long totalBooksCost, long notKnownBooksCost)
        {
            BooksCostText.Text = $"{totalBooksCost:N0} ISK";
            NotKnownCostText.Text = notKnownBooksCost > 0
                ? $"{notKnownBooksCost:N0} ISK (unowned books)"
                : "All skill books owned";
        }
    }
}
