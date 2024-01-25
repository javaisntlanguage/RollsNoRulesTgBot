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
    public class TelegramMain
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        public TelegramMain(IConfiguration configuration) 
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void Start()
        {
            TelegramBotClient telegramClient = new TelegramBotClient(Configuration["TelegramBotToken"]!, new HttpClient());

            IHost _host = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddDbContext<ApplicationContext>((x) => x.UseSqlServer(Configuration["ConnectionString"]));
                })
                .Build();

            ApplicationContext dataSource = _host.Services.GetRequiredService<ApplicationContext>();

            TelegramWorker worker = new TelegramWorker(telegramClient, dataSource, _logger, _cancellationTokenSource);
            worker.Start();
        }
    }
}
