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
    public partial class MarketOrdersWindow : EVEMonForm
    {
        private readonly bool m_init;

        /// <summary>
        /// Initializes a new instance of the <see cref="MarketOrdersWindow"/> class.
        /// </summary>
        public MarketOrdersWindow()
        {
            InitializeComponent();
            RememberPositionKey = "MarketOrdersWindow";
            m_init = true;
        }

        /// <summary>
        /// Gets or sets the grouping mode.
        /// </summary>
        internal Enum Grouping
        {
            get { return ordersList.Grouping; }
            set
            {
                ordersList.Grouping = value;

                if (!m_init)
                    return;

                ordersList.UpdateColumns();
                ordersList.Visibility = ordersList.Orders.Any();
            }
        }

        /// <summary>
        /// Gets or sets the ShowIssuedFor mode.
        /// </summary>
        internal IssuedFor ShowIssuedFor
        {
            get { return ordersList.ShowIssuedFor; }
            set
            {
                ordersList.ShowIssuedFor = value;

                if (!m_init)
                    return;

                ordersList.UpdateColumns();
                ordersList.Visible = ordersList.Orders.Any();
            }
        }

        /// <summary>
        /// Gets or sets the enumeration of orders to display.
        /// </summary>
        internal IEnumerable<MarketOrder> Orders
        {
            get { return ordersList.Orders; }
            set { ordersList.Orders = value; }
        }

        /// <summary>
        /// Gets or sets the enumeration of displayed columns.
        /// </summary>
        internal IEnumerable<IColumnSettings> Columns
        {
            get { return ordersList.Columns; }
            set
            {
                ordersList.Columns = value;

                if (!m_init)
                    return;

                ordersList.UpdateColumns();
                ordersList.Visibility = ordersList.Orders.Any();
            }
        }
    }
}