namespace EveLens.XmlGenerator.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    

    public partial class eveUnits
    {
        [Key]
        public byte unitID { get; set; }

        [StringLength(100)]
        public string unitName { get; set; }

        [StringLength(50)]
        public string displayName { get; set; }

        [StringLength(1000)]
        public string description { get; set; }
    }
}
