using System;
using Avalonia.Media;
using EVEMon.Common.Models;

namespace EVEMon.Avalonia.ViewModels
{
    /// <summary>
    /// Avalonia display wrapper for ActivityEntry with IBrush color properties.
    /// Contains zero business logic per Law 16.
    /// </summary>
    public sealed class ActivityDisplayEntry
    {
        private static readonly IBrush UnreadBrush = new SolidColorBrush(Color.Parse("#FFF0F0F0"));
        private static readonly IBrush ReadBrush = new SolidColorBrush(Color.Parse("#FFAAAAAA"));

        public ActivityEntry Data { get; }

        public string Description => Data.Description;
        public string CharacterName => Data.CharacterName;
        public bool HasCharacter => !string.IsNullOrEmpty(Data.CharacterName);
        public string CategoryDisplay => FormatCategory(Data.Category);
        public string TimeAgo => FormatTimeAgo(Data.Timestamp);
        public IBrush DescriptionBrush => Data.IsRead ? ReadBrush : UnreadBrush;

        public ActivityDisplayEntry(ActivityEntry data)
        {
            Data = data;
        }

        private static string FormatCategory(string cat) => cat switch
        {
            "SkillCompletion" => "Skill",
            "MarketOrdersEnding" => "Market",
            "ContractsEnded" => "Contract",
            "ContractsAssigned" => "Contract",
            "IndustryJobsCompletion" => "Industry",
            "PlanetaryPinsCompleted" => "Planetary",
            "NewEveMailMessage" => "Mail",
            "NewEveNotification" => "Notification",
            "ServerStatusChange" => "Server",
            "SkillQueueRoomAvailable" => "Queue",
            "QueryingError" => "Error",
            _ => cat
        };

        private static string FormatTimeAgo(DateTime utc)
        {
            var ago = DateTime.UtcNow - utc;
            if (ago.TotalSeconds < 60) return "just now";
            if (ago.TotalMinutes < 60) return $"{(int)ago.TotalMinutes}m ago";
            if (ago.TotalHours < 24) return $"{(int)ago.TotalHours}h ago";
            return $"{(int)ago.TotalDays}d ago";
        }
    }
}
