namespace EveLens.XmlGenerator.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    

    public partial class eveIcons
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int iconID { get; set; }

        [Required]
        [StringLength(500)]
        public string iconFile { get; set; }

        [Required]
        public string description { get; set; }
    }
}
