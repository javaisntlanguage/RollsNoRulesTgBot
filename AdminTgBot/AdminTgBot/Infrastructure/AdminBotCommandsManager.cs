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

namespace AdminTgBot.Infrastructure
{
    internal class AdminBotCommandsManager : CommandsManager
    {
        private readonly IDbContextFactory<ApplicationContext> _contextFactory;

        public AdminBotCommandsManager(ITelegramBotClient client, IDbContextFactory<ApplicationContext> contextFactory)
        {
            _botClient = client;
            _contextFactory = contextFactory;
            _stateManagers = new Dictionary<long, StateManager>();
            Commands = GetComands();
        }

        #region Private Methods

        /// <summary>
        /// назначить состояние пользователю
        /// </summary>
        /// <param name="chatId"></param>
        /// <returns></returns>
        protected override async Task InitStateManagerIfNotExistsAsync(long chatId)
        {
            if (!_stateManagers.ContainsKey(chatId))
            {
                _stateManagers[chatId] = await AdminBotStateManager.CreateAsync(_botClient, _contextFactory, this, chatId);
            }
        }


        /// <summary>
        /// создание меню
        /// </summary>
        /// <returns></returns>
        protected override AdminBotCommandHandler[] GetComands()
        {
            return new AdminBotCommandHandler[]
            {
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
                    TelegramWorker.BotToken,
                    CommandDisplay.None),
                new LkkCommand(
					MessagesText.CommandLkk,
                    CommandDisplay.ButtonMenu),
                new AdministrationCommand(
					MessagesText.CommandAdministration,
                    CommandDisplay.ButtonMenu),
            };
        }

        #endregion
    }
}
