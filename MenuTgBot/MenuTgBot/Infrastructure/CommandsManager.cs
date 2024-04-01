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

namespace MenuTgBot.Infrastructure
{
    internal class CommandsManager
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IDbContextFactory<ApplicationContext> _contextFactory;
        private readonly Dictionary<long, StateManager> _stateManagers;
        public MenuBotCommandHandler[] Commands { get; private set; }

        public CommandsManager(ITelegramBotClient client, IDbContextFactory<ApplicationContext> contextFactory)
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
        /// <param name="userId"></param>
        /// <returns></returns>
        private async Task InitStateManagerIfNotExistsAsync(long chatId)
        {
            if (!_stateManagers.ContainsKey(chatId))
            {
                _stateManagers[chatId] = await StateManager.CreateAsync(_botClient, _contextFactory, this, chatId);
            }
        }

        /// <summary>
        /// парсинг команды от пользователя
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private MenuBotCommandHandler GetCommand(Message message)
        {
            long userId = message.From.Id;
            MenuBotCommandHandler command = null;

            command = Commands
                .FirstOrDefault(x =>
                    x.Command == message.Text);
            return command;
        }


        /// <summary>
        /// создание меню
        /// </summary>
        /// <returns></returns>
        private MenuBotCommandHandler[] GetComands()
        {
            return new MenuBotCommandHandler[]
            {
                new StartCommand(
                        "/start",
                        CommandDisplay.None),
                new CatalogCommand(
                    $"📦{MessagesText.CommandShopCatalog}",
                    CommandDisplay.ButtonMenu),
                new CartCommand(
                    $"🛒{MessagesText.CommandCart}",
                    CommandDisplay.ButtonMenu),
                new OrdersCommand(
                    MessagesText.CommandOrder,
                    CommandDisplay.ButtonMenu)
            };
        }

        #endregion

        #region Public Methods
        /// <summary>
        /// обработка сообщения от пользователя
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<bool> ProcessMessageAsync(Message message)
        {
            long chatId = message.Chat.Id;
            await InitStateManagerIfNotExistsAsync(chatId);

            MenuBotCommandHandler command = GetCommand(message);

            if (command.IsNotNull())
            {
                await command.StartCommandAsync(_stateManagers[chatId], message);
                return true;
            }

            return await _stateManagers[chatId].NextStateAsync(message);
        }

        /// <summary>
        /// обработка запроса от пользователя
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public async Task<bool> ProcessQueryAsync(CallbackQuery query)
        {
            long chatId = query.Message.Chat.Id;
            await InitStateManagerIfNotExistsAsync(chatId);

            return await _stateManagers[chatId].NextStateAsync(query);
        }

        /// <summary>
        /// показать кнопки меню
        /// </summary>
        /// <param name="chatId"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public async Task<Message> ShowButtonMenuAsync(long chatId, string text, IEnumerable<KeyboardButton> additionalButtons = null)
        {
            IEnumerable<KeyboardButton> defaultButtons = GetDefaultMenuButtons();

            List<IEnumerable<KeyboardButton>> keyboard = new List<IEnumerable<KeyboardButton>>()
            {
                defaultButtons
            };

            if (additionalButtons.IsNotNull())
            {
                keyboard.Insert(0,additionalButtons);
            }
            ReplyKeyboardMarkup markup = new ReplyKeyboardMarkup(keyboard)
            {
                ResizeKeyboard = true
            };

            Message result = await _botClient.SendTextMessageAsync(chatId, text, replyMarkup: markup);
            
            return result;
        }

        public IEnumerable<KeyboardButton> GetDefaultMenuButtons()
        {
            IEnumerable<KeyboardButton> result = Commands
                    .Where(command => command.DisplayMode == CommandDisplay.ButtonMenu)
                    .Select(command =>
                        new KeyboardButton(command.Command));
            
            return result;
        }

        #endregion
    }
}
