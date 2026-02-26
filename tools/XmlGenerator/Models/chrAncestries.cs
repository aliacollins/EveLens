namespace EveLens.XmlGenerator.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    

    public partial class chrAncestries
    {
        [Key]
        public byte ancestryID { get; set; }

        [StringLength(100)]
        public string ancestryName { get; set; }

        public byte? bloodlineID { get; set; }

        [StringLength(1000)]
        public string description { get; set; }

        public byte? perception { get; set; }

        public byte? willpower { get; set; }

        public byte? charisma { get; set; }

        public byte? memory { get; set; }

        public byte? intelligence { get; set; }

        public int? iconID { get; set; }

        [StringLength(500)]
        public string shortDescription { get; set; }
    }
}
