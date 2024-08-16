using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Tables
{
	[PrimaryKey(nameof(AdminId), nameof(RightId))]
	public class AdminPermission
	{
		[ForeignKey(nameof(AdminCredential))]
		public int AdminId { get; set; }
		[ForeignKey("Right")]
		public Guid RightId { get; set; }

		public AdminCredential AdminCredential { get; set; }
		public Right Right { get; set; }

	}
}
