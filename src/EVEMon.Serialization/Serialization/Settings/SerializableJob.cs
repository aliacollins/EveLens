using System;
using System.Xml.Serialization;
using EVEMon.Common.Enumerations;

namespace EVEMon.Common.Serialization.Settings
{
    /// <summary>
    /// Represents an industry job.
    /// </summary>
    public class SerializableJob
    {
        /// <summary>
        /// Unique job ID for this job. Note that these are not guaranteed to be unique forever, they can recycle. 
        /// But they are unique for the purpose of one data pull. 
        /// </summary>
        [XmlAttribute("jobID")]
        public long JobID { get; set; }

        [XmlAttribute("jobState")]
        public JobState State { get; set; }

        [XmlAttribute("startDate")]
        public DateTime StartDate { get; set; }

        [XmlAttribute("endDate")]
        public DateTime EndDate { get; set; }

        [XmlAttribute("pauseDate")]
        public DateTime PauseDate { get; set; }

        /// <summary>
        /// Which this job was issued for.
        /// </summary>
        [XmlAttribute("issuedFor")]
        public IssuedFor IssuedFor { get; set; }

        /// <summary>
        /// The time this job state was last changed.
        /// </summary>
        [XmlAttribute("lastStateChange")]
        public DateTime LastStateChange { get; set; }
    }
}