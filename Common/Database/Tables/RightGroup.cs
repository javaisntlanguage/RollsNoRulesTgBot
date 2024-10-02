using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Tables
{
	public class RightGroup
	{
		public Guid Id { get; set; }
		[Required]
		[MaxLength(64)]
		public string Name { get; set; }
		[Required]
		[MaxLength(255)]
		public string Description { get; set; }

		public List<RightsInGroup>? RightInGroups { get; set; }
		public List<AdminsInGroup>? AdminsInGroup { get; set; }
	}
}
