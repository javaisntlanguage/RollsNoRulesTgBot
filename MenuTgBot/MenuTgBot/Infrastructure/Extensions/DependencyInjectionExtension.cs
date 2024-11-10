using Database;
using MenuTgBot.Infrastructure.Interfaces;
using MenuTgBot.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Util.Core.Extensions;
using Telegram.Util.Core.Interfaces;

namespace MenuTgBot.Infrastructure.Extensions
{
	public static class DependencyInjectionExtension
	{
		public static IServiceCollection AddTelegramBot(this IServiceCollection services, IConfiguration configuration)
		{
			services
				.Configure<MenuBotSettings>(configuration.GetSection("MenuBotSettings"));
			services
				.AddTelegramBotClient(configuration)
				.AddSingleton<IDbContextFactory<ApplicationContext>, ContextFactory>()
				.AddSingleton<IMenuHandlerBuilder, MenuHandlerBuilder>()
				.AddSingleton<IMenuHandler>(service => service.GetRequiredService<IMenuHandlerBuilder>().Build())
				.AddSingleton<ICommandsManager, MenuBotCommandsManager>()
				.AddSingleton<IMenuBotStateMachineBuilder, MenuBotStateMachineBuilder>()
				.AddSingleton<IMenuBotStateManagerFactory, StateManagerFactory>()
				.AddSingleton<IThreadsManager, ThreadsManager>()
				.AddSingleton<ITelegramWorker, TelegramWorker>();

			return services;
		}
	}
}
