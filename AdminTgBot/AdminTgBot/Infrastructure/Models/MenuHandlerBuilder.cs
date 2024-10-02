using AdminTgBot.Infrastructure.Commands;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Util.Core.Enums;
using Telegram.Util.Core.Interfaces;
using Telegram.Util.Core.Models;

namespace AdminTgBot.Infrastructure.Models
{
	internal class MenuHandlerBuilder : IMenuHandlerBuilder
	{
		private readonly AdminSettings _config;

		public MenuHandlerBuilder(IOptions<AdminSettings> options)
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
				new CatalogEditorCommand(
					MessagesText.CommandCatalogEditor,
					CommandDisplay.ButtonMenu),
				new OrdersCommand(
					MessagesText.CommandOrders,
					CommandDisplay.ButtonMenu),
				new ButtonsCommand(
					MessagesText.CommandButtons,
					CommandDisplay.None),
				new BotOwnerCommand(
					_config.TelegramBotToken,
					CommandDisplay.None),
				new LkkCommand(
					MessagesText.CommandLkk,
					CommandDisplay.ButtonMenu),
				new AdministrationCommand(
					MessagesText.CommandAdministration,
					CommandDisplay.ButtonMenu),
			];
		}

	}
}
