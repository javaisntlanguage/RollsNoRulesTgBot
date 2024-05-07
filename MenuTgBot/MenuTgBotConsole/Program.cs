using MenuTgBot;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

// чтобы при обновлении бд не запускалась программа
if (EF.IsDesignTime)
{
    return;
}

IConfiguration Configuration = new ConfigurationBuilder()
   .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
   .AddEnvironmentVariables()
   .AddCommandLine(args)
   .Build();

MenuTgBotMain telegram = new MenuTgBotMain(Configuration);
telegram.Start();

await Task.Delay(-1);