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
using Telegram.Util.Core.Interfaces;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using Update = Telegram.Bot.Types.Update;

namespace MenuTgBot
{
    internal class TelegramWorker : ITelegramWorker
	{
		private static readonly ReceiverOptions _receiverOptions;
		private readonly ILogger _logger;
		private readonly IThreadsManager _threadsManager;
		private readonly ITelegramBotClient _telegramClient;

        static TelegramWorker()
        {
			_receiverOptions = new ReceiverOptions
			{
				AllowedUpdates = new UpdateType[]
	            {
		            UpdateType.Message,
		            UpdateType.CallbackQuery
	            },
				ThrowPendingUpdates = true,
			};
		}

		public TelegramWorker(
			ITelegramBotClient telegramBotClient,
			ILogger logger,
			IThreadsManager threadsManager)
		{
			_telegramClient = telegramBotClient;
			_logger = logger;
			_threadsManager = threadsManager;

		}

		public TelegramBotClient TelegramClient { get; }

		public void Start()
		{
			_telegramClient.StartReceiving(UpdateHandler, ErrorHandler, _receiverOptions);
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
