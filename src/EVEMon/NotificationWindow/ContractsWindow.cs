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
    public partial class ContractsWindow : EVEMonForm
    {
        private readonly bool m_init;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContractsWindow"/> class.
        /// </summary>
        public ContractsWindow()
        {
            InitializeComponent();
            RememberPositionKey = "ContractsWindow";
            m_init = true;
        }

        /// <summary>
        /// Gets or sets the grouping mode.
        /// </summary>
        internal Enum Grouping
        {
            get { return contractsList.Grouping; }
            set
            {
                contractsList.Grouping = value;

                if (!m_init)
                    return;

                contractsList.UpdateColumns();
                contractsList.Visibility = contractsList.Contracts.Any();
            }
        }

        /// <summary>
        /// Gets or sets the showIssuedFor mode.
        /// </summary>
        internal IssuedFor ShowIssuedFor
        {
            get { return contractsList.ShowIssuedFor; }
            set
            {
                contractsList.ShowIssuedFor = value;

                if (!m_init)
                    return;

                contractsList.UpdateColumns();
                contractsList.Visible = contractsList.Contracts.Any();
            }
        }

        /// <summary>
        /// Gets or sets the enumeration of contracts to display.
        /// </summary>
        internal IEnumerable<Contract> Contracts
        {
            get { return contractsList.Contracts; }
            set { contractsList.Contracts = value; }
        }

        /// <summary>
        /// Gets or sets the enumeration of displayed columns.
        /// </summary>
        internal IEnumerable<IColumnSettings> Columns
        {
            get { return contractsList.Columns; }
            set
            {
                contractsList.Columns = value;

                if (!m_init)
                    return;

                contractsList.UpdateColumns();
                contractsList.Visibility = contractsList.Contracts.Any();
            }
        }
    }
}
