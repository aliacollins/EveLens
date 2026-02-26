namespace EveLens.XmlGenerator.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    

    public partial class ramActivities
    {
        [Key]
        public byte activityID { get; set; }

        [StringLength(100)]
        public string activityName { get; set; }

        [StringLength(5)]
        public string iconNo { get; set; }

        [StringLength(1000)]
        public string description { get; set; }

        public bool? published { get; set; }
    }
}
