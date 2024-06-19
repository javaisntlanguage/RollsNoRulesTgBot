using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace AdminTgBot.Infrastructure.Commands
{
	internal class DefaultCommands
	{
		public IEnumerable<BotCommand> Commands { get; set; }
		public DefaultCommands() 
		{
			Commands = new List<BotCommand>()
			{
				new BotCommand()
				{
					Command = MessagesText.CommandButtons,
					Description = MessagesText.CommandButtonsDescriptions
				}
			};
		}
	}
}
