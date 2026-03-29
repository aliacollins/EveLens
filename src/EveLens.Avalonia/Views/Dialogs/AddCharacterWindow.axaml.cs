// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using EveLens.Common.CustomEventArgs;
using EveLens.Common.Models;
using EveLens.Common.Service;

using EveLens.Common.Service;
using EveLens.Avalonia.Services;
namespace EveLens.Avalonia.Views.Dialogs
{
    public partial class AddCharacterWindow : Window
    {
        private SSOWebServerHttpListener? _server;
        private SSOAuthenticationService? _authService;
        private string _state = string.Empty;
        private ESIKeyCreationEventArgs? _creationArgs;
        private readonly List<string> _importedNames = new();

        /// <summary>
        /// True if at least one character was imported during this dialog session.
        /// </summary>
        public bool CharacterImported => _importedNames.Count > 0;

        /// <summary>
        /// Names of all characters imported during this dialog session.
        /// </summary>
        public IReadOnlyList<string> ImportedCharacterNames => _importedNames;

        public AddCharacterWindow()
        {
            InitializeComponent();

            OpenLoginBtn.Click += OnOpenLoginClick;
            AddAnotherBtn.Click += OnAddAnotherClick;
            DoneBtn.Click += OnDoneClick;
            TryAgainBtn.Click += OnTryAgainClick;

            CancelReadyBtn.Click += OnCancelClick;
            CancelWaitingBtn.Click += OnCancelClick;
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
            await StartSSOLogin();
        }

        private async System.Threading.Tasks.Task StartSSOLogin()
        {
            try
            {
                CleanupServer();
                _creationArgs = null;

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

                // Auto-import: create the ESI key immediately
                _creationArgs.CreateOrUpdate();
                string name = identity.CharacterName ?? "Unknown";

                // Show previous imports if any
                if (_importedNames.Count > 0)
                {
                    PreviousAddsDivider.IsVisible = true;
                    PreviousAddsLabel.IsVisible = true;
                    RebuildPreviousAddsList();
                }

                // Add to session list
                _importedNames.Add(name);

                CharacterNameText.Text = name;
                ShowPanel("success");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error processing character info: {ex}");
                ShowError($"Error: {ex.Message}");
            }
        }

        private void RebuildPreviousAddsList()
        {
            PreviousAddsList.Children.Clear();
            foreach (var name in _importedNames)
            {
                PreviousAddsList.Children.Add(new TextBlock
                {
                    Text = $"\u2713 {name}",
                    FontSize = FontScaleService.Body,
                    Foreground = (IBrush?)this.FindResource("EveTextSecondaryBrush") ?? Brushes.Gray
                });
            }
        }

        private async void OnAddAnotherClick(object? sender, RoutedEventArgs e)
        {
            // Go directly to browser login, skip the landing page
            await StartSSOLogin();
        }

        private void OnDoneClick(object? sender, RoutedEventArgs e)
        {
            CleanupServer();
            Close();
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
