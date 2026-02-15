using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using EVEMon.Common;
using EVEMon.Common.Constants;
using EVEMon.Common.Controls;
using EVEMon.Common.Events;
using EVEMon.Common.Helpers;
using EVEMon.Common.Services;

namespace EVEMon.Sales
{
    public partial class MineralWorksheet : EVEMonForm
    {
        private Decimal m_total;
        private string m_courtesyUrl;
        private string m_source;
        private bool m_pricesLocked;
        private IDisposable? _subSettingsChanged;


        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="MineralWorksheet"/> class.
        /// </summary>
        public MineralWorksheet()
        {
            InitializeComponent();
        }

        #endregion


        #region Properties

        /// <summary>
        /// Gets the tiles.
        /// </summary>
        /// <value>The tiles.</value>
        private IEnumerable<MineralTile> Tiles
            => MineralTileTableLayout.Controls.OfType<MineralTile>().Select(control => control);

        /// <summary>
        /// Sets a value indicating whether [prices locked].
        /// </summary>
        /// <value><c>true</c> if [prices locked]; otherwise, <c>false</c>.</value>
        private bool PricesLocked
        {
            set
            {
                m_pricesLocked = value;
                foreach (MineralTile mt in Tiles)
                {
                    mt.PriceLocked = value;
                }
            }
        }

        #endregion


        #region Helper Methods

        /// <summary>
        /// Updates the visuals.
        /// </summary>
        private void UpdateVisuals()
        {
            foreach (ToolStripItem item in MineralWorksheetToolStrip.Items)
            {
                item.DisplayStyle = Settings.UI.SafeForWork
                    ? ToolStripItemDisplayStyle.Text
                    : ToolStripItemDisplayStyle.ImageAndText;
            }

            foreach (ToolStripStatusLabel item in MineralWorksheetStatusStrip.Items)
            {
                item.DisplayStyle = Settings.UI.SafeForWork
                    ? ToolStripItemDisplayStyle.Text
                    : ToolStripItemDisplayStyle.ImageAndText;
            }

            foreach (MineralTile tile in Tiles)
            {
                tile.icon.Visible = !Settings.UI.SafeForWork;
            }
        }

        #endregion


        #region Local Events Handlers

        /// <summary>
        /// Handles the Load event of the MineralWorksheet control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void MineralWorksheet_Load(object sender, EventArgs e)
        {
            if (DesignMode || this.IsDesignModeHosted())
                return;

            MineralDataRequest.Initialize();

            _subSettingsChanged = AppServices.EventAggregator?.Subscribe<SettingsChangedEvent>(e => EveMonClient_SettingsChanged(null, EventArgs.Empty));
            Disposed += OnDiposed;

            UpdateVisuals();

            foreach (MineralTile mineralTile in Tiles)
            {
                mineralTile.SubtotalChanged += TileSubtotal_Changed;
            }

            foreach (IMineralParser parser in MineralDataRequest.Parsers)
            {
                ToolStripMenuItem menuItem;
                using (ToolStripMenuItem item = new ToolStripMenuItem())
                {
                    item.Text = parser.Title;
                    item.Tag = parser.Name;
                    item.Click += menuItem_Click;
                    menuItem = item;
                }
                tsddFetch.DropDownItems.Add(menuItem);
            }
        }

        /// <summary>
        /// Called when diposed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void OnDiposed(object sender, EventArgs e)
        {
            _subSettingsChanged?.Dispose();
            _subSettingsChanged = null;
            Disposed -= OnDiposed;
        }

        /// <summary>
        /// Handles the Changed event of the TileSubtotal control.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void TileSubtotal_Changed(object sender, EventArgs e)
        {
            m_total = 0;
            foreach (MineralTile mt in Tiles)
            {
                m_total += mt.Subtotal;
            }

            tslTotal.Text = $"{m_total:N} ISK";
        }

        #endregion


        #region Global Event Handlers

        /// <summary>
        /// Handles the SettingsChanged event of the EveMonClient control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void EveMonClient_SettingsChanged(object sender, EventArgs e)
        {
            UpdateVisuals();
        }

        #endregion


        #region Control Event Handlers

        /// <summary>
        /// Handles the Click event of the mi control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void menuItem_Click(object sender, EventArgs e)
        {
            if (bckgrndWrkrGetPrices.IsBusy)
                return;

            ToolStripMenuItem mi = sender as ToolStripMenuItem;
            if (mi == null || !(mi.Tag is string))
                return;

            m_source = mi.Tag as string;
            bckgrndWrkrGetPrices.RunWorkerAsync(m_source);
        }

        /// <summary>
        /// Handles the DoWork event of the bckgrndWrkrGetPrices control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.ComponentModel.DoWorkEventArgs"/> instance containing the event data.</param>
        private void bckgrndWrkrGetPrices_DoWork(object sender, DoWorkEventArgs e)
        {
            IDictionary<string, Decimal> prices = new Dictionary<string, decimal>();
            string source = e.Argument as string;
            if (source != null)
            {
                foreach (MineralPrice mineralPrice in MineralDataRequest.Prices(source))
                {
                    prices[mineralPrice.Name] = mineralPrice.Price;
                }
            }

            e.Result = prices;
        }

        /// <summary>
        /// Handles the RunWorkerCompleted event of the bckgrndWrkrGetPrices control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.ComponentModel.RunWorkerCompletedEventArgs"/> instance containing the event data.</param>
        private void bckgrndWrkrGetPrices_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                ExceptionHandler.LogException(e.Error, true);
                MessageBox.Show($"Failed to retrieve mineral pricing data:\n{e.Error.Message}",
                    @"Failed to Retrieve Data", MessageBoxButtons.OK, MessageBoxIcon.Error,
                    MessageBoxDefaultButton.Button1, 0);
            }
            else
            {
                IDictionary<string, Decimal> prices = e.Result as IDictionary<string, Decimal>;
                if (prices == null)
                    return;

                foreach (MineralTile mt in Tiles)
                {
                    mt.PricePerUnit = prices.ContainsKey(mt.MineralName) ? prices[mt.MineralName] : 0m;
                }

                tslCourtesy.Text = $"Mineral Prices Courtesy of {MineralDataRequest.GetCourtesyText(m_source)}";
                m_courtesyUrl = MineralDataRequest.GetCourtesyUrl(m_source).AbsoluteUri;
                tslCourtesy.Visible = true;
            }
        }

        /// <summary>
        /// Handles the Click event of the btnLockPrices control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void btnLockPrices_Click(object sender, EventArgs e)
        {
            if (m_pricesLocked)
            {
                PricesLocked = false;
                btnLockPrices.Text = @"Lock Prices";
            }
            else
            {
                PricesLocked = true;
                btnLockPrices.Text = @"Unlock Prices";
            }
        }

        /// <summary>
        /// Handles the Click event of the tslCourtesy control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void tslCourtesy_Click(object sender, EventArgs e)
        {
            Util.OpenURL(new Uri(m_courtesyUrl));
        }

        /// <summary>
        /// Handles the MineralPriceChanged event of the mt control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void mt_MineralPriceChanged(object sender, EventArgs e)
        {
            tslCourtesy.Visible = false;
        }

        /// <summary>
        /// Handles the Click event of the btnReset control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void btnReset_Click(object sender, EventArgs e)
        {
            foreach (MineralTile mt in Tiles)
            {
                mt.Quantity = 0;
            }
        }

        /// <summary>
        /// Handles the DropDownOpening event of the copyTotalDropDownButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void copyTotalDropDownButton_DropDownOpening(object sender, EventArgs e)
        {
            copyFormattedTotalToolStripMenuItem.Text = $"Formatted ({m_total:N} ISK)";
            copyUnformattedTotalToolStripMenuItem.Text = $"Unformatted ({m_total})";
        }

        /// <summary>
        /// Handles the Click event of the copyFormattedTotalToolStripMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void copyFormattedTotalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Clipboard.Clear();
                Clipboard.SetText(tslTotal.Text);
            }
            catch (ExternalException ex)
            {
                // An exception is thrown when the clipboard is in use by another procedure
                ExceptionHandler.LogException(ex, true);
            }
        }

        /// <summary>
        /// Handles the Click event of the copyUnformattedTotalToolStripMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void copyUnformattedTotalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Clipboard.Clear();
                Clipboard.SetText(m_total.ToString(CultureConstants.DefaultCulture));
            }
            catch (ExternalException ex)
            {
                // An exception is thrown when the clipboard is in use by another procedure
                ExceptionHandler.LogException(ex, true);
            }
        }

        #endregion
    }
}