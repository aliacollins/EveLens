using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Windows.Forms;
using EVEMon.Common.Controls;
using EVEMon.Common.Helpers;
using EVEMon.Common.Net;
using EVEMon.Common.Threading;

namespace EVEMon.Updater
{
    public partial class UpdateDownloadForm : EVEMonForm
    {
        private readonly Uri m_url = null!;
        private readonly string m_fileName = null!;
        private WebClient m_client = null!;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateDownloadForm"/> class.
        /// </summary>
        private UpdateDownloadForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateDownloadForm"/> class.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="fileName">The filename.</param>
        public UpdateDownloadForm(Uri url, string fileName)
            : this()
        {
            m_url = url;
            m_fileName = fileName;
        }

        /// <summary>
        /// Handles the Shown event of the UpdateDownloadForm control.
        /// </summary>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            string urlValidationError;
            if (!HttpWebClientService.IsValidURL(m_url, out urlValidationError))
                throw new ArgumentException(urlValidationError);

            try
            {
                using (m_client = HttpWebClientService.GetWebClient())
                {
                    m_client.DownloadFileCompleted += DownloadCompleted;
                    m_client.DownloadProgressChanged += ProgressChanged;

                    try
                    {
                        m_client.DownloadFileAsync(m_url, m_fileName);
                    }
                    catch (WebException ex)
                    {
                        throw HttpWebClientServiceException.HttpWebClientException(m_url, ex);
                    }
                    catch (Exception ex)
                    {
                        throw HttpWebClientServiceException.Exception(m_url, ex);
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
        /// Handles the FormClosing event of the UpdateDownloadForm control.
        /// </summary>
        /// <param name="e">The <see cref="FormClosingEventArgs"/> instance containing the event data.</param>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            if (!Visible || (e.CloseReason != CloseReason.ApplicationExitCall && e.CloseReason != CloseReason.TaskManagerClosing &&
                e.CloseReason != CloseReason.WindowsShutDown))
            {
                return;
            }

            m_client?.CancelAsync();
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
                ProgressLabel.Text = $"Downloading update ({e.ProgressPercentage}%, " +
                                     $"{e.BytesReceived:N0} of {e.TotalBytesToReceive:N0} bytes received)...";
                pbProgress.Style = ProgressBarStyle.Continuous;

                // Under Vista and Windows 7 there is a lag when progress bar updates too quick.
                // This hackish way though solves this issue (in a way) as explained in
                // http://stackoverflow.com/questions/977278/how-can-i-make-the-progress-bar-update-fast-enough/1214147#1214147.
                pbProgress.Value = e.ProgressPercentage;
                pbProgress.Value = e.ProgressPercentage == pbProgress.Minimum ||
                                   e.ProgressPercentage == pbProgress.Maximum
                    ? e.ProgressPercentage
                    : e.ProgressPercentage - 1;
            }
            else
            {
                ProgressLabel.Text = $"Downloading update ({e.BytesReceived:N0} bytes received)...";
                pbProgress.Style = ProgressBarStyle.Marquee;
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
            {
                ExceptionHandler.LogException(e.Error, true);
                DialogResult = DialogResult.Cancel;
            }
            else if (e.Cancelled)
            {
                DialogResult = DialogResult.Cancel;
                if (File.Exists(m_fileName))
                    FileHelper.DeleteFile(m_fileName);
            }
            else
                DialogResult = DialogResult.OK;

            Close();
        }

        /// <summary>
        /// Handles the Click event of the btCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void btCancel_Click(object? sender, EventArgs e)
        {
            m_client?.CancelAsync();
        }
    }
}