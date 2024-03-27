using Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NLog;
using System.Net;
using Telegram.Bot;

namespace MenuTgBot
{
    public class MenuTgBotMain
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        public MenuTgBotMain(IConfiguration configuration) 
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
        }
    }
}
