// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Extensions;
using System;
using System.Runtime.Serialization;

namespace EveLens.Common.Serialization.Esi
{
    /// <summary>
    /// Jump fatigue now has its own API call!
    /// </summary>
    [DataContract]
    public sealed class EsiAPIJumpFatigue
    {
        private DateTime fatigueExpires;
        private DateTime lastJump;
        private DateTime lastUpdate;

        public EsiAPIJumpFatigue()
        {
            fatigueExpires = DateTime.MinValue;
            lastJump = DateTime.MinValue;
            lastUpdate = DateTime.MinValue;
        }

        [DataMember(Name = "last_jump_date", EmitDefaultValue = false, IsRequired = false)]
        private string LastJumpJson
        {
            get
            {
                return lastJump.DateTimeToTimeString();
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                    lastJump = value.TimeStringToDateTime();
            }
        }

        [DataMember(Name = "jump_fatigue_expire_date", EmitDefaultValue = false, IsRequired = false)]
        private string FatigueExpiresJson
        {
            get
            {
                return fatigueExpires.DateTimeToTimeString();
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                    fatigueExpires = value.TimeStringToDateTime();
            }
        }

        [DataMember(Name = "last_update_date", EmitDefaultValue = false, IsRequired = false)]
        private string LastUpdateJson
        {
            get
            {
                return lastUpdate.DateTimeToTimeString();
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                    lastUpdate = value.TimeStringToDateTime();
            }
        }

        public DateTime LastJump
        {
            get
            {
                return lastJump;
            }
        }

        public DateTime FatigueExpires
        {
            get
            {
                return fatigueExpires;
            }
        }

        public DateTime LastUpdate
        {
            get
            {
                return lastUpdate;
            }
        }
    }
}
