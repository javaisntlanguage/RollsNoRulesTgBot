using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot;
using Helper;
using Robox.Telegram.Util.Core;
using Database;
using Database.Tables;
using Microsoft.EntityFrameworkCore;
using MenuTgBot.Infrastructure.Conversations.Start;
using MenuTgBot.Infrastructure.Conversations.Catalog;
using MenuTgBot.Infrastructure.Conversations.Cart;

namespace MenuTgBot.Infrastructure
{
    internal class CommandsManager
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ApplicationContext _dataSource;
        private readonly Dictionary<long, StateManager> _stateManagers;
        public BotCommandHandler[] Commands { get; private set; }

        public CommandsManager(ITelegramBotClient client, ApplicationContext dataSource)
        {
            _botClient = client;
            _dataSource = dataSource;
            _stateManagers = new Dictionary<long, StateManager>();
            Commands = GetComands();
        }

        #region Private Methods

        /// <summary>
        /// назначить состояние пользователю
        /// </summary>
        /// <param name="chatId"></param>
        /// <returns></returns>
        private async Task InitStateManagerIfNotExistsAsync(long userId, long chatId)
        {
            if (!_stateManagers.ContainsKey(userId))
            {
                _stateManagers[userId] = await StateManager.Create(_botClient, _dataSource, this, userId, chatId);
            }
        }

        /// <summary>
        /// парсинг команды от пользователя
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private BotCommandHandler GetCommand(Message message)
        {
            long userId = message.From.Id;
            BotCommandHandler command = null;
            if (message.Entities!.IsNotNullOrEmpty())
                command = _stateManagers[userId].Commands.Values
                    .FirstOrDefault(x =>
                        message.EntityValues.Contains(x.Command));
            else
                command = _stateManagers[userId].Commands.Values
                    .FirstOrDefault(x =>
                        x.TriggerCommands
                            .Contains(message.Text));
            return command;
        }


        /// <summary>
        /// создание меню
        /// </summary>
        /// <returns></returns>
        private BotCommandHandler[] GetComands()
        {
            return new BotCommandHandler[]
            {
            new(
                "start",
                typeof(StartConversation),
                MessagesText.CommandStart
            ),
            new(
                "catalog",
                typeof(ShopCatalogConversation),
                MessagesText.CommandShopCatalog,
                CommandDisplay.ButtonMenu,
                new List<string>
                {
                    $"📦{MessagesText.CommandShopCatalog}"
                }),
            new(
                "cart",
                typeof(CartConversation),
                MessagesText.CommandCart,
                CommandDisplay.ButtonMenu,
                new List<string>
                {
                    $"🛒{MessagesText.CommandCart}"
                }),
            /*
            new(
                "сontacts",
                typeof(ShopContactsConversation),
                MessagesText.CommandShopContacts,
                CommandDisplay.ButtonMenu,
                new List<string>
                {
                    $"☎️{MessagesText.CommandShopContacts}"
                })*/
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
            long userId = message.From.Id;
            long chatId = message.Chat.Id;
            await InitStateManagerIfNotExistsAsync(userId, chatId);

            BotCommandHandler command = GetCommand(message);

            if (command.IsNotNull())
            {
                await command.TriggerAction(message);
                return true;
            }

            return await _stateManagers[userId].NextStateAsync(message);
        }

        /// <summary>
        /// обработка запроса от пользователя
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public async Task<bool> ProcessQueryAsync(CallbackQuery query)
        {
            long userId = query.From.Id;
            long chatId = query.Message.Chat.Id;
            await InitStateManagerIfNotExistsAsync(userId, chatId);

            return await _stateManagers[userId].NextStateAsync(query);
        }

        /// <summary>
        /// показать кнопки меню
        /// </summary>
        /// <param name="chatId"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public async Task ShowButtonMenuAsync(long chatId, long userId, string text)
        {
            ReplyKeyboardMarkup keyboard = new(
                Commands
                    .Where(command => command.DisplayMode == CommandDisplay.ButtonMenu &&
                                      command.TriggerCommands.IsNotEmpty() &&
                                      (command.Roles.IsNullOrEmpty() || _stateManagers[userId].Roles.In(command.Roles)))
                    .Select(command =>
                        new KeyboardButton(command.TriggerCommands
                            .FirstOrDefault())))
            {
                ResizeKeyboard = true
            };

            await _botClient.SendTextMessageAsync(chatId, text, replyMarkup: keyboard);
        }

        #endregion
    }
}
