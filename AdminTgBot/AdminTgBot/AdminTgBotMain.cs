using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System.Net;
using Database;
using Telegram.Bot;
using Microsoft.EntityFrameworkCore;
using RabbitClient;
using MessageContracts;
using AdminTgBot.Infrastructure.Consumers;

namespace AdminTgBot
{
    public class AdminTgBotMain
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        public AdminTgBotMain(IConfiguration configuration)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void Start()
        {
            TelegramBotClient telegramClient = new TelegramBotClient(Configuration["TelegramBotToken"]!, new HttpClient());

            string connectionString = Configuration["ConnectionString"];

            TelegramWorker worker = new TelegramWorker(telegramClient, connectionString, _logger, _cancellationTokenSource);
            worker.Start();

            RabbitEventHandler rabbitEventHandler = new RabbitEventHandler(_logger);
            rabbitEventHandler.Consume<IOrder>(new OrderConsumer(telegramClient, connectionString));
        }
    }
}
