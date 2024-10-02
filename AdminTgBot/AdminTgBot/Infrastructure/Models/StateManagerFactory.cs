using AdminTgBot.Infrastructure.Interfaces;
using Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Util.Core;
using Telegram.Util.Core.Interfaces;

namespace AdminTgBot.Infrastructure.Models
{
	internal class StateManagerFactory : IAdminStateManagerFactory
	{
		private readonly ITelegramBotClient _botClient;
		private readonly IDbContextFactory<ApplicationContext> _contextFactory;
		private readonly IMenuHandler _menuHandler;

		public StateManagerFactory(ITelegramBotClient botClient,
			IDbContextFactory<ApplicationContext> contextFactory,
			IMenuHandler menuHandler)
		{
			_botClient = botClient;
			_contextFactory = contextFactory;
			_menuHandler = menuHandler;
		}

		public AdminBotStateManager Create(long chatId)
		{
			AdminBotStateManager stateManager = new(_botClient, _contextFactory, _menuHandler, chatId);

			stateManager.ConfigureMachine();
			stateManager.ConfigureHandlers();

			return stateManager;
		}
	}
}
