using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot;
using Helper;
using Database;
using Database.Tables;
using Microsoft.EntityFrameworkCore;
using MenuTgBot.Infrastructure.Conversations.Start;
using MenuTgBot.Infrastructure.Conversations.Catalog;
using MenuTgBot.Infrastructure.Conversations.Cart;
using MenuTgBot.Infrastructure.Conversations.Orders;
using Database.Enums;
using Telegram.Util.Core;
using Telegram.Util.Core.Enums;
using MenuTgBot.Infrastructure.Commands;
using Microsoft.EntityFrameworkCore.Internal;
using Telegram.Util.Core.Interfaces;
using Microsoft.Extensions.Options;
using MenuTgBot.Infrastructure.Models;
using MenuTgBot.Infrastructure.Interfaces;

namespace MenuTgBot.Infrastructure
{
    internal class MenuBotCommandsManager : CommandsManager
	{
		public MenuBotCommandsManager(ITelegramBotClient botClient,
			IMenuHandler menuHandler,
			IMenuBotStateManagerFactory stateManagerFactory,
			IOptions<MenuBotSettings> options) : base(botClient, menuHandler, stateManagerFactory)
		{
		}
	}
}
