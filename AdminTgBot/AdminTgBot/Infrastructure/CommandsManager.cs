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

namespace AdminTgBot.Infrastructure
{
    internal class CommandsManager
    {
        private readonly ITelegramBotClient _botClient;
        private readonly string _connectionString;
        private readonly Dictionary<long, StateManager> _stateManagers;
        public AdminBotCommandHandler[] Commands { get; private set; }

        public CommandsManager(ITelegramBotClient client, string connectionString)
        {
            _botClient = client;
            _connectionString = connectionString;
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
                _stateManagers[userId] = await StateManager.Create(_botClient, _connectionString, this, userId, chatId);
            }
        }

        /// <summary>
        /// парсинг команды от пользователя
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private AdminBotCommandHandler GetCommand(Message message)
        {
            long userId = message.From.Id;
            AdminBotCommandHandler command = null;
            
            command = Commands
                .FirstOrDefault(x =>
                    x.Command == message.Text);
            return command;
        }


        /// <summary>
        /// создание меню
        /// </summary>
        /// <returns></returns>
        private AdminBotCommandHandler[] GetComands()
        {
            return new AdminBotCommandHandler[]
            {
                new StartCommand(
                    "/start",
                    CommandDisplay.None),
                new CatalogEditorCommand(
                    $"{MessagesText.CommandCatalogEditor}",
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
            long userId = message.From.Id;
            long chatId = message.Chat.Id;
            await InitStateManagerIfNotExistsAsync(userId, chatId);

            AdminBotCommandHandler command = GetCommand(message);

            if (command.IsNotNull())
            {
                await command.StartCommandAsync(_stateManagers[chatId], message);
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
            ReplyKeyboardMarkup keyboard = new ReplyKeyboardMarkup(
                Commands
                    .Where(command => command.DisplayMode == CommandDisplay.ButtonMenu)
                    .Select(command =>
                        new KeyboardButton(command.Command)))
            {
                ResizeKeyboard = true
            };

            await _botClient.SendTextMessageAsync(chatId, text, replyMarkup: keyboard);
        }

        #endregion
    }
}
