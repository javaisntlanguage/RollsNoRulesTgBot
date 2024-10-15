using AdminTgBot.Infrastructure.Interfaces;
using Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Options;
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
		private readonly IAdminStateMachineBuilder _stateMachineBuilder;
		private readonly IOptions<AdminSettings> _options;

		public StateManagerFactory(ITelegramBotClient botClient,
			IDbContextFactory<ApplicationContext> contextFactory,
			IMenuHandler menuHandler,
			IAdminStateMachineBuilder stateMachineBuilder,
			IOptions<AdminSettings> options)
		{
			_botClient = botClient;
			_contextFactory = contextFactory;
			_menuHandler = menuHandler;
			_stateMachineBuilder = stateMachineBuilder;
			_options = options;
		}

		public AdminBotStateManager Create(long chatId)
		{
			AdminBotStateManager stateManager = new(_botClient, _contextFactory, _menuHandler, _stateMachineBuilder, _options, chatId);
			ApplicationContext dataSource = _contextFactory.CreateDbContext();
			stateManager.ConfigureHandlers();
			stateManager.StateRecoveryAsync(dataSource).Wait();

			return stateManager;
		}
	}
}
