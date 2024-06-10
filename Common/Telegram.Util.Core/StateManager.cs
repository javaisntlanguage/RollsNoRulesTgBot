using Database;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Util.Core.StateMachine.Graph;
using Telegram.Util.Core.StateMachine;
using Telegram.Util.Core.Interfaces;
using Microsoft.EntityFrameworkCore.Internal;
using Telegram.Util.Core.Exceptions;
using Microsoft.IdentityModel.Tokens;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Helper;

namespace Telegram.Util.Core
{
    public abstract class StateManager
    {
        private int MAX_MESSAGE_LENGTH = 4096;
        protected static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        protected CommandsManager _commandsManager;
        protected Message _message;
        protected CallbackQuery _query;
        protected int? _lastMessageId;
        protected ITelegramBotClient _botClient;

        public long ChatId { get; protected set; }

        /// <summary>
        /// назначение классов-обработчиков команд
        /// </summary>
        protected abstract void ConfigureHandlers();
        /// <summary>
        /// конфигурация конечного автомата
        /// </summary>
        protected abstract void ConfigureMachine();

        /// <summary>
        /// показать кнопки меню
        /// </summary>
        /// <param name="chatId"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        private async Task<Message> ShowButtonMenuAsync(long chatId, string text, IEnumerable<KeyboardButton>? additionalButtons = null)
        {
            IEnumerable<KeyboardButton> defaultButtons = _commandsManager.GetDefaultMenuButtons();

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

        protected async Task NextStateMessageAsync()
        {
            await NextStateAsync(_message);
        }

        protected async Task NextStateQueryAsync()
        {
            await NextStateAsync(_query);
        }

        public virtual async Task<bool> NextStateAsync(Message message)
        {
            _message = message;
            _query = null!;

            return default;
        }

        public virtual async Task<bool> NextStateAsync(CallbackQuery query)
        {
            _message = null!;
            _query = query;

            return default;
        }

        public async Task<Message> SendMessageAsync(string text, ParseMode? parseMode = null, IReplyMarkup? replyMarkup = null, string? photo = null, bool isOutOfQueue = false)
        {
            Message result = null!;

            if (text.Length > MAX_MESSAGE_LENGTH)
            {
                string textFirstPart = text.Substring(0, MAX_MESSAGE_LENGTH);
                await _botClient.SendTextMessageAsync(ChatId, textFirstPart, parseMode: parseMode);
                text = text.Substring(MAX_MESSAGE_LENGTH);
            }

            if (photo.IsNullOrEmpty())
            {
                result = await _botClient.SendTextMessageAsync(ChatId, text, parseMode: parseMode, replyMarkup: replyMarkup);
            }
            else
            {
                await using Stream stream = new MemoryStream(Convert.FromBase64String(photo!));
                InputFileStream inputFile = InputFile.FromStream(stream);
                result = await _botClient.SendPhotoAsync(ChatId, inputFile, caption: text, parseMode: parseMode, replyMarkup: replyMarkup);
            }

            if (!isOutOfQueue)
            {
                _lastMessageId = result.MessageId;
            }

            return result;
        }

        public async Task<Message> EditMessageReplyMarkupAsync(int messageId, InlineKeyboardMarkup replyMarkup)
        {
            return await _botClient.EditMessageReplyMarkupAsync(ChatId, messageId, replyMarkup);
        }

        public async Task<Message> ShowButtonMenuAsync(string text, IEnumerable<KeyboardButton> additionalButtons = null)
        {
            Message result = await ShowButtonMenuAsync(ChatId, text, additionalButtons);
            return result;
        }
    }
}
