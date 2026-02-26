namespace EveLens.XmlGenerator.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    

    public partial class translationTables
    {
        [Key]
        [Column(Order = 0)]
        [StringLength(200)]
        public string sourceTable { get; set; }

        [StringLength(200)]
        public string destinationTable { get; set; }

        [Key]
        [Column(Order = 1)]
        [StringLength(200)]
        public string translatedKey { get; set; }

        public int? tcGroupID { get; set; }

        public int? tcID { get; set; }
    }
}
