using MenuTgBot.Infrastructure.Models;
using Microsoft.Extensions.Options;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Util.Core.Exceptions;
using Telegram.Util.Core.Interfaces;
using Telegram.Util.Core.Models;

namespace MenuTgBot.Infrastructure
{
    internal class ThreadsManager : IThreadsManager
	{
        private static readonly ConcurrentDictionary<long, object> Users = new ConcurrentDictionary<long, object>();
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly ITelegramBotClient _telegramClient;
        private readonly ICommandsManager _commandsManager;
        private readonly MenuBotSettings _config;

        public ThreadsManager(ITelegramBotClient telegramClient,
            ICommandsManager commandsManager,
			IOptions<MenuBotSettings> options) 
        {
            _telegramClient = telegramClient;
            _commandsManager = commandsManager;
            _config = options.Value;
        }

        public async Task<bool> ProcessUpdate(Update update)
        {
            long chatId = update.Message?.Chat.Id ?? update.CallbackQuery.Message.Chat.Id;

            if (Users.TryAdd(chatId, new()))
            {
                _ = Task.Run(() => SetMessageTimeout(chatId));
                await ProcessUpdateForUser(update);
                Users.TryRemove(chatId, out _);
                return true;
            }

            //может быть слишком много запросов
            try
            {
                await _telegramClient.SendChatActionAsync(chatId, ChatAction.Typing);
            }
            catch
            {

            }

            return false;
        }

        private void SetMessageTimeout(long chatId)
        {
            Thread.Sleep(_config.MessageTimeoutSec * 1000);
            Users.TryRemove(chatId, out _);
        }

        private async Task ProcessUpdateForUser(Update update)
        {
			long chatId = update.Message?.Chat.Id ?? update.CallbackQuery.Message.Chat.Id;

			try
			{
                switch (update.Type)
                {
                    case UpdateType.Message:
                        {
                            Message message = update.Message!;

                            if (!await _commandsManager.ProcessMessageAsync(message))
                            {
                                await ProcessUnknownCommandAsync(message);
                            }
                            break;
                        }
                    case UpdateType.CallbackQuery:
                        {
                            CallbackQuery query = update.CallbackQuery!;

                            if (!await _commandsManager.ProcessQueryAsync(query))
                            {
                                await ProcessUnknownQueryAsync(query);
                            }
                            break;
                        }
                }
            }
			catch (MinLengthMessageException ex)
			{
				string errorText = string.Format(MessagesText.ValueTooShort, ex.MinLength);
				await _telegramClient.SendTextMessageAsync(chatId, errorText);
			}
			catch (MaxLengthMessageException ex)
			{
				string errorText = string.Format(MessagesText.ValueTooLong, ex.MaxLength);
				await _telegramClient.SendTextMessageAsync(chatId, errorText);
			}
			catch (NotLastMessageException ex)
            {
                await _telegramClient.AnswerCallbackQueryAsync(update.CallbackQuery.Id, MessagesText.GoLastMessage, showAlert: true);
            }
            catch (Exception ex)
            {
                await _telegramClient.SendTextMessageAsync(chatId, MessagesText.SomethingWrong);

                string message = $"UserId: {chatId}\n";
                _logger.Error(message, ex);
                Console.WriteLine(message + ex.ToString());
            }
        }

        /// <summary>
        ///     Обработка команды не известной для бота
        /// </summary>
        /// <param name="message">Сообщение с командой </param>
        private async Task ProcessUnknownCommandAsync(Message message)
        {
            long chatId = message.Chat.Id;
            await _telegramClient.SendTextMessageAsync(chatId, MessagesText.UnknownCommand);
        }

        /// <summary>
        ///     Обработка команды не известной для бота
        /// </summary>
        /// <param name="message">Сообщение с командой </param>
        private async Task ProcessUnknownQueryAsync(CallbackQuery query)
        {
            string queryId = query.Id.ToString();
            await _telegramClient.AnswerCallbackQueryAsync(queryId, MessagesText.UnknownCommand, showAlert: true);
        }
    }
}
