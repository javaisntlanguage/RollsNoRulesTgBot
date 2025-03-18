using DependencyInjection;
using DependencyInjection.Extensions;
using DependencyInjection.Inferfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OutboxMessagesServiceConsole;
using System.Reflection;

// для сборки при накате EF
if (EF.IsDesignTime)
{
    return;
}

IocBuilder iocBuilder = new();
string basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

IConfigurationRoot config = iocBuilder
    .CreateConfigurationBuilder(basePath, "appsettings.json")
    .Build();

IHost host = iocBuilder
    .CreateIocContainer()
    .UseStartup<Startup>(config)
    .BuildHost();

await host.RunAsync();