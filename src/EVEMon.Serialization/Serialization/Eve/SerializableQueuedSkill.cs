// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using EVEMon.Common.Extensions;

namespace EVEMon.Common.Serialization.Eve
{
    public sealed class SerializableQueuedSkill : ISynchronizableWithLocalClock
    {
        [XmlAttribute("typeID")]
        public int ID { get; set; }

        [XmlAttribute("level")]
        public int Level { get; set; }

        [XmlAttribute("startSP")]
        public int StartSP { get; set; }

        [XmlAttribute("endSP")]
        public int EndSP { get; set; }

        [XmlAttribute("startTime")]
        [JsonIgnore]
        public string? CCPStartTime { get; set; }

        [XmlAttribute("endTime")]
        [JsonIgnore]
        public string? CCPEndTime { get; set; }

        [XmlIgnore]
        [JsonInclude]
        public DateTime StartTime
        {
            get { return CCPStartTime?.TimeStringToDateTime() ?? DateTime.MinValue; }
            set { CCPStartTime = value.DateTimeToTimeString(); }
        }

        [XmlIgnore]
        [JsonInclude]
        public DateTime EndTime
        {
            get { return CCPEndTime?.TimeStringToDateTime() ?? DateTime.MinValue; }
            set { CCPEndTime = value.DateTimeToTimeString(); }
        }

        // When the skill queue is paused, startTime and endTime are empty in the XML document
        // As a result, the serialization leaves the DateTime with its default value
        [XmlIgnore]
        [JsonIgnore]
        public bool IsPaused
        {
            get { return EndTime == DateTime.MinValue; }
        }

        [XmlIgnore]
        [JsonIgnore]
        public bool IsCompleted
        {
            get { return !IsPaused && EndTime <= DateTime.UtcNow; }
        }

        [XmlIgnore]
        [JsonIgnore]
        public bool IsTraining
        {
            get { return !IsPaused && StartTime <= DateTime.UtcNow && DateTime.UtcNow <= EndTime; }
        }

        #region ISynchronizableWithLocalClock Members

        /// <summary>
        /// Synchronizes the stored times with local clock
        /// </summary>
        /// <param name="drift"></param>
        void ISynchronizableWithLocalClock.SynchronizeWithLocalClock(TimeSpan drift)
        {
            if (!string.IsNullOrEmpty(CCPStartTime))
                StartTime -= drift;

            if (!string.IsNullOrEmpty(CCPEndTime))
                EndTime -= drift;
        }

        #endregion
    }
}
