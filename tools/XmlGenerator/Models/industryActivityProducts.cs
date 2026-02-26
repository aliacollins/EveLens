using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveLens.XmlGenerator.Models
{
	public partial class industryActivityProducts
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.None)]
		public int? typeID { get; set; }

		public int? activityID { get; set; }

		public int? productTypeID { get; set; }

		public int? quantity { get; set; }
	}
}


