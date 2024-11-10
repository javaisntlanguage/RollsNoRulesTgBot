using DependencyInjection.Inferfaces;
using MenuTgBot.Infrastructure.Extensions;
using MenuTgBotConsole.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MenuTgBotConsole
{
	internal class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}


		public void ConfigureServices(IServiceCollection services)
		{
			services
				.AddSingleton<IConfiguration>(service => Configuration)
				.AddSingleton<ILogger>(service => LogManager.GetCurrentClassLogger())
				.AddTelegramBot(Configuration)
				.AddSingleton<IApplicationRunner, ApplicationRunner>();
		}


		public IConfiguration Configuration { get; }
	}
}
