using AdminTgBot;
using AdminTgBotConsole;
using DependencyInjection;
using DependencyInjection.Extensions;
using DependencyInjection.Inferfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;

if(EF.IsDesignTime)
{
    return;
}

IocBuilder iocbuilder = new();
string basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

IConfigurationRoot config = iocbuilder
    .CreateConfigurationBuilder(basePath, "appsettings.json")
    .Build();

ServiceProvider iocContainer = iocbuilder
    .CreateIocContainer()
    .UseStartup<Startup>(config)
    .BuildServiceProvider();

IApplicationRunner runner = iocContainer.GetRequiredService<IApplicationRunner>();
runner.Run();

await Task.Delay(-1);