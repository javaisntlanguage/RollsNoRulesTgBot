using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Tables
{
	[PrimaryKey(nameof(GroupId), nameof(RightId))]
	public class RightsInGroup
	{
		[ForeignKey(nameof(Group))]
		public Guid GroupId { get; set; }
		[ForeignKey(nameof(AdminRight))]
		public Guid RightId { get; set; }

		public RightGroup? Group {  get; set; }
		public Right? AdminRight {  get; set; }
	}
}
