﻿using Database;
using Helper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Util.Core.Enums;
using Telegram.Util.Core.Interfaces;

namespace Telegram.Util.Core
{
    public abstract class CommandsManager
    {
        protected ITelegramBotClient _botClient;
        protected Dictionary<long, StateManager> _stateManagers;
        public IBotCommandHandler[] Commands { get; protected set; }

        /// <summary>
        /// назначить состояние пользователю
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        protected abstract Task InitStateManagerIfNotExistsAsync(long chatId);

        /// <summary>
        /// создание меню
        /// </summary>
        /// <returns></returns>
        protected abstract IBotCommandHandler[] GetComands();

        /// <summary>
        /// парсинг команды от пользователя
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        protected IBotCommandHandler GetCommand(Message message)
        {
            long userId = message.From.Id;
            IBotCommandHandler command = null;

            command = Commands
                .FirstOrDefault(x =>
                    x.Command == message.Text);
            return command;
        }

        /// <summary>
        /// обработка сообщения от пользователя
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<bool> ProcessMessageAsync(Message message)
        {
            long chatId = message.Chat.Id;
            await InitStateManagerIfNotExistsAsync(chatId);

            IBotCommandHandler command = GetCommand(message);

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

        public IEnumerable<KeyboardButton> GetDefaultMenuButtons()
        {
            IEnumerable<KeyboardButton> result = Commands
                    .Where(command => command.DisplayMode == CommandDisplay.ButtonMenu)
                    .Select(command =>
                        new KeyboardButton(command.Command));

            return result;
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
                keyboard.Insert(0, additionalButtons);
            }
            ReplyKeyboardMarkup markup = new ReplyKeyboardMarkup(keyboard)
            {
                ResizeKeyboard = true
            };

            Message result = await _botClient.SendTextMessageAsync(chatId, text, replyMarkup: markup);

            return result;
        }
    }
}
