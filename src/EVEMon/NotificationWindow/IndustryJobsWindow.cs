// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EVEMon.Common.Controls;
using EVEMon.Common.Enumerations;
using EVEMon.Common.Models;
using EVEMon.Common.SettingsObjects;

namespace EVEMon.NotificationWindow
{
    public partial class IndustryJobsWindow : EVEMonForm
    {
        private readonly bool m_init;

        /// <summary>
        /// Initializes a new instance of the <see cref="IndustryJobsWindow"/> class.
        /// </summary>
        public IndustryJobsWindow()
        {
            InitializeComponent();
            RememberPositionKey = "IndustryJobsWindow";
            m_init = true;
        }

        /// <summary>
        /// Gets or sets the grouping mode.
        /// </summary>
        internal Enum Grouping
        {
            get { return jobsList.Grouping; }
            set
            {
                jobsList.Grouping = value;

                if (!m_init)
                    return;

                jobsList.UpdateColumns();
                jobsList.Visibility = jobsList.Jobs.Any();
            }
        }

        /// <summary>
        /// Gets or sets the showIssuedFor mode.
        /// </summary>
        internal IssuedFor ShowIssuedFor
        {
            get { return jobsList.ShowIssuedFor; }
            set
            {
                jobsList.ShowIssuedFor = value;

                if (!m_init)
                    return;

                jobsList.UpdateColumns();
                jobsList.Visible = jobsList.Jobs.Any();
            }
        }

        /// <summary>
        /// Gets or sets the enumeration of jobs to display.
        /// </summary>
        internal IEnumerable<IndustryJob> Jobs
        {
            get { return jobsList.Jobs; }
            set { jobsList.Jobs = value; }
        }

        /// <summary>
        /// Gets or sets the enumeration of displayed columns.
        /// </summary>
        internal IEnumerable<IColumnSettings> Columns
        {
            get { return jobsList.Columns; }
            set
            {
                jobsList.Columns = value;

                if (!m_init)
                    return;

                jobsList.UpdateColumns();
                jobsList.Visibility = jobsList.Jobs.Any();
            }
        }
    }
}