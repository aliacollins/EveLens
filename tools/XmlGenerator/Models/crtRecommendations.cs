namespace EveLens.XmlGenerator.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    

    public partial class crtRecommendations
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int recommendationID { get; set; }

        public int? shipTypeID { get; set; }

        public int? certificateID { get; set; }

        public byte recommendationLevel { get; set; }
    }
}
