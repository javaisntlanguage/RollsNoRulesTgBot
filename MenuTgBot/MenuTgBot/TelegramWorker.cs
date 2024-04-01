using Database;
using MenuTgBot.Infrastructure;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Util.Core.Exceptions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using Update = Telegram.Bot.Types.Update;

namespace MenuTgBot
{
    internal class TelegramWorker
    {
        private static readonly ReceiverOptions _receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new UpdateType[]
            {
                UpdateType.Message,
                UpdateType.CallbackQuery
            },
            ThrowPendingUpdates = true,
        };
        private readonly Logger _logger;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ContextFactory _contextFactory;
        private readonly CommandsManager _commandsManager;

        public TelegramWorker(TelegramBotClient telegramClient, string connectionString, Logger logger, CancellationTokenSource cancellationTokenSource)
        {
            TelegramClient = telegramClient;
            _logger = logger;
            _cancellationTokenSource = cancellationTokenSource;
            _contextFactory = new ContextFactory(connectionString);
            _commandsManager = new CommandsManager(TelegramClient, _contextFactory);
        }

        public TelegramBotClient TelegramClient { get; }
        
        public void Start()
        {
            TelegramClient.StartReceiving(UpdateHandlerAsync, ErrorHandler, _receiverOptions, _cancellationTokenSource.Token);
        }

        private async Task UpdateHandlerAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
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
                await TelegramClient.AnswerCallbackQueryAsync(update.CallbackQuery.Id, MessagesText.GoLastMessage, showAlert: true);
            }
            catch (Exception ex)
            {
                long chatId = update.Message?.Chat.Id ?? update.CallbackQuery.Message.Chat.Id;
                await botClient.SendTextMessageAsync(chatId, MessagesText.SomethingWrong);
                
                string message = $"UserId: {chatId}\n";
                _logger.Error(message, ex);
                Console.WriteLine(message + ex.ToString());
            }
        }

        private Task ErrorHandler(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken)
        {
            var ErrorMessage = error switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => error.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }

        /// <summary>
        ///     Обработка команды не известной для бота
        /// </summary>
        /// <param name="message">Сообщение с командой </param>
        private async Task ProcessUnknownCommandAsync(Message message)
        {
            long chatId = message.Chat.Id;
            await TelegramClient.SendTextMessageAsync(chatId, MessagesText.UnknownCommand);
        }
        /// <summary>
        ///     Обработка команды не известной для бота
        /// </summary>
        /// <param name="message">Сообщение с командой </param>
        private async Task ProcessUnknownQueryAsync(CallbackQuery query)
        {
            string queryId = query.Id.ToString();
            await TelegramClient.AnswerCallbackQueryAsync(queryId, MessagesText.UnknownCommand, showAlert: true);
        }
    }
}
