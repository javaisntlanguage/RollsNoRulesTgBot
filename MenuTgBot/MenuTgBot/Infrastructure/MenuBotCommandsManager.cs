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

namespace MenuTgBot.Infrastructure
{
    internal class MenuBotCommandsManager : CommandsManager
    {
        private readonly IDbContextFactory<ApplicationContext> _contextFactory;

        public MenuBotCommandsManager(ITelegramBotClient client, IDbContextFactory<ApplicationContext> contextFactory)
        {
            _botClient = client;
            _contextFactory = contextFactory;
            _stateManagers = new Dictionary<long, StateManager>();
            Commands = GetComands();
        }

        #region Private Methods

        protected override async Task InitStateManagerIfNotExistsAsync(long chatId)
        {
            if (!_stateManagers.ContainsKey(chatId))
            {
                _stateManagers[chatId] = await MenuBotStateManager.CreateAsync(_botClient, _contextFactory, this, chatId);
            }
        }

        protected override IBotCommandHandler[] GetComands()
        {
            return new MenuBotCommandHandler[]
            {
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
            };
        }

        #endregion
    }
}
