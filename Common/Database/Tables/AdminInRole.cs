using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Tables
{
	[PrimaryKey(nameof(UserId), nameof(RoleId))]
	public class AdminInRole
	{

		[ForeignKey("User")]
		public long UserId { get; set; }
		[ForeignKey("Role")]
		public int RoleId { get; set; }
		public User User { get; set; }
		public Role Role { get; set; }
	}
}
