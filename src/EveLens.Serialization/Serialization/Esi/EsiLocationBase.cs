// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Enumerations.CCPAPI;
using System;
using System.Runtime.Serialization;

namespace EveLens.Common.Serialization.Esi
{
    /// <summary>
    /// Base for classes which have a location and location type. Can be instantiated on its
    /// own or inherited.
    /// </summary>
    [DataContract]
    public class EsiLocationBase
    {
        private CCPAPILocationType locationType;

        public EsiLocationBase()
        {
            locationType = CCPAPILocationType.Other;
        }

        [DataMember(Name = "location_id")]
        public long LocationID { get; set; }

        // One of "station", "solar_system", "other"
        [DataMember(Name = "location_type")]
        private string LocationTypeJson
        {
            get
            {
                return locationType.ToString().ToLower();
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                    Enum.TryParse(value, true, out locationType);
            }
        }

        [IgnoreDataMember]
        public CCPAPILocationType LocationType
        {
            get
            {
                return locationType;
            }
        }
    }
}
