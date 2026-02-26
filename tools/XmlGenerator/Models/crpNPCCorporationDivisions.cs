namespace EveLens.XmlGenerator.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    

    public partial class crpNPCCorporationDivisions
    {
        [Key]
        [Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int corporationID { get; set; }

        [Key]
        [Column(Order = 1)]
        public byte divisionID { get; set; }

        public byte? size { get; set; }
    }
}
