using EVEMon.Common;
using EVEMon.Common.Controls;
using EVEMon.Common.CustomEventArgs;
using EVEMon.Common.Enumerations;
using EVEMon.Common.Events;
using EVEMon.Common.Helpers;
using EVEMon.Common.Models;
using EVEMon.Common.Service;
using EVEMon.Common.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EVEMon.ApiCredentialsManagement
{
    /// <summary>
    /// A modal form that walks through SSO re-authentication for multiple
    /// ESI keys sequentially. Used after settings restore when all tokens
    /// are stale due to CCP's PKCE token rotation.
    /// </summary>
    public partial class BulkReauthenticationWindow : EVEMonForm
    {
        private readonly List<ESIKey> m_keysToAuth;
        private int m_currentIndex;
        private int m_authenticatedCount;
        private bool m_paused;
        private bool m_authInProgress;

        private SSOAuthenticationService? m_authService;
        private SSOWebServerHttpListener? m_server;
        private string m_state = string.Empty;
        private IDisposable? _subESIKeyInfoUpdated;

        /// <summary>
        /// Initializes a new instance of the <see cref="BulkReauthenticationWindow"/> class.
        /// Scans AppServices.ESIKeys for keys with HasError == true.
        /// </summary>
        public BulkReauthenticationWindow()
        {
            InitializeComponent();

            m_keysToAuth = new List<ESIKey>();
            m_currentIndex = -1;
            m_authenticatedCount = 0;
            m_paused = false;
            m_authInProgress = false;
        }

        /// <summary>
        /// Loads the key list and starts the auth flow.
        /// </summary>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (DesignMode)
                return;

            // Subscribe to ESI key info updates
            _subESIKeyInfoUpdated = AppServices.EventAggregator?.SubscribeOnUI<ESIKeyInfoUpdatedEvent>(
                this, OnESIKeyInfoUpdated);

            // Collect keys that need re-auth
            foreach (ESIKey key in AppServices.ESIKeys)
            {
                if (!key.HasError)
                    continue;

                m_keysToAuth.Add(key);

                string charName = GetCharacterName(key);
                var item = new ListViewItem(charName)
                {
                    Tag = key
                };
                item.SubItems.Add("Pending");
                lvCharacters.Items.Add(item);
            }

            if (m_keysToAuth.Count == 0)
            {
                lblProgress.Text = "All characters are already authenticated.";
                lblCurrentCharacter.Text = string.Empty;
                lblCurrentStatus.Text = string.Empty;
                return;
            }

            progressBar.Maximum = m_keysToAuth.Count;
            progressBar.Value = 0;
            UpdateProgressLabel();

            // Start the first auth
            AdvanceToNext();
        }

        /// <summary>
        /// Disposes event subscriptions on close.
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _subESIKeyInfoUpdated?.Dispose();
            StopCurrentServer();
            base.OnFormClosing(e);
        }

        /// <summary>
        /// Gets the character name associated with an ESI key.
        /// </summary>
        private static string GetCharacterName(ESIKey key)
        {
            var identity = key.CharacterIdentities.FirstOrDefault();
            return identity?.CharacterName ?? $"Key {key.ID}";
        }

        /// <summary>
        /// Updates the progress label text.
        /// </summary>
        private void UpdateProgressLabel()
        {
            lblProgress.Text = $"Progress: {m_authenticatedCount} of {m_keysToAuth.Count} characters authenticated";
        }

        /// <summary>
        /// Advances to the next key in the list and starts auth.
        /// </summary>
        private void AdvanceToNext()
        {
            m_currentIndex++;

            // Check if we're done
            if (m_currentIndex >= m_keysToAuth.Count)
            {
                OnAllComplete();
                return;
            }

            // Check if paused
            if (m_paused)
            {
                lblCurrentStatus.Text = "Paused. Click Resume to continue.";
                throbber.State = ThrobberState.Stopped;
                throbber.Visible = false;
                btnSkip.Enabled = false;
                return;
            }

            StartAuthForCurrentKey();
        }

        /// <summary>
        /// Starts the SSO auth flow for the current key.
        /// </summary>
        private void StartAuthForCurrentKey()
        {
            ESIKey key = m_keysToAuth[m_currentIndex];
            string charName = GetCharacterName(key);

            // Update UI
            lblCurrentCharacter.Text = $"Current: {charName}";
            lblCurrentStatus.Text = "Waiting for browser authorization...";
            throbber.State = ThrobberState.Rotating;
            throbber.Visible = true;
            btnSkip.Enabled = true;
            btnPause.Enabled = true;

            // Update ListView
            UpdateListViewItem(m_currentIndex, "Waiting for browser...");
            lvCharacters.Items[m_currentIndex].Selected = true;
            lvCharacters.EnsureVisible(m_currentIndex);

            // Create fresh auth service and server for this key
            m_authService = SSOAuthenticationService.GetInstance();
            if (m_authService == null)
            {
                UpdateListViewItem(m_currentIndex, "Failed - No SSO client ID");
                AdvanceToNext();
                return;
            }

            m_state = DateTime.UtcNow.ToFileTime().ToString();

            try
            {
                m_server = new SSOWebServerHttpListener();
                m_server.Start();
                m_authInProgress = true;
                m_server.BeginWaitForCode(m_state, OnCodeReceived);
                m_authService.SpawnBrowserForLogin(m_state, SSOWebServerHttpListener.PORT);
            }
            catch (IOException)
            {
                UpdateListViewItem(m_currentIndex, "Failed - Port in use");
                m_authInProgress = false;
                StopCurrentServer();
                AdvanceToNext();
            }
        }

        /// <summary>
        /// Called when the auth code is received from the browser callback.
        /// </summary>
        private void OnCodeReceived(Task<string> results)
        {
            try
            {
                if (!m_authInProgress)
                    return;

                if (results.IsFaulted)
                {
                    ExceptionHandler.LogException(results.Exception, true);
                    UpdateListViewItem(m_currentIndex, "Failed - Auth error");
                    m_authInProgress = false;
                    StopCurrentServer();
                    AdvanceToNext();
                    return;
                }

                string code;
                if (!results.IsCanceled && !string.IsNullOrEmpty(code = results.Result))
                {
                    UpdateListViewItem(m_currentIndex, "Verifying token...");
                    m_authService?.VerifyAuthCode(code, OnTokenReceived);
                }
                else
                {
                    UpdateListViewItem(m_currentIndex, "Failed - No code received");
                    m_authInProgress = false;
                    StopCurrentServer();
                    AdvanceToNext();
                }
            }
            catch (Exception ex)
            {
                ExceptionHandler.LogException(ex, true);
                m_authInProgress = false;
                StopCurrentServer();
                AdvanceToNext();
            }
        }

        /// <summary>
        /// Called when the access token is received from CCP.
        /// </summary>
        private void OnTokenReceived(AccessResponse response)
        {
            try
            {
                if (!m_authInProgress)
                    return;

                bool failed = string.IsNullOrEmpty(response?.AccessToken) ||
                              string.IsNullOrEmpty(response?.RefreshToken);

                if (failed)
                {
                    UpdateListViewItem(m_currentIndex, "Failed - Invalid token");
                    m_authInProgress = false;
                    StopCurrentServer();
                    AdvanceToNext();
                    return;
                }

                // Use the existing key's TryUpdateAsync to update with new tokens
                ESIKey key = m_keysToAuth[m_currentIndex];
                key.TryUpdateAsync(response!, OnKeyUpdated);
            }
            catch (Exception ex)
            {
                ExceptionHandler.LogException(ex, true);
                m_authInProgress = false;
                StopCurrentServer();
                AdvanceToNext();
            }
        }

        /// <summary>
        /// Called when the key has been updated with new token info.
        /// </summary>
        private void OnKeyUpdated(object? sender, ESIKeyCreationEventArgs e)
        {
            try
            {
                m_authInProgress = false;
                StopCurrentServer();

                if (e.CCPError != null)
                {
                    UpdateListViewItem(m_currentIndex, $"Failed - {e.CCPError.ErrorMessage}");
                }
                else
                {
                    // Import/update the key
                    e.CreateOrUpdate();

                    m_authenticatedCount++;
                    progressBar.Value = m_authenticatedCount;
                    UpdateProgressLabel();
                    UpdateListViewItem(m_currentIndex, "Authenticated");
                    lvCharacters.Items[m_currentIndex].ForeColor = Color.Green;
                }

                AdvanceToNext();
            }
            catch (Exception ex)
            {
                ExceptionHandler.LogException(ex, true);
                UpdateListViewItem(m_currentIndex, "Failed - Unexpected error");
                StopCurrentServer();
                AdvanceToNext();
            }
        }

        /// <summary>
        /// Handles ESIKeyInfoUpdated events to refresh ListView status.
        /// </summary>
        private void OnESIKeyInfoUpdated(ESIKeyInfoUpdatedEvent e)
        {
            // Refresh any items whose key status may have changed
            for (int i = 0; i < m_keysToAuth.Count; i++)
            {
                ESIKey key = m_keysToAuth[i];
                if (!key.HasError && lvCharacters.Items[i].SubItems[1].Text != "Authenticated")
                {
                    lvCharacters.Items[i].SubItems[1].Text = "Authenticated";
                    lvCharacters.Items[i].ForeColor = Color.Green;
                }
            }
        }

        /// <summary>
        /// Called when all keys have been processed.
        /// </summary>
        private void OnAllComplete()
        {
            lblCurrentCharacter.Text = string.Empty;
            lblCurrentStatus.Text = m_authenticatedCount == m_keysToAuth.Count
                ? "All characters authenticated successfully."
                : $"Complete. {m_authenticatedCount} of {m_keysToAuth.Count} authenticated.";
            throbber.State = ThrobberState.Stopped;
            throbber.Visible = false;
            btnSkip.Enabled = false;
            btnPause.Enabled = false;
            btnClose.Text = "C&lose";
        }

        /// <summary>
        /// Stops the current SSO web server if running.
        /// </summary>
        private void StopCurrentServer()
        {
            try
            {
                m_server?.Stop();
                m_server?.Dispose();
            }
            catch (Exception ex)
            {
                ExceptionHandler.LogException(ex, true);
            }
            m_server = null;
        }

        /// <summary>
        /// Updates a ListView item's status column.
        /// </summary>
        private void UpdateListViewItem(int index, string status)
        {
            if (index >= 0 && index < lvCharacters.Items.Count)
                lvCharacters.Items[index].SubItems[1].Text = status;
        }

        #region Button Handlers

        /// <summary>
        /// Skips the current character and advances to the next.
        /// </summary>
        private void btnSkip_Click(object? sender, EventArgs e)
        {
            try
            {
                m_authInProgress = false;
                StopCurrentServer();
                UpdateListViewItem(m_currentIndex, "Skipped");
                lvCharacters.Items[m_currentIndex].ForeColor = Color.Gray;
                AdvanceToNext();
            }
            catch (Exception ex)
            {
                ExceptionHandler.LogException(ex, true);
            }
        }

        /// <summary>
        /// Pauses/resumes the auth flow.
        /// </summary>
        private void btnPause_Click(object? sender, EventArgs e)
        {
            try
            {
                if (m_paused)
                {
                    // Resume
                    m_paused = false;
                    btnPause.Text = "&Pause";
                    // If we're sitting at a key that hasn't started, start it
                    if (!m_authInProgress && m_currentIndex < m_keysToAuth.Count)
                        StartAuthForCurrentKey();
                }
                else
                {
                    // Pause - will take effect after current auth completes or on skip
                    m_paused = true;
                    btnPause.Text = "&Resume";
                    lblCurrentStatus.Text = "Will pause after current character completes...";
                }
            }
            catch (Exception ex)
            {
                ExceptionHandler.LogException(ex, true);
            }
        }

        /// <summary>
        /// Closes the window. Shows confirmation if auth is still in progress.
        /// </summary>
        private void btnClose_Click(object? sender, EventArgs e)
        {
            try
            {
                int remaining = m_keysToAuth.Count - m_authenticatedCount;
                bool hasRemaining = m_currentIndex < m_keysToAuth.Count && remaining > 0;

                if (hasRemaining)
                {
                    var result = MessageBox.Show(
                        $"{m_authenticatedCount} of {m_keysToAuth.Count} characters authenticated.\n\n" +
                        "Already-authenticated characters will keep working.\n" +
                        "Remaining characters can be re-authenticated later from\n" +
                        "File > Re-authenticate All Characters.\n\n" +
                        "Close now?",
                        "Close Re-authentication",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result != DialogResult.Yes)
                        return;
                }

                m_authInProgress = false;
                StopCurrentServer();
                Close();
            }
            catch (Exception ex)
            {
                ExceptionHandler.LogException(ex, true);
            }
        }

        #endregion
    }
}
