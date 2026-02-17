using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EVEMon.Common.Enumerations;
using EVEMon.Common.Extensions;
using EVEMon.Common.Helpers;
using EVEMon.Common.Models;
using EVEMon.Common.Services;
using EVEMon.Common.SettingsObjects;
using EVEMon.Core.Enumerations;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace EVEMon.Common.Service
{
    /// <summary>
    /// Provides SMTP based e-mail services tailored to Skill Completion.
    /// Uses MailKit for modern, cross-platform email support.
    /// </summary>
    public static class Emailer
    {
        private static bool s_isTestMail;

        /// <summary>
        /// Sends a test mail
        /// </summary>
        /// <param name="settings">NotificationSettings object</param>
        /// <remarks>
        /// A notification settings object is required, as this function
        /// is called from the Settings Window, and assumedly the user
        /// is changing settings.
        /// </remarks>
        /// <returns>False if an exception was thrown, otherwise True.</returns>
        public static void SendTestMail(NotificationSettings settings)
        {
            s_isTestMail = true;
            SendMail(settings, "EVEMon Test Mail", "This is a test email sent by EVEMon");
        }

        /// <summary>
        /// Sends a mail alert for a skill completion
        /// </summary>
        /// <param name="queueList">Current Skill Queue</param>
        /// <param name="skill">Skill that has just completed</param>
        /// <param name="character">Character affected</param>
        /// <exception cref="System.ArgumentNullException">
        /// </exception>
        public static void SendSkillCompletionMail(IList<QueuedSkill> queueList, QueuedSkill skill, Character character)
        {
            s_isTestMail = false;

            queueList.ThrowIfNull(nameof(queueList));

            skill.ThrowIfNull(nameof(skill));

            CCPCharacter ccpCharacter = character as CCPCharacter;

            // Current character isn't a CCP character, so can't have a Queue.
            if (ccpCharacter == null)
                return;

            string skillLevelText = $"{skill.SkillName} {Skill.GetRomanFromInt(skill.Level)}";
            string subjectText = $"{character.Name} has finished training {skillLevelText}.";

            // Message's first line
            StringBuilder body = new StringBuilder();
            body
                .AppendLine(subjectText)
                .AppendLine();

            // Next skills in queue
            if (queueList[0] != null)
            {
                string plural = queueList.Count > 1 ? "s" : string.Empty;
                body.AppendLine($"Next skill{plural} in queue:");

                foreach (QueuedSkill qskill in queueList)
                {
                    body.AppendLine($"- {qskill}");
                }
                body.AppendLine();
            }
            else
                body
                    .AppendLine("Character is not training.")
                    .AppendLine();

            // Skill queue less than a day
            if (ccpCharacter.SkillQueue.LessThanWarningThreshold)
            {
                TimeSpan skillQueueEndTime = ccpCharacter.SkillQueue.EndTime.Subtract(DateTime.UtcNow);
                TimeSpan timeLeft = SkillQueue.WarningThresholdTimeSpan.Subtract(skillQueueEndTime);

                // Skill queue empty?
                if (timeLeft > SkillQueue.WarningThresholdTimeSpan)
                    body.AppendLine("Skill queue is empty.");
                else
                {
                    string timeLeftText = skillQueueEndTime < TimeSpan.FromMinutes(1)
                        ? skillQueueEndTime.ToDescriptiveText(DescriptiveTextOptions.IncludeCommas)
                        : skillQueueEndTime.ToDescriptiveText(DescriptiveTextOptions.IncludeCommas, false);

                    body.AppendLine($"Queue ends in {timeLeftText}.");
                }
            }

            // Short format (also for SMS)
            if (Settings.Notifications.UseEmailShortFormat)
            {
                SendMail(Settings.Notifications,
                    $"[STC] {character.Name} :: {skillLevelText}",
                    body.ToString());

                return;
            }

            // Long format
            if (character.Plans.Count > 0)
            {
                body.AppendLine("Next skills listed in plans:")
                    .AppendLine();
            }

            foreach (Plan plan in character.Plans)
            {
                if (plan.Count <= 0)
                    continue;

                // Print plan name
                CharacterScratchpad scratchpad = new CharacterScratchpad(character);
                body.AppendLine($"{plan.Name}:");

                // Scroll through entries
                int i = 0;
                int minDays = 1;
                foreach (PlanEntry entry in plan)
                {
                    TimeSpan trainTime = scratchpad.GetTrainingTime(entry.Skill, entry.Level,
                        TrainingOrigin.FromPreviousLevelOrCurrent);

                    // Only print the first three skills, and the very long skills
                    // (first limit is one day, then we add skills duration)
                    if (++i > 3 && trainTime.Days <= minDays)
                        continue;

                    if (i > 3)
                    {
                        // Print long message once
                        if (minDays == 1)
                            body.AppendLine().Append($"Longer skills from {plan.Name}:").AppendLine();

                        minDays = trainTime.Days + minDays;
                    }
                    body.Append($"\t{entry}");

                    // Notes
                    if (!string.IsNullOrEmpty(entry.Notes))
                        body.Append($" ({entry.Notes})");

                    // Training time
                    body
                        .Append(trainTime.Days > 0 ? $" - {trainTime.Days}d, {trainTime}" : $" - {trainTime}")
                        .AppendLine();
                }
                body.AppendLine();
            }

            SendMail(Settings.Notifications, subjectText, body.ToString());
        }

        /// <summary>
        /// Performs the sending of the mail asynchronously using MailKit.
        /// </summary>
        /// <param name="settings">Settings object to use when sending</param>
        /// <param name="subject">Subject of the message</param>
        /// <param name="body">Body of the message</param>
        /// <remarks>
        /// NotificationSettings object is required to support
        /// alternative settings from Tools -> Options. Use
        /// Settings.Notifications unless using an alternative
        /// configuration.
        /// </remarks>
        private static void SendMail(NotificationSettings settings, string subject, string body)
        {
            // Fire and forget - the async method handles errors internally
            _ = SendMailAsync(settings, subject, body);
        }

        /// <summary>
        /// Internal async implementation for sending email using MailKit.
        /// </summary>
        private static async Task SendMailAsync(NotificationSettings settings, string subject, string body)
        {
            // Trace something to the logs so we can identify the time the message was sent
            AppServices.TraceService?.Trace($"(Subject - {subject}; Server - {settings.EmailSmtpServerAddress}:{settings.EmailPortNumber})");

            string sender = string.IsNullOrEmpty(settings.EmailFromAddress)
                ? "no-reply@evemon.net"
                : settings.EmailFromAddress;

            List<string> toAddresses = settings.EmailToAddress.Split(
                new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            try
            {
                // Build the message using MimeKit
                var message = new MimeMessage();
                message.From.Add(MailboxAddress.Parse(sender));
                foreach (var address in toAddresses)
                {
                    message.To.Add(MailboxAddress.Parse(address.Trim()));
                }
                message.Subject = subject;
                message.Body = new TextPart("plain") { Text = body };

                // Send using MailKit SmtpClient
                using (var client = new SmtpClient())
                {
                    // Set timeout
                    client.Timeout = (int)TimeSpan.FromSeconds(Settings.Updates.HttpTimeout).TotalMilliseconds;

                    // Determine SSL/TLS options
                    var secureSocketOptions = settings.EmailServerRequiresSsl
                        ? SecureSocketOptions.StartTls
                        : SecureSocketOptions.Auto;

                    // For servers with self-signed certificates, accept all certificates
                    // This matches the previous behavior of the deprecated SmtpClient
                    client.ServerCertificateValidationCallback = (s, certificate, chain, sslPolicyErrors) => true;

                    // Connect to the server
                    await client.ConnectAsync(
                        settings.EmailSmtpServerAddress,
                        settings.EmailPortNumber,
                        secureSocketOptions).ConfigureAwait(false);

                    // Authenticate if required
                    if (settings.EmailAuthenticationRequired)
                    {
                        await client.AuthenticateAsync(
                            settings.EmailAuthenticationUserName,
                            Util.Decrypt(settings.EmailAuthenticationPassword,
                                settings.EmailAuthenticationUserName)).ConfigureAwait(false);
                    }

                    // Send the message
                    await client.SendAsync(message).ConfigureAwait(false);

                    // Disconnect cleanly
                    await client.DisconnectAsync(true).ConfigureAwait(false);
                }

                // Success notification
                AppServices.TraceService?.Trace("Message sent.");
                if (s_isTestMail)
                {
                    ShowMessageBox(@"The message sent successfully. Please verify that the message was received.",
                        @"EVEMon Emailer Success", DialogIcon.Information);
                }
            }
            catch (Exception e)
            {
                AppServices.TraceService?.Trace("An error occurred sending email");
                ExceptionHandler.LogException(e, true);
                ShowMessageBox(e.Message, @"EVEMon Emailer Error", DialogIcon.Error);
            }
        }

        /// <summary>
        /// Shows a message box on the UI thread via the dispatcher.
        /// </summary>
        private static void ShowMessageBox(string message, string caption, DialogIcon icon)
        {
            AppServices.Dispatcher.Invoke(() =>
                AppServices.DialogService.ShowMessage(message, caption, DialogButtons.OK, icon));
        }
    }
}
