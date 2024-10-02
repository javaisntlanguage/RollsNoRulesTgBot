using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Util.Core.Interfaces;
using Telegram.Util.Core.Models;

namespace Telegram.Util.Core.Extensions
{
	public static class DependencyInjectionExtension
	{
		public static IServiceCollection AddTelegramBotClient(this IServiceCollection services, IConfiguration configuration)
		{
			services
				.AddHttpClient("TelegramBotClient");
			services
				.Configure<TelegramSettings>(configuration.GetSection("TelegramSettings"))
				.AddSingleton<ITelegramBotClientBuilder, TelegramBotClientBuilder>()
				.AddSingleton<ITelegramBotClient>(service => service.GetRequiredService<ITelegramBotClientBuilder>().Build());
				
			return services;
		}
	}
}
