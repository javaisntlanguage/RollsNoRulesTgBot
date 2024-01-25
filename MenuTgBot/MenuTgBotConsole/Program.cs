using MenuTgBot;
using Microsoft.Extensions.Configuration;

IConfiguration Configuration = new ConfigurationBuilder()
   .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
   .AddEnvironmentVariables()
   .AddCommandLine(args)
   .Build();

TelegramMain telegram = new TelegramMain(Configuration);
telegram.Start();

await Task.Delay(-1);