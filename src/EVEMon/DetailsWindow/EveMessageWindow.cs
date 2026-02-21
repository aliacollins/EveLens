// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Windows.Forms;
using EVEMon.Common;
using EVEMon.Common.Controls;
using EVEMon.Common.CustomEventArgs;
using EVEMon.Common.Enumerations;
using EVEMon.Common.Events;
using EVEMon.Common.Extensions;
using EVEMon.Common.Interfaces;
using EVEMon.Common.Models;
using EVEMon.Common.Notifications;
using EVEMon.Common.Serialization.Eve;
using EVEMon.Common.Services;

namespace EVEMon.DetailsWindow
{
    public sealed partial class EveMessageWindow : EVEMonForm
    {
        private readonly Timer m_timer = new Timer();

        private IDisposable? _subCharacterEVEMailBodyDownloaded;
        private IDisposable? _subNotificationSent;
        private IDisposable? _subEveIDToNameUpdated;

        /// <summary>
        /// Initializes a new instance of the <see cref="EveMessageWindow"/> class.
        /// </summary>
        private EveMessageWindow()
        {
            InitializeComponent();

            RememberPositionKey = "EVEMessageWindow";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EveMessageWindow"/> class.
        /// Constructor used in WindowsFactory.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <exception cref="System.ArgumentNullException">message</exception>
        public EveMessageWindow(IEveMessage message)
            : this()
        {
            message.ThrowIfNull(nameof(message));

            _subCharacterEVEMailBodyDownloaded = AppServices.EventAggregator.SubscribeOnUI<CharacterEVEMailBodyDownloadedEvent>(this, OnCharacterEVEMailBodyDownloaded);
            _subNotificationSent = AppServices.EventAggregator.SubscribeOnUI<NotificationSentEvent>(this, OnNotificationSent);
            _subEveIDToNameUpdated = AppServices.EventAggregator.SubscribeOnUI<EveIDToNameUpdatedEvent>(this, OnEveIDToNameUpdated);
            Disposed += OnDisposed;

            Tag = message;
            Text = $"{message.Title} - EVE Message";
            readingPane.SelectedObject = message;
        }

        /// <summary>
        /// On load.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            var notification = Tag as EveNotification;
            if (notification != null)
            {
                // Notification text is available now
                throbber.State = ThrobberState.Stopped;
                throbber.Visible = false;
                readingPane.SelectedObject = notification;
            }
            else
            {
                throbber.State = ThrobberState.Rotating;

                // Configure the timer to close the form on queries timeout
                m_timer.Start();
                m_timer.Interval = (int)TimeSpan.FromSeconds(Settings.Updates.HttpTimeout).TotalMilliseconds;

                m_timer.Tick += timer_Tick;
            }
        }

        /// <summary>
        /// Called when [disposed].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void OnDisposed(object? sender, EventArgs e)
        {
            _subCharacterEVEMailBodyDownloaded?.Dispose();
            _subNotificationSent?.Dispose();
            _subEveIDToNameUpdated?.Dispose();
            Disposed -= OnDisposed;
        }

        /// <summary>
        /// Handles the Tick event of the timer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void timer_Tick(object? sender, EventArgs e)
        {
            m_timer.Stop();

            // Close the form when there is nothing to show after query timeout
            if (!readingPane.Visible)
                Close();
        }

        /// <summary>
        /// Handles the CharacterEVEMailBodyDownloaded event of the EveMonClient control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EVEMon.Common.CustomEventArgs.CharacterChangedEventArgs"/> instance containing the event data.</param>
        private void OnCharacterEVEMailBodyDownloaded(CharacterEVEMailBodyDownloadedEvent e)
        {
            throbber.State = ThrobberState.Stopped;
            throbber.Visible = false;
            readingPane.SelectedObject = (Tag as EveMailMessage)!;
        }

        /// <summary>
        /// Handles the NotificationSent event.
        /// </summary>
        private void OnNotificationSent(NotificationSentEvent e)
        {
            APIErrorNotificationEventArgs? notification = e.Args as APIErrorNotificationEventArgs;
            if (notification != null)
            {
                var eveMailBodiesResult = notification.Result as CCPAPIResult<
                    SerializableAPIMailBodies>;
                if (eveMailBodiesResult != null && notification.Result.HasError)
                    // In case there was an error, close the window
                    Close();
            }
        }

        /// <summary>
        /// Handles the EveIDToNameUpdated event.
        /// </summary>
        private void OnEveIDToNameUpdated(EveIDToNameUpdatedEvent e)
        {
            if (Visible)
                readingPane.UpdatePane();
        }
    }
}
