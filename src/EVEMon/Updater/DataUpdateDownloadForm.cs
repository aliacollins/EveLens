using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using EVEMon.Common;
using EVEMon.Common.Controls;
using EVEMon.Common.Helpers;
using EVEMon.Common.Net;
using EVEMon.Common.Serialization.PatchXml;
using EVEMon.Common.Threading;

namespace EVEMon.Updater
{
    public partial class DataUpdateDownloadForm : EVEMonForm
    {
        private TableLayoutPanel m_tableLayoutPanel = null!;

        private readonly List<DataUpdateDownloadControl> m_controls = null!;
        private readonly List<SerializableDatafile> m_datafiles = null!;
        private bool m_canceling;

        /// <summary>
        /// Prevents a default instance of the <see cref="DataUpdateDownloadForm"/> class from being created.
        /// </summary>
        private DataUpdateDownloadForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataUpdateDownloadForm"/> class.
        /// </summary>
        /// <param name="datafiles">The datafiles.</param>
        public DataUpdateDownloadForm(List<SerializableDatafile> datafiles)
            : this()
        {
            m_datafiles = datafiles;
            m_controls = new List<DataUpdateDownloadControl>();

            InitilizeControls();
        }

        /// <summary>
        /// Initilizes the controls.
        /// </summary>
        private void InitilizeControls()
        {
            SuspendLayout();

            m_tableLayoutPanel = new TableLayoutPanel
            {
                AutoSize = true,
                ColumnCount = 1,
                Dock = DockStyle.Top,
                Location = new Point(0, 0),
                Name = "m_tableLayoutPanel",
                RowCount = m_datafiles.Count,
                TabIndex = 0,
            };

            m_tableLayoutPanel.ColumnStyles.Add(new ColumnStyle());

            foreach (SerializableDatafile datafile in m_datafiles)
            {
                DataUpdateDownloadControl control = new DataUpdateDownloadControl(datafile)
                {
                    Dock = DockStyle.Fill,
                    Margin = new Padding(10)
                };
                m_controls.Add(control);

                m_tableLayoutPanel.RowStyles.Add(new RowStyle());
                m_tableLayoutPanel.Controls.Add(control, 0, m_datafiles.IndexOf(datafile));
            }

            int height = m_tableLayoutPanel.PreferredSize.Height + ClientSize.Height;

            btCancel.Location = new Point(325, height - btCancel.Height - 14);
            Controls.Add(m_tableLayoutPanel);
            ClientSize = new Size(ClientSize.Width, height);
            ResumeLayout(false);
            PerformLayout();
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Forms.Form.Shown" /> event.
        /// </summary>
        /// <param name="e">A <see cref="T:System.EventArgs" /> that contains the event data.</param>
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            do
            {
                Application.DoEvents();
            } while (!m_canceling && m_controls.Any(control => control.Tag == null || !(bool)control.Tag));

            Close();
        }

        /// <summary>
        /// Handles the FormClosing event of the UpdateDownloadForm control.
        /// </summary>
        /// <param name="e">The <see cref="FormClosingEventArgs"/> instance containing the event data.</param>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            if (!Visible ||
                (e.CloseReason != CloseReason.ApplicationExitCall && e.CloseReason != CloseReason.TaskManagerClosing &&
                 e.CloseReason != CloseReason.WindowsShutDown))
            {
                return;
            }

            Cancel();
        }


        /// <summary>
        /// Handles the Click event of the btCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void btCancel_Click(object? sender, EventArgs e)
        {
            Cancel();
        }

        /// <summary>
        /// Cancels this instance.
        /// </summary>
        private void Cancel()
        {
            m_canceling = true;
            m_controls.ForEach(control => control?.WebClient?.CancelAsync());
        }


        #region DataUpdateDownloadControl class

        private class DataUpdateDownloadControl : UserControl
        {
            private Label m_label;
            private Label m_progressLabel;
            private ProgressBar m_progressBar = null!;

            private readonly SerializableDatafile m_datafile;
            private readonly string m_tempFilename;

            /// <summary>
            /// Initializes a new instance of the <see cref="DataUpdateDownloadControl"/> class.
            /// </summary>
            /// <param name="datafile">The datafile.</param>
            internal DataUpdateDownloadControl(SerializableDatafile datafile)
            {
                InitializeComponent();

                m_datafile = datafile;
                m_label!.Text = $"{datafile.Name}";
                m_progressLabel!.Text = @"Downloading update...";
                m_tempFilename = Path.Combine(EveMonClient.EVEMonDataDir, $"{datafile.Name}.tmp");

                WebClient = HttpWebClientService.GetWebClient();
            }

            /// <summary>
            /// Initializes the component.
            /// </summary>
            private void InitializeComponent()
            {
                SuspendLayout();

                m_label = new Label
                {
                    AutoSize = true,
                    Location = new Point(0, 0),
                    Name = "lblDatafileName"
                };

                m_progressLabel = new Label
                {
                    AutoSize = true,
                    Location = new Point(0, 35),
                    Name = "lblProgress"
                };

                m_progressBar = new ProgressBar
                {
                    Location = new Point(3, 15),
                    Name = "pbProgress",
                    Size = new Size(385, 18)
                };

                Controls.Add(m_progressLabel);
                Controls.Add(m_progressBar);
                Controls.Add(m_label);

                ClientSize = PreferredSize;
                ResumeLayout(false);
                PerformLayout();
            }

            /// <summary>
            /// Raises the <see cref="E:System.Windows.Forms.Control.OnVisibleChangeds"/> event.
            /// </summary>
            /// <param name="e">An <see cref="T:System.EventArgs" /> that contains the event data.</param>
            protected override void OnVisibleChanged(EventArgs e)
            {
                base.OnVisibleChanged(e);

                BeginDownload();
            }

            /// <summary>
            /// Gets the web client.
            /// </summary>
            /// <value>
            /// The web client.
            /// </value>
            internal WebClient WebClient { get; }

            /// <summary>
            /// Begins the download.
            /// </summary>
            private void BeginDownload()
            {
                Uri url = new Uri($"{m_datafile.Address}/{m_datafile.Name}");
                string urlValidationError;
                if (!HttpWebClientService.IsValidURL(url, out urlValidationError))
                    return;

                if (File.Exists(m_tempFilename))
                    FileHelper.DeleteFile(m_tempFilename);

                try
                {
                    using (WebClient)
                    {
                        WebClient.DownloadFileCompleted += DownloadCompleted;
                        WebClient.DownloadProgressChanged += ProgressChanged;

                        try
                        {
                            WebClient.DownloadFileAsync(url, m_tempFilename);
                        }
                        catch (WebException ex)
                        {
                            throw HttpWebClientServiceException.HttpWebClientException(url, ex);
                        }
                        catch (Exception ex)
                        {
                            throw HttpWebClientServiceException.Exception(url, ex);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ExceptionHandler.LogRethrowException(ex);
                    throw;
                }
            }

            /// <summary>
            /// Handles the download progress changed event.
            /// </summary>
            /// <param name="sender">The sender.</param>
            /// <param name="e">The <see cref="DownloadProgressChangedEventArgs" /> instance containing the event data.</param>
            private void ProgressChanged(object? sender, DownloadProgressChangedEventArgs e)
            {
                if (InvokeRequired)
                {
                    Dispatcher.Invoke(() => ProgressChanged(sender, e));
                    return;
                }

                if (e.TotalBytesToReceive > 0)
                {
                    m_progressLabel.Text = $"Downloading update ({e.ProgressPercentage}%, " +
                                           $"{e.BytesReceived:N0} of {e.TotalBytesToReceive:N0} bytes received)...";
                    m_progressBar.Style = ProgressBarStyle.Continuous;

                    // Under Vista and Windows 7 there is a lag when progress bar updates too quick.
                    // This hackish way though solves this issue (in a way) as explained in
                    // http://stackoverflow.com/questions/977278/how-can-i-make-the-progress-bar-update-fast-enough/1214147#1214147.
                    m_progressBar.Value = e.ProgressPercentage;
                    m_progressBar.Value = e.ProgressPercentage == m_progressBar.Minimum ||
                                          e.ProgressPercentage == m_progressBar.Maximum
                        ? e.ProgressPercentage
                        : e.ProgressPercentage - 1;
                }
                else
                {
                    m_progressLabel.Text = $"Downloading update ({e.BytesReceived:N0} bytes received)...";
                    m_progressBar.Style = ProgressBarStyle.Marquee;
                }
            }

            /// <summary>
            /// Called when download is completed.
            /// </summary>
            /// <param name="sender">The sender.</param>
            /// <param name="e">The <see cref="AsyncCompletedEventArgs"/> instance containing the event data.</param>
            private void DownloadCompleted(object? sender, AsyncCompletedEventArgs e)
            {
                if (InvokeRequired)
                {
                    Dispatcher.Invoke(() => DownloadCompleted(sender, e));
                    return;
                }

                if (e.Error != null)
                    ExceptionHandler.LogException(e.Error, true);

                if (e.Cancelled)
                {
                    if (File.Exists(m_tempFilename))
                        FileHelper.DeleteFile(m_tempFilename);
                }

                // Set the download state in the tag for conditional check
                Tag = m_datafile.IsDownloaded = !e.Cancelled && (e.Error == null);
            }
        }

        #endregion
    }
}
