namespace EveLens.XmlGenerator.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    

    public partial class dgmTraits
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int traitID { get; set; }

        [Required]
        [StringLength(500)]
        public string bonusText { get; set; }

        public byte? unitID { get; set; }
    }
}
