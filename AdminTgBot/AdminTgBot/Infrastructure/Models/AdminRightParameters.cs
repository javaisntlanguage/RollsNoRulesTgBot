using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdminTgBot.Infrastructure.Models
{
	internal class AdminRightParameters
	{
		public Guid RightForCheck {  get; set; }
		public int AdminId { get; set; }
	}
}
