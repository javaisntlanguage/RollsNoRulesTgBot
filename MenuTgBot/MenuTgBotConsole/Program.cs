using DependencyInjection;
using DependencyInjection.Inferfaces;
using DependencyInjection.Extensions;
using MenuTgBot;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NLog.Fluent;
using System.Reflection;
using MenuTgBotConsole;

// чтобы при обновлении бд не запускалась программа
if (EF.IsDesignTime)
{
    return;
}

IocBuilder iocBuilder = new();
string basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

IConfigurationRoot config = iocBuilder
	.CreateConfigurationBuilder(basePath, "appsettings.json")
	.Build();

ServiceProvider iocContainer = iocBuilder
	.CreateIocContainer()
	.UseStartup<Startup>(config)
	.BuildServiceProvider();

IApplicationRunner runner = iocContainer.GetRequiredService<IApplicationRunner>();
runner.Run();

await Task.Delay(-1);