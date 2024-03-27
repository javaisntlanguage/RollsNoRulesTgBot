using Database;
using AdminTgBot.Infrastructure;
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
using AdminTgBot.Infrastructure.Exceptions;

namespace AdminTgBot
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
        private readonly CommandsManager _commandsManager;

        public TelegramWorker(TelegramBotClient telegramClient, string connectionString, Logger logger, CancellationTokenSource cancellationTokenSource)
        {
            TelegramClient = telegramClient;
            _logger = logger;
            _cancellationTokenSource = cancellationTokenSource;
            _commandsManager = new CommandsManager(TelegramClient, connectionString);
        }

        public TelegramBotClient TelegramClient { get; }
        
        public void Start()
        {
            TelegramClient.StartReceiving(UpdateHandler, ErrorHandler, _receiverOptions, _cancellationTokenSource.Token);
        }

        private async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            long chatId = update.Message?.Chat.Id ?? update.CallbackQuery.Message.Chat.Id;
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
            catch (AuthException)
            {
                await botClient.SendTextMessageAsync(chatId, MessagesText.AuthFail);
            }
            catch (Exception ex)
            {
                await botClient.SendTextMessageAsync(chatId, MessagesText.SomethingWrong);
                
                _logger.Error(ex);
                Console.WriteLine(ex.ToString());
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
