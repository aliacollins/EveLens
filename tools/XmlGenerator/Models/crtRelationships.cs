namespace EveLens.XmlGenerator.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    

    public partial class crtRelationships
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int relationshipID { get; set; }

        public int? parentID { get; set; }

        public int? parentTypeID { get; set; }

        public byte? parentLevel { get; set; }

        public int? childID { get; set; }

        public byte? grade { get; set; }
    }
}
