using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using EVEMon.Common;
using EVEMon.Common.Constants;
using EVEMon.Common.Controls;
using EVEMon.Common.Helpers;
using EVEMon.Common.Properties;
using EVEMon.Common.Service;
using EVEMon.Common.Services;

namespace EVEMon.ExceptionHandling
{
    /// <summary>
    /// Form to handle the display of the error report for easy bug reporting.
    /// </summary>
    public partial class UnhandledExceptionWindow : EVEMonForm
    {
        private readonly Exception m_exception = null!;


        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="UnhandledExceptionWindow"/> class.
        /// </summary>
        private UnhandledExceptionWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnhandledExceptionWindow"/> class.
        /// </summary>
        /// <param name="exception">The exception.</param>
        public UnhandledExceptionWindow(Exception exception)
            : this()
        {
            m_exception = exception;
        }

        #endregion


        #region Inherited Events

        /// <summary>
        /// Loads resources, generates the report
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UnhandledExceptionWindow_Load(object? sender, EventArgs e)
        {
            SetBugImage();

            string context = DetectCrashContext(m_exception);
            if (!string.IsNullOrEmpty(context))
                UserDescriptionTextBox.Text = context;

            BuildExceptionMessage();
        }

        #endregion


        #region Content Management

        /// <summary>
        /// Builds the exception message.
        /// </summary>
        private void BuildExceptionMessage()
        {
            EveMonClient.StopTraceLogging();
            try
            {
                TechnicalDetailsTextBox.Text =
                    DiagnosticReportBuilder.BuildCrashReport(m_exception);
            }
            catch (InvalidOperationException ex)
            {
                ExceptionHandler.LogException(ex, true);
                TechnicalDetailsTextBox.Text = Properties.Resources.ErrorBuildingError;
            }
        }

        /// <summary>
        /// Sets the bug image.
        /// </summary>
        private void SetBugImage()
        {
            try
            {
                Bitmap bug = Resources.Bug;

                int oHeight = bug.Height;
                int oWidth = bug.Width;
                if (bug.Height <= BugPictureBox.ClientSize.Height)
                    return;

                double scale = (double)BugPictureBox.ClientSize.Height / bug.Height;
                oHeight = (int)(oHeight * scale);
                oWidth = (int)(oWidth * scale);
                BugPictureBox.Image = new Bitmap(bug, new Size(oWidth, oHeight));
                BugPictureBox.ClientSize = new Size(oWidth, oHeight);
            }
            catch (Exception ex)
            {
                ExceptionHandler.LogRethrowException(ex);
                throw;
            }
        }

        /// <summary>
        /// Gets the recursive stack trace.
        /// Delegates to DiagnosticReportBuilder for the actual implementation.
        /// </summary>
        /// <value>The recursive stack trace.</value>
        internal static string GetRecursiveStackTrace(Exception exception)
            => DiagnosticReportBuilder.GetRecursiveStackTrace(exception);

        /// <summary>
        /// Attempts to detect what the user was doing based on the exception stack trace.
        /// Returns a human-readable context string, or null if unknown.
        /// </summary>
        private static string DetectCrashContext(Exception exception)
        {
            string trace = exception?.ToString() ?? string.Empty;

            if (trace.Contains("AssetBrowser") || trace.Contains("AssetsList"))
                return "Opening or browsing assets";
            if (trace.Contains("SkillBrowser") || trace.Contains("SkillTreeDisplay"))
                return "Browsing skills";
            if (trace.Contains("SkillQueue"))
                return "Viewing skill queue";
            if (trace.Contains("PlanWindow") || trace.Contains("PlanEditor"))
                return "Working with skill plans";
            if (trace.Contains("MarketOrder"))
                return "Viewing market orders";
            if (trace.Contains("ContractBrowser") || trace.Contains("Contract"))
                return "Viewing contracts";
            if (trace.Contains("IndustryJob"))
                return "Viewing industry jobs";
            if (trace.Contains("KillReport"))
                return "Viewing kill reports";
            if (trace.Contains("MailMessage") || trace.Contains("EveMailBody"))
                return "Reading EVE mail";
            if (trace.Contains("CharacterMonitor"))
                return "Viewing character monitor";
            if (trace.Contains("MainWindow"))
                return "Using main window";
            if (trace.Contains("Settings") || trace.Contains("SettingsForm"))
                return "Changing settings";
            if (trace.Contains("ESIKey") || trace.Contains("EsiKey"))
                return "Managing ESI keys";
            if (trace.Contains("NotificationList") || trace.Contains("EveNotification"))
                return "Viewing notifications";
            if (trace.Contains("Calendar"))
                return "Viewing calendar";

            return null!;
        }

        #endregion


        #region Local Events

        /// <summary>
        /// Handles the Click event of the CloseButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void CloseButton_Click(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        /// <summary>
        /// Handles the Click event of the CopyDetailsButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void CopyDetailsButton_Click(object? sender, EventArgs e)
        {
            try
            {
                Clipboard.SetText(TechnicalDetailsTextBox.Text, TextDataFormat.Text);
                MessageBox.Show(Properties.Resources.MessageCopiedDetails, "Copy",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (ExternalException ex)
            {
                // Occurs when another process is using the clipboard
                ExceptionHandler.LogException(ex, true);
                MessageBox.Show(Properties.Resources.ErrorClipboardFailure, "Error copying",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handles the Click event of the DataDirectoryButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void DataDirectoryButton_Click(object? sender, EventArgs e)
        {
            Util.OpenURL(new Uri(AppServices.ApplicationPaths.DataDirectory));
        }

        /// <summary>
        /// Handles the Click event of the SubmitReportButton control.
        /// Submits the crash report via the webhook, falling back to the manual flow.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private async void SubmitReportButton_Click(object? sender, EventArgs e)
        {
            SubmitReportButton.Enabled = false;
            try
            {
                string type = m_exception.GetType().Name;
                string crashSummary = $"{type}: {m_exception.Message}";

                string version = EveMonClient.FileVersionInfo?.FileVersion ?? "unknown";
                string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm");
                string? userDesc = UserDescriptionTextBox.Text?.Trim();
                string title = string.IsNullOrEmpty(userDesc)
                    ? $"Crash: {type} - v{version} - {timestamp}Z"
                    : $"Crash: {userDesc} ({type}) - v{version}";
                ReportSubmissionResult result = await ReportSubmissionService
                    .SubmitReportAsync(title, "crash", TechnicalDetailsTextBox.Text, crashSummary);

                if (result.Success)
                {
                    MessageBox.Show($"Crash report submitted successfully.\n\n{result.IssueUrl}",
                        "Report Submitted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Util.OpenURL(new Uri(result.IssueUrl!));
                    return;
                }

                // Fallback: notify user, save to file, clipboard, manual GitHub URL
                MessageBox.Show(
                    $"Auto-submit failed: {result.ErrorMessage}\n\n" +
                    "The report has been saved and copied to your clipboard. " +
                    "A GitHub issue form will open for manual submission.",
                    "Auto-Submit Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                DiagnosticReportBuilder.SaveReportToFile(TechnicalDetailsTextBox.Text);

                try
                {
                    Clipboard.SetText(TechnicalDetailsTextBox.Text, TextDataFormat.Text);
                }
                catch (ExternalException)
                {
                    // Continue even if clipboard fails
                }

                string fallbackTitle = string.IsNullOrEmpty(userDesc)
                    ? $"Crash: {type}"
                    : $"Crash: {userDesc} ({type})";
                string url = DiagnosticReportBuilder.BuildGitHubIssueUrl(
                    fallbackTitle, crashSummary);
                Util.OpenURL(new Uri(url));
            }
            finally
            {
                SubmitReportButton.Enabled = true;
            }
        }

        /// <summary>
        /// Handles the LinkClicked event of the LatestBinariesLinkLabel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="LinkLabelLinkClickedEventArgs"/> instance containing the event data.</param>
        private void LatestBinariesLinkLabel_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
        {
            Util.OpenURL(new Uri(NetworkConstants.GitHubDownloadsBase));
        }

        #endregion
    }
}
