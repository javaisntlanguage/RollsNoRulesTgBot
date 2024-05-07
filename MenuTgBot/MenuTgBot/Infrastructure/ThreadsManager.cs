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

namespace MenuTgBot.Infrastructure
{
    internal class ThreadsManager
    {
        private static readonly ConcurrentDictionary<long, object> Users = new ConcurrentDictionary<long, object>();
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly TelegramBotClient _telegramClient;
        private readonly CommandsManager _commandsManager;
        private readonly int _messageTimeout;

        public ThreadsManager(TelegramBotClient telegramClient, CommandsManager commandsManager, int timeout) 
        {
            _telegramClient = telegramClient;
            _commandsManager = commandsManager;
            _messageTimeout = timeout;
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
            Thread.Sleep(_messageTimeout * 1000);
            Users.TryRemove(chatId, out _);
        }

        private async Task ProcessUpdateForUser(Update update)
        {
            try
            {
                switch (update.Type)
                {
                    case UpdateType.Message:
                        {
                            Message message = update.Message;

                            if (!await _commandsManager.ProcessMessageAsync(message))
                            {
                                await ProcessUnknownCommandAsync(message);
                            }
                            break;
                        }
                    case UpdateType.CallbackQuery:
                        {
                            CallbackQuery query = update.CallbackQuery;

                            if (!await _commandsManager.ProcessQueryAsync(query))
                            {
                                await ProcessUnknownQueryAsync(query);
                            }
                            break;
                        }
                }
            }
            catch (NotLastMessageException ex)
            {
                long chatId = update.Message?.Chat.Id ?? update.CallbackQuery.Message.Chat.Id;
                await _telegramClient.AnswerCallbackQueryAsync(update.CallbackQuery.Id, MessagesText.GoLastMessage, showAlert: true);
            }
            catch (Exception ex)
            {
                long chatId = update.Message?.Chat.Id ?? update.CallbackQuery.Message.Chat.Id;
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
