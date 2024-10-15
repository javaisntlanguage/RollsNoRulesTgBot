using AdminTgBot.Infrastructure.Interfaces;
using AdminTgBot.Infrastructure.Models;
using Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Util.Core.Extensions;
using Telegram.Util.Core.Interfaces;
using Telegram.Util.Core.Models;

namespace AdminTgBot.Infrastructure.Extensions
{
    public static class DependencyInjectionExtension
	{
		public static IServiceCollection AddTelegramBot(this IServiceCollection services, IConfiguration configuration)
		{
			services
				.Configure<AdminSettings>(configuration.GetSection("AdminSettings"));
			services
				.AddTelegramBotClient(configuration)
				//.AddDbContext<ApplicationContext>(options => configuration.GetConnectionString("RollsNoRules"))
				.AddSingleton<IDbContextFactory<ApplicationContext>, ContextFactory>()
				.AddSingleton<IMenuHandlerBuilder, MenuHandlerBuilder>()
				.AddSingleton<IMenuHandler>(service => service.GetRequiredService<IMenuHandlerBuilder>().Build())
				.AddSingleton<ICommandsManager, AdminBotCommandsManager>()
				.AddSingleton<IAdminStateMachineBuilder, AdminStateMachineBuilder>()
				.AddSingleton<IAdminStateManagerFactory, StateManagerFactory>()
				.AddSingleton<IThreadsManager, ThreadsManager>()
				.AddSingleton<ITelegramWorker, TelegramWorker>();

			return services;
		}
	}
}
