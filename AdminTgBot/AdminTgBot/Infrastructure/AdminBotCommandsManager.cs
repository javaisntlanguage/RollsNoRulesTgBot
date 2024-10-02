using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot;
using Helper;
using Telegram.Util.Core;
using Database;
using Database.Tables;
using Microsoft.EntityFrameworkCore;
using Database.Enums;
using AdminTgBot.Infrastructure.Conversations.Start;
using AdminTgBot.Infrastructure.Conversations.CatalogEditor;
using AdminTgBot.Infrastructure.Commands;
using Telegram.Util.Core.Enums;
using Telegram.Util.Core.Interfaces;
using Microsoft.EntityFrameworkCore.Internal;
using AdminTgBot.Infrastructure.Models;
using AdminTgBot.Infrastructure.Interfaces;
using Microsoft.Extensions.Options;
using Telegram.Util.Core.Models;

namespace AdminTgBot.Infrastructure
{
    internal class AdminBotCommandsManager : CommandsManager
    {
        private readonly IDbContextFactory<ApplicationContext> _contextFactory;
		private readonly IAdminStateManagerFactory _stateManagerFactory;
		private readonly AdminSettings _config;

		public AdminBotCommandsManager(ITelegramBotClient botClient,
			IMenuHandler menuHandler,
			IDbContextFactory<ApplicationContext> contextFactory,
			IAdminStateManagerFactory stateManagerFactory,
            IOptions<AdminSettings> options) : base(botClient, menuHandler)
        {
            _contextFactory = contextFactory;
			_stateManagerFactory = stateManagerFactory;
            _config = options.Value;
			_stateManagers = new Dictionary<long, StateManager>();
        }

        #region Private Methods

        /// <summary>
        /// назначить состояние пользователя
        /// </summary>
        /// <param name="chatId"></param>
        /// <returns></returns>
        protected override async Task InitStateManagerIfNotExistsAsync(long chatId)
        {
            if (!_stateManagers.ContainsKey(chatId))
            {
                _stateManagers[chatId] = _stateManagerFactory.Create(chatId);
            }
        }
        #endregion
    }
}
