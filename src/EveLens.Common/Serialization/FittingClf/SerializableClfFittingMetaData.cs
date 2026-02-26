// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Runtime.Serialization;
using EveLens.Common.Extensions;

namespace EveLens.Common.Serialization.FittingClf
{
    [DataContract]
    public sealed class SerializableClfFittingMetaData
    {
        [DataMember(Name = "title")]
        public string? Title { get; set; }

        [DataMember(Name = "description")]
        public string? Description { get; set; }

        [DataMember(Name = "creationdate")]
        public string CreationDateJson
        {
            get { return CreationDate.DateTimeToTimeString("ddd, dd MMM yyyy HH:mm:ss +0000"); }
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                    CreationDate = value.TimeStringToDateTime();
            }
        }

        public DateTime CreationDate { get; set; }
    }
}