namespace EveLens.XmlGenerator.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    

    public partial class chrRaces
    {
        [Key]
        public byte raceID { get; set; }

        [StringLength(100)]
        public string raceName { get; set; }

        [StringLength(1000)]
        public string description { get; set; }

        public int? iconID { get; set; }

        [StringLength(500)]
        public string shortDescription { get; set; }
    }
}
