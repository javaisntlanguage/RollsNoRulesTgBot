using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Tables
{
	[PrimaryKey(nameof(AdminId), nameof(GroupId))]
	public class AdminsInGroup
	{
		[ForeignKey(nameof(Admin))]
		public int AdminId { get; set; }
		[ForeignKey(nameof(Group))]
		public Guid GroupId { get; set; }

		public AdminCredential? Admin { get; set; }
		public RightGroup? Group { get; set; }
		public List<RightsInGroup>? RightInGroups { get; set; }
	}
}
