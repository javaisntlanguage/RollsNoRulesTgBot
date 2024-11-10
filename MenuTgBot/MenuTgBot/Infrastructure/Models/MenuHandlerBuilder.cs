using MenuTgBot.Infrastructure.Commands;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Util.Core.Enums;
using Telegram.Util.Core.Interfaces;
using Telegram.Util.Core.Models;

namespace MenuTgBot.Infrastructure.Models
{
	internal class MenuHandlerBuilder : IMenuHandlerBuilder
	{
		private readonly MenuBotSettings _config;

		public MenuHandlerBuilder(IOptions<MenuBotSettings> options)
		{
			_config = options.Value;
		}
		public IMenuHandler Build()
		{
			IBotCommandHandler[] commands = GetCommands();
			return new MenuHandler(commands);

		}

		private IBotCommandHandler[] GetCommands()
		{
			return
			[
				new StartCommand(
						MessagesText.CommandStart,
						CommandDisplay.None),
				new CatalogCommand(
					MessagesText.CommandShopCatalog,
					CommandDisplay.ButtonMenu),
				new CartCommand(
					MessagesText.CommandCart,
					CommandDisplay.ButtonMenu),
				new OrdersCommand(
					MessagesText.CommandOrder,
					CommandDisplay.ButtonMenu)
			];
		}
	}
}
