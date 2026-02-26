namespace EveLens.XmlGenerator.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    

    public partial class dgmAttributeCategories
    {
        [Key]
        public byte categoryID { get; set; }

        [StringLength(50)]
        public string categoryName { get; set; }

        [StringLength(200)]
        public string categoryDescription { get; set; }
    }
}
