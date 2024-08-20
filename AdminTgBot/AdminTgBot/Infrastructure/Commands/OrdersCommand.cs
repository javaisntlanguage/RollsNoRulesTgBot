using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Util.Core.Enums;

namespace AdminTgBot.Infrastructure.Commands
{
	internal class OrdersCommand : AdminBotCommandHandler
	{
		public OrdersCommand(string command, CommandDisplay displayMode) : base(command, displayMode)
		{
		}

		public override async Task StartCommandAsync(object stateManager, Message message)
		{
			await base.StartCommandAsync(stateManager, message);

			await (stateManager as AdminBotStateManager)!.OrdersAsync(message);
		}
	}
}
