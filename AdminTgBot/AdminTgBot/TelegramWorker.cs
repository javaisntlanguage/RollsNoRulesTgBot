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
using System.Threading;
using AdminTgBot.Infrastructure.Commands;

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
        private readonly ContextFactory _contextFactory;
        private readonly AdminBotCommandsManager _commandsManager;
        private readonly ThreadsManager _threadsManager;

        public static string BotToken {  get; private set; }

        public TelegramWorker(TelegramBotClient telegramClient, string connectionString, string botToken, Logger logger, int timeout, CancellationTokenSource cancellationTokenSource)
        {
            BotToken = botToken;
            TelegramClient = telegramClient;
            _logger = logger;
            _cancellationTokenSource = cancellationTokenSource;
            _contextFactory = new ContextFactory(connectionString);
            _commandsManager = new AdminBotCommandsManager(TelegramClient, _contextFactory);
            _threadsManager = new ThreadsManager(TelegramClient, _commandsManager, timeout);

        }

        public TelegramBotClient TelegramClient { get; }
        
        public void Start()
        {
			DefaultCommands defaultCommands = new DefaultCommands();

			//TelegramClient.SetMyCommandsAsync(defaultCommands.Commands);
            TelegramClient.StartReceiving(UpdateHandler, ErrorHandler, _receiverOptions, _cancellationTokenSource.Token);
        }

        private void UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
			Task.Run(() => ProcessUpdateAsync(update));
        }

		private async Task ProcessUpdateAsync(Update update)
		{
			try
			{
				while (!await _threadsManager.ProcessUpdate(update))
				{
					continue;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				_logger.Error(ex);
			}
		}

		private void ErrorHandler(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken)
        {
			string ErrorMessage = error switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => error.ToString()
            };

            Console.WriteLine(ErrorMessage);
			_logger.Error(ErrorMessage);
        }
    }
}
