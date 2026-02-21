// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Linq;
using System.Text.RegularExpressions;
using EVEMon.Common.Models;

namespace EVEMon.Avalonia.ViewModels
{
    /// <summary>
    /// Avalonia display wrapper for EveMailMessage used in the accordion mail view.
    /// Contains zero business logic per Law 16.
    /// </summary>
    public sealed class MailDisplayEntry
    {
        public EveMailMessage Mail { get; }

        public string SenderName => Mail.SenderName;
        public string Subject => Mail.Title;
        public string SentDateText => FormatDate(Mail.SentDate);
        public string RecipientText => FormatRecipients();
        public string BodyText => StripHtmlTags(Mail.Text);
        public bool HasBody => !string.IsNullOrWhiteSpace(Mail.Text);
        public bool IsNew { get; }
        public bool IsExpanded { get; set; }

        public MailDisplayEntry(EveMailMessage mail, bool isNew)
        {
            Mail = mail;
            IsNew = isNew;
        }

        private string FormatRecipients()
        {
            var chars = Mail.ToCharacters?.Where(c => !string.IsNullOrEmpty(c)).ToList();
            if (!string.IsNullOrEmpty(Mail.ToCorpOrAlliance))
                return $"To: {Mail.ToCorpOrAlliance}";
            if (chars != null && chars.Count > 0)
                return $"To: {string.Join(", ", chars)}";
            return string.Empty;
        }

        private static string FormatDate(DateTime date)
        {
            var now = DateTime.UtcNow;
            if (date.Date == now.Date)
                return date.ToString("HH:mm");
            if (date.Year == now.Year)
                return date.ToString("MMM dd HH:mm");
            return date.ToString("yyyy-MM-dd HH:mm");
        }

        internal static string StripHtmlTags(string html)
        {
            if (string.IsNullOrEmpty(html))
                return string.Empty;
            // Replace <br> variants with newlines
            var text = Regex.Replace(html, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
            // Strip remaining HTML tags
            text = Regex.Replace(text, @"<[^>]+>", string.Empty);
            // Decode common HTML entities
            text = text.Replace("&amp;", "&")
                       .Replace("&lt;", "<")
                       .Replace("&gt;", ">")
                       .Replace("&nbsp;", " ")
                       .Replace("&quot;", "\"");
            return text.Trim();
        }
    }
}
