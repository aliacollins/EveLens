namespace EveLens.XmlGenerator.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    

    public partial class crtCertificates
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int certificateID { get; set; }

        public short? groupID { get; set; }

        public int? classID { get; set; }

        public byte? grade { get; set; }

        public int? corpID { get; set; }

        public int? iconID { get; set; }

        [StringLength(500)]
        public string description { get; set; }
    }
}
