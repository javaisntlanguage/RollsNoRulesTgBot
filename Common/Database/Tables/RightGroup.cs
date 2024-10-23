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
		public const int NAME_MIN_LENGTH = 3;
		public const int NAME_MAX_LENGTH = 64;
		public const int DESCRIPTION_MAX_LENGTH = 255;

		public Guid Id { get; set; }
		[Required]
		[MaxLength(NAME_MAX_LENGTH)]
		[MinLength(NAME_MIN_LENGTH)]
		public string Name { get; set; }
		[Required]
		[MaxLength(DESCRIPTION_MAX_LENGTH)]
		public string Description { get; set; }

		public List<RightsInGroup>? RightInGroups { get; set; }
		public List<AdminsInGroup>? AdminsInGroup { get; set; }
	}
}
