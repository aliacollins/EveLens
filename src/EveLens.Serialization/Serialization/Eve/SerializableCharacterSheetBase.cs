// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using EveLens.Common.Extensions;

namespace EveLens.Common.Serialization.Eve
{
    /// <summary>
    /// Represents a serializable version of a character sheet. Used for settings.xml serialization and CCP querying
    /// </summary>
    public class SerializableCharacterSheetBase : ISerializableCharacterIdentity
    {
        protected SerializableCharacterSheetBase()
        {
            Attributes = new SerializableCharacterAttributes();
            Skills = new Collection<SerializableCharacterSkill>();
            Certificates = new Collection<SerializableCharacterCertificate>();
            EmploymentHistory = new Collection<SerializableEmploymentHistory>();
        }

        [XmlElement("characterID")]
        public long ID { get; set; }

        [XmlElement("name")]
        [JsonIgnore]
        public string? NameXml
        {
            get { return Name; }
            set { Name = value?.HtmlDecode() ?? string.Empty; }
        }

        [XmlElement("homeStationID")]
        public long HomeStationID { get; set; }

        [XmlElement("DoB")]
        [JsonIgnore]
        public string BirthdayXml
        {
            get { return Birthday.DateTimeToTimeString(); }
            set
            {
                if (!string.IsNullOrEmpty(value))
                    Birthday = value.TimeStringToDateTime();
            }
        }

        [XmlElement("race")]
        public string? Race { get; set; }

        [XmlElement("bloodLine")]
        public string? BloodLine { get; set; }

        [XmlElement("ancestry")]
        public string? Ancestry { get; set; }

        [XmlElement("gender")]
        public string? Gender { get; set; }

        [XmlElement("corporationName")]
        [JsonIgnore]
        public string? CorporationNameXml
        {
            get { return CorporationName; }
            set { CorporationName = value?.HtmlDecode() ?? string.Empty; }
        }

        [XmlElement("corporationID")]
        public long CorporationID { get; set; }

        [XmlElement("allianceName")]
        [JsonIgnore]
        public string? AllianceNameXml
        {
            get { return AllianceName; }
            set { AllianceName = value?.HtmlDecode() ?? string.Empty; }
        }

        [XmlElement("allianceID")]
        public long AllianceID { get; set; }

        [XmlElement("factionName")]
        public string? FactionName { get; set; }

        [XmlElement("factionID")]
        public int FactionID { get; set; }

        [XmlElement("freeSkillPoints")]
        public int FreeSkillPoints { get; set; }

        [XmlElement("freeRespecs")]
        public short FreeRespecs { get; set; }

        [XmlElement("cloneJumpDate")]
        [JsonIgnore]
        public string CloneJumpDateXml
        {
            get { return CloneJumpDate.DateTimeToTimeString(); }
            set
            {
                if (!string.IsNullOrEmpty(value))
                    CloneJumpDate = value.TimeStringToDateTime();
            }
        }

        [XmlElement("lastRespecDate")]
        [JsonIgnore]
        public string LastRespecDateXml
        {
            get { return LastRespecDate.DateTimeToTimeString(); }
            set
            {
                if (!string.IsNullOrEmpty(value))
                    LastRespecDate = value.TimeStringToDateTime();
            }
        }

        [XmlElement("lastTimedRespec")]
        [JsonIgnore]
        public string LastTimedRespecXml
        {
            get { return LastTimedRespec.DateTimeToTimeString(); }
            set
            {
                if (!string.IsNullOrEmpty(value))
                    LastTimedRespec = value.TimeStringToDateTime();
            }
        }

        [XmlElement("remoteStationDate")]
        [JsonIgnore]
        public string RemoteStationDateXml
        {
            get { return RemoteStationDate.DateTimeToTimeString(); }
            set
            {
                if (!string.IsNullOrEmpty(value))
                    RemoteStationDate = value.TimeStringToDateTime();
            }
        }

        [XmlElement("jumpActivation")]
        [JsonIgnore]
        public string JumpActivationDateXml
        {
            get { return JumpActivationDate.DateTimeToTimeString(); }
            set
            {
                if (!string.IsNullOrEmpty(value))
                    JumpActivationDate = value.TimeStringToDateTime();
            }
        }

        [XmlElement("jumpFatigue")]
        [JsonIgnore]
        public string JumpFatigueDateXml
        {
            get { return JumpFatigueDate.DateTimeToTimeString(); }
            set
            {
                if (!string.IsNullOrEmpty(value))
                    JumpFatigueDate = value.TimeStringToDateTime();
            }
        }

        [XmlElement("jumpLastUpdate")]
        [JsonIgnore]
        public string JumpLastUpdateDateXml
        {
            get { return JumpLastUpdateDate.DateTimeToTimeString(); }
            set
            {
                if (!string.IsNullOrEmpty(value))
                    JumpLastUpdateDate = value.TimeStringToDateTime();
            }
        }

		[XmlArray("certificates")]
		[XmlArrayItem("certificate")]
		public Collection<SerializableCharacterCertificate> Certificates { get; }

		[XmlElement("balance")]
        public decimal Balance { get; set; }

        [XmlElement("shipName")]
        public string? ShipName { get; set; }

        [XmlElement("shipTypeName")]
        public string? ShipTypeName { get; set; }

        [XmlElement("lastKnownLocation")]
        public SerializableLocation? LastKnownLocation { get; set; }

        [XmlElement("securityStatus")]
        public double SecurityStatus { get; set; }

        [XmlElement("cloneStateOverride")]
        public string? CloneState { get; set; }

        [XmlArray("employmentHistory")]
		[XmlArrayItem("record")]
		public Collection<SerializableEmploymentHistory> EmploymentHistory { get; }

		[XmlElement("attributes")]
        public SerializableCharacterAttributes Attributes { get; set; }

		[XmlArray("skills")]
		[XmlArrayItem("skill")]
		public Collection<SerializableCharacterSkill> Skills { get; }

		/// <summary>
		/// Gets or sets the name.
		/// </summary>
		[XmlIgnore]
        [JsonInclude]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the name of the corporation.
        /// </summary>
        [XmlIgnore]
        [JsonInclude]
        public string? CorporationName { get; set; }

        /// <summary>
        /// Gets or sets the name of the alliance.
        /// </summary>
        [XmlIgnore]
        [JsonInclude]
        public string? AllianceName { get; set; }

        /// <summary>
        /// The date and time the character was created.
        /// </summary>
        [XmlIgnore]
        [JsonInclude]
        public DateTime Birthday { get; set; }

        /// <summary>
        /// The date and time the jump clone was created.
        /// </summary>
        [XmlIgnore]
        [JsonInclude]
        public DateTime CloneJumpDate { get; set; }

        /// <summary>
        /// The date and time of the last remap.
        /// </summary>
        [XmlIgnore]
        [JsonInclude]
        public DateTime LastRespecDate { get; set; }

        /// <summary>
        /// The date and time of the last timed remap.
        /// </summary>
        [XmlIgnore]
        [JsonInclude]
        public DateTime LastTimedRespec { get; set; }

        /// <summary>
        /// The date and time of the last remap.
        /// </summary>
        [XmlIgnore]
        [JsonInclude]
        public DateTime RemoteStationDate { get; set; }

        /// <summary>
        /// Gets or sets the jump activation date.
        /// </summary>
        [XmlIgnore]
        [JsonInclude]
        public DateTime JumpActivationDate { get; set; }

        /// <summary>
        /// Gets or sets the jump fatigue date.
        /// </summary>
        [XmlIgnore]
        [JsonInclude]
        public DateTime JumpFatigueDate { get; set; }

        /// <summary>
        /// Gets or sets the jump last update date.
        /// </summary>
        [XmlIgnore]
        [JsonInclude]
        public DateTime JumpLastUpdateDate { get; set; }
    }
}
