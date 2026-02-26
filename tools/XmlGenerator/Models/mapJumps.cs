namespace EveLens.XmlGenerator.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    

    public partial class mapJumps
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int stargateID { get; set; }

        public int? celestialID { get; set; }
    }
}
