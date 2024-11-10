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
using Database.Tables;
using Telegram.Util.Core.Models;

namespace Telegram.Util.Core
{
    public abstract class StateManager
    {
        private int MAX_MESSAGE_LENGTH = 4096;
        protected static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        protected Message _message;
        protected CallbackQuery _query;
        protected int? _lastMessageId;
        protected ITelegramBotClient _botClient;
		protected readonly IDbContextFactory<ApplicationContext> _contextFactory;
		private readonly IMenuHandler _menuHandler;

		public long ChatId { get; protected set; }

        protected StateManager(ITelegramBotClient botClient, 
            IDbContextFactory<ApplicationContext> contextFactory,
			IMenuHandler menuHandler)
        {
            _botClient = botClient;
			_contextFactory = contextFactory;
			_menuHandler = menuHandler;
		}

        /// <summary>
        /// назначение классов-обработчиков команд
        /// </summary>
        public abstract void ConfigureHandlers();

		private async Task<string> CutTextAsync(string text, ParseMode? parseMode)
		{
			while (text.Length > MAX_MESSAGE_LENGTH)
			{
				string textFirstPart = text.Substring(0, MAX_MESSAGE_LENGTH);
				await _botClient.SendTextMessageAsync(ChatId, textFirstPart, parseMode: parseMode);
				text = text.Substring(MAX_MESSAGE_LENGTH);
			}

            return text;
		}
		private async Task<string> CutMessageTextAsync(string text, ParseMode? parseMode)
		{
			while (text.Length > MAX_MESSAGE_LENGTH)
			{
				string textFirstPart = text.Substring(0, MAX_MESSAGE_LENGTH);
				await _botClient.SendTextMessageAsync(ChatId, textFirstPart, parseMode: parseMode);
				text = text.Substring(MAX_MESSAGE_LENGTH);
			}

			return text;
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

        public async Task<Message> SendMessageAsync(string text, IReplyMarkup? markup = null, ParseMode? parseMode = null, string? photo = null, bool isOutOfQueue = false)
        {
            Message result = null!;

            text = await CutTextAsync(text, parseMode);

            if (photo.IsNullOrEmpty())
            {
                result = await _botClient.SendTextMessageAsync(ChatId, text, parseMode: parseMode, replyMarkup: markup);
            }
            else
            {
                await using Stream stream = new MemoryStream(Convert.FromBase64String(photo!));
                InputFileStream inputFile = InputFile.FromStream(stream);
                result = await _botClient.SendPhotoAsync(ChatId, inputFile, caption: text, parseMode: parseMode, replyMarkup: markup);
            }

            if (!isOutOfQueue)
            {
                _lastMessageId = result.MessageId;
            }

            return result;
        }

		public async Task<Message> SendVideoAsync(string text, string path, ParseMode? parseMode = null, IReplyMarkup? replyMarkup = null, bool isOutOfQueue = false)
		{
			Message result = null!;

			text = await CutMessageTextAsync(text, parseMode);


			await using Stream stream = System.IO.File.OpenRead(path);
			InputFileStream inputFile = InputFile.FromStream(stream);
			result = await _botClient.SendVideoAsync(ChatId, inputFile, caption: text, parseMode: parseMode, replyMarkup: replyMarkup);

			if (!isOutOfQueue)
			{
				_lastMessageId = result.MessageId;
			}

			return result;
		}

        public async Task DeleteMessageAsync(Message message)
        {
            await DeleteMessageAsync(message.MessageId);
        }

        public async Task DeleteMessageAsync(int messageId)
        {
            await _botClient.DeleteMessageAsync(ChatId, messageId);
        }

		public async Task<Message> EditMessageReplyMarkupAsync(int messageId, InlineKeyboardMarkup replyMarkup)
        {
            return await _botClient.EditMessageReplyMarkupAsync(ChatId, messageId, replyMarkup);
        }

        public async Task<Message> ShowButtonMenuAsync(string text, IEnumerable<KeyboardButton> additionalButtons = null)
        {
			List<IEnumerable<KeyboardButton>> defaultButtons = _menuHandler.GetDefaultMenuButtons();

			if (additionalButtons.IsNotNull())
			{
				defaultButtons.Insert(0, additionalButtons);
			}
			ReplyKeyboardMarkup markup = new ReplyKeyboardMarkup(defaultButtons)
			{
				ResizeKeyboard = true
			};

			Message result = await _botClient.SendTextMessageAsync(ChatId, text, replyMarkup: markup);

			return result;
		}

		public void CheckText(string? text)
		{
			if (text.IsNullOrEmpty())
            {
                throw new MessageTextException();
            }
		}

		public void CheckTextLength(string? text, int? minLength = null, int? maxLength = null)
        {
            if (minLength == null &&  maxLength == null)
            {
                throw new ArgumentException("Минимальная или максимальная длины должны быть заданы");
            }

			if (minLength != null && text!.Length < minLength)
			{
                throw new MinLengthMessageException(minLength);
			}

			if (maxLength != null && text!.Length > maxLength)
			{
				throw new MaxLengthMessageException(maxLength);
			}
		}

        public void CheckTextAndLength(string? text, int? minLength = null, int? maxLength = null)
        {
            CheckText(text);
            CheckTextLength(text, minLength, maxLength);
        }
	}
}
