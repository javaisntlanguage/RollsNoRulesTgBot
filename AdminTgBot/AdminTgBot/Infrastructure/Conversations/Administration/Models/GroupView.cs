using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdminTgBot.Infrastructure.Conversations.Administration.Models
{
	internal class GroupView
	{
		public Guid Id { get; set; }
		public string Name { get; set; }
		public bool IsInGroup { get; set; }
	}
}
