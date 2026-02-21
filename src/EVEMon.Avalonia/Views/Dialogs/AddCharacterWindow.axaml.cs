using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using EVEMon.Common.CustomEventArgs;
using EVEMon.Common.Models;
using EVEMon.Common.Service;

namespace EVEMon.Avalonia.Views.Dialogs
{
    public partial class AddCharacterWindow : Window
    {
        private SSOWebServerHttpListener? _server;
        private SSOAuthenticationService? _authService;
        private string _state = string.Empty;
        private ESIKeyCreationEventArgs? _creationArgs;

        /// <summary>
        /// True if a character was successfully imported before the dialog closed.
        /// </summary>
        public bool CharacterImported { get; private set; }

        public AddCharacterWindow()
        {
            InitializeComponent();

            OpenLoginBtn.Click += OnOpenLoginClick;
            AddCharacterBtn.Click += OnAddCharacterClick;
            TryAgainBtn.Click += OnTryAgainClick;

            CancelReadyBtn.Click += OnCancelClick;
            CancelWaitingBtn.Click += OnCancelClick;
            CancelSuccessBtn.Click += OnCancelClick;
            CancelErrorBtn.Click += OnCancelClick;
        }

        private void ShowPanel(string panel)
        {
            ReadyPanel.IsVisible = panel == "ready";
            WaitingPanel.IsVisible = panel == "waiting";
            SuccessPanel.IsVisible = panel == "success";
            ErrorPanel.IsVisible = panel == "error";
        }

        private async void OnOpenLoginClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                _authService = SSOAuthenticationService.GetInstance();
                _state = DateTime.UtcNow.ToFileTime().ToString();
                _server = new SSOWebServerHttpListener();

                try
                {
                    _server.Start();
                }
                catch (IOException ex)
                {
                    ShowError($"Could not start authentication server on port {SSOWebServerHttpListener.PORT}.\n{ex.Message}");
                    return;
                }

                // Open browser for SSO login
                _authService.SpawnBrowserForLogin(_state, SSOWebServerHttpListener.PORT);
                ShowPanel("waiting");

                // Await the auth code from the local HTTP listener
                string code;
                try
                {
                    code = await _server.WaitForCodeAsync(_state);
                }
                catch (IOException)
                {
                    ShowError("Login was cancelled or the server encountered an error.");
                    return;
                }

                if (string.IsNullOrEmpty(code))
                {
                    ShowError("Login was cancelled or no authorization code was received.");
                    return;
                }

                // Exchange the code for tokens
                _authService.VerifyAuthCode(code, OnTokenReceived);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during SSO login: {ex}");
                ShowError($"An unexpected error occurred: {ex.Message}");
            }
        }

        private void OnTokenReceived(AccessResponse? response)
        {
            try
            {
                if (response == null ||
                    string.IsNullOrEmpty(response.AccessToken) ||
                    string.IsNullOrEmpty(response.RefreshToken))
                {
                    ShowError("Failed to receive a valid token from CCP. Please try again.");
                    return;
                }

                long newID = DateTime.UtcNow.ToFileTime();
                ESIKey.TryAddOrUpdateAsync(newID, response, OnCharacterInfoReceived);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error processing token: {ex}");
                ShowError($"Error processing token: {ex.Message}");
            }
        }

        private void OnCharacterInfoReceived(object? sender, ESIKeyCreationEventArgs e)
        {
            try
            {
                _creationArgs = e;

                if (e.CCPError != null)
                {
                    string message = e.CCPError.ErrorMessage ?? "Unknown error";
                    ShowError(message);
                    return;
                }

                var identity = e.Identity;
                if (identity == null)
                {
                    ShowError("Could not retrieve character information.");
                    return;
                }

                CharacterNameText.Text = identity.CharacterName;
                ShowPanel("success");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error processing character info: {ex}");
                ShowError($"Error: {ex.Message}");
            }
        }

        private void OnAddCharacterClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (_creationArgs == null) return;

                _creationArgs.CreateOrUpdate();
                CharacterImported = true;
                CleanupServer();
                Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error adding character: {ex}");
                ShowError($"Failed to add character: {ex.Message}");
            }
        }

        private void OnTryAgainClick(object? sender, RoutedEventArgs e)
        {
            CleanupServer();
            _creationArgs = null;
            ShowPanel("ready");
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            CleanupServer();
            Close();
        }

        private void ShowError(string message)
        {
            ErrorMessageText.Text = message;
            ShowPanel("error");
        }

        private void CleanupServer()
        {
            try
            {
                _server?.Stop();
                _server?.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error stopping server: {ex}");
            }
            _server = null;
        }

        protected override void OnClosed(EventArgs e)
        {
            CleanupServer();
            base.OnClosed(e);
        }
    }
}
