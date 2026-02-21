// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Windows.Forms;
using EVEMon.Common.Controls;

namespace EVEMon.Schedule
{
    public partial class DateSelectWindow : EVEMonForm
    {
        private int m_numClicks;
        private DateTime m_firstClick = DateTime.MinValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="DateSelectWindow"/> class.
        /// </summary>
        public DateSelectWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Gets or sets the selected date.
        /// </summary>
        /// <value>The selected date.</value>
        public DateTime SelectedDate
        {
            get { return monthCalendar1.SelectionStart; }
            set
            {
                monthCalendar1.SelectionStart = value;
                monthCalendar1.SelectionEnd = value;
            }
        }

        /// <summary>
        /// Handles the Click event of the btnCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        /// <summary>
        /// Handles the Click event of the btnOk control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void btnOk_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        /// <summary>
        /// Handles the DateSelected event of the monthCalendar1 control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Forms.DateRangeEventArgs"/> instance containing the event data.</param>
        private void monthCalendar1_DateSelected(object sender, DateRangeEventArgs e)
        {
            if (m_numClicks >= 2)
                btnOk_Click(this, new EventArgs());
        }

        /// <summary>
        /// Handles the MouseDown event of the monthCalendar1 control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Forms.MouseEventArgs"/> instance containing the event data.</param>
        private void monthCalendar1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            if (DateTime.Now - TimeSpan.FromMilliseconds(500) > m_firstClick)
            {
                m_numClicks = 0;
                m_firstClick = DateTime.Now;
            }
            m_numClicks++;
        }
    }
}