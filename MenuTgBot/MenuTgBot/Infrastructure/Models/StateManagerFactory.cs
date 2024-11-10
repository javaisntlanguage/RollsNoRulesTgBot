using Database;
using MenuTgBot.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Util.Core.Interfaces;

namespace MenuTgBot.Infrastructure.Models
{
	internal class StateManagerFactory : IMenuBotStateManagerFactory
	{
		private readonly ITelegramBotClient _botClient;
		private readonly IDbContextFactory<ApplicationContext> _contextFactory;
		private readonly IMenuHandler _menuHandler;
		private readonly IMenuBotStateMachineBuilder _stateMachineBuilder;
		private readonly IOptions<MenuBotSettings> _options;

		public StateManagerFactory(ITelegramBotClient botClient,
			IDbContextFactory<ApplicationContext> contextFactory,
			IMenuHandler menuHandler,
			IMenuBotStateMachineBuilder stateMachineBuilder,
			IOptions<MenuBotSettings> options)
		{
			_botClient = botClient;
			_contextFactory = contextFactory;
			_menuHandler = menuHandler;
			_stateMachineBuilder = stateMachineBuilder;
			_options = options;
		}

		public MenuBotStateManager Create(long chatId)
		{
			MenuBotStateManager stateManager = new(_botClient, _contextFactory, _menuHandler, _stateMachineBuilder, _options, chatId);
			ApplicationContext dataSource = _contextFactory.CreateDbContext();
			stateManager.ConfigureHandlers();
			stateManager.StateRecoveryAsync(dataSource).Wait();

			return stateManager;
		}
	}
}
