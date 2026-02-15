using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using EVEMon.Common;
using EVEMon.Common.Controls;
using EVEMon.Common.Helpers;
using EVEMon.Common.Service;

namespace EVEMon.DiagnosticReport
{
    /// <summary>
    /// Displays a sanitized diagnostic report that users can review, edit, and share.
    /// </summary>
    public partial class DiagnosticReportWindow : EVEMonForm
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DiagnosticReportWindow"/> class.
        /// </summary>
        public DiagnosticReportWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Generates the diagnostic report on load.
        /// </summary>
        private void DiagnosticReportWindow_Load(object? sender, EventArgs e)
        {
            ReportTextBox.Text = DiagnosticReportBuilder.BuildDiagnosticReport();
        }

        /// <summary>
        /// Copies the report text to the clipboard.
        /// </summary>
        private void CopyToClipboardButton_Click(object? sender, EventArgs e)
        {
            try
            {
                Clipboard.SetText(ReportTextBox.Text, TextDataFormat.Text);
                MessageBox.Show("Report copied to clipboard.", "Copy",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (ExternalException)
            {
                MessageBox.Show("Failed to copy to clipboard. Another application may be " +
                    "using the clipboard.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Submits the diagnostic report via the webhook. Falls back to the manual
        /// save-to-file + GitHub URL flow if the webhook is unreachable.
        /// </summary>
        private async void OpenGitHubIssueButton_Click(object? sender, EventArgs e)
        {
            OpenGitHubIssueButton.Enabled = false;
            OpenGitHubIssueButton.Text = "Submitting...";
            try
            {
                string version = EveMonClient.FileVersionInfo?.FileVersion ?? "unknown";
                string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm");
                string? subject = SubjectTextBox.Text?.Trim();
                string title = string.IsNullOrEmpty(subject)
                    ? $"Diagnostic Report - v{version} - {timestamp}Z"
                    : $"Diagnostic: {subject} - v{version}";
                ReportSubmissionResult result = await ReportSubmissionService
                    .SubmitReportAsync(title, "diagnostic", ReportTextBox.Text);

                if (result.Success)
                {
                    MessageBox.Show($"Report submitted successfully.\n\n{result.IssueUrl}",
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

                DiagnosticReportBuilder.SaveReportToFile(ReportTextBox.Text);

                try
                {
                    Clipboard.SetText(ReportTextBox.Text, TextDataFormat.Text);
                }
                catch (ExternalException)
                {
                    // Continue even if clipboard fails
                }

                string fallbackTitle = string.IsNullOrEmpty(subject)
                    ? "Diagnostic Report"
                    : $"Diagnostic: {subject}";
                string url = DiagnosticReportBuilder.BuildGitHubIssueUrl(fallbackTitle);
                Util.OpenURL(new Uri(url));
            }
            finally
            {
                OpenGitHubIssueButton.Text = "Submit Report";
                OpenGitHubIssueButton.Enabled = true;
            }
        }

        /// <summary>
        /// Closes the window.
        /// </summary>
        private void CloseButton_Click(object? sender, EventArgs e)
        {
            Close();
        }
    }
}
