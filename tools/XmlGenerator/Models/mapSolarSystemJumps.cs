namespace EveLens.XmlGenerator.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    

    public partial class mapSolarSystemJumps
    {
        public int? fromRegionID { get; set; }

        public int? fromConstellationID { get; set; }

        [Key]
        [Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int fromSolarSystemID { get; set; }

        [Key]
        [Column(Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int toSolarSystemID { get; set; }

        public int? toConstellationID { get; set; }

        public int? toRegionID { get; set; }
    }
}
