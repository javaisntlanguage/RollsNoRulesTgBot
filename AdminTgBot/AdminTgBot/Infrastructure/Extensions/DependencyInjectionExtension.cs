using AdminTgBot.Infrastructure.Consumers;
using AdminTgBot.Infrastructure.Interfaces;
using AdminTgBot.Infrastructure.Models;
using Database;
using MessageContracts;
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
using RabbitClient;
using RabbitMQ.Client;
using RabbitClient.Connection;

namespace AdminTgBot.Infrastructure.Extensions
{
    public static class DependencyInjectionExtension
	{
		public static IServiceCollection AddTelegramBot(this IServiceCollection services, IConfiguration configuration)
		{
			services
				.Configure<AdminSettings>(configuration.GetSection("AdminSettings"))
				.Configure<RabbitMqSettings>(configuration.GetSection("RabbitMqSettings"));
			services
				.AddTelegramBotClient(configuration)
				.AddSingleton<IDbContextFactory<ApplicationContext>, ContextFactory>()
				.AddSingleton<IMenuHandlerBuilder, MenuHandlerBuilder>()
				.AddSingleton<IMenuHandler>(service => service.GetRequiredService<IMenuHandlerBuilder>().Build())
				.AddSingleton<ICommandsManager, AdminBotCommandsManager>()
				.AddSingleton<IAdminStateMachineBuilder, AdminStateMachineBuilder>()
				.AddSingleton<IAdminStateManagerFactory, StateManagerFactory>()
				.AddSingleton<IThreadsManager, ThreadsManager>()
				.AddSingleton<IConsumer<IOrder>, OrderConsumer>()
				.AddSingleton<IIdempotentConsumer<IOrder, IConsumer<IOrder>>, IdempotentConsumer<IOrder, IConsumer<IOrder>>>()
				.AddSingleton<IBaseConnectionCreator, BaseConnectionCreator>()
				.AddSingleton<IConnection>(service => service.GetRequiredService<IBaseConnectionCreator>().Create())
				.AddSingleton<ITelegramWorker, TelegramWorker>()
				.AddSingleton<MessageJsonSerializerSettings>();
			services
				.AddHostedService<ConsumerService<IOrder, IConsumer<IOrder>>>();

			return services;
		}
	}
}
