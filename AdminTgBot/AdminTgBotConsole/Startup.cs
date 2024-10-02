using AdminTgBot;
using AdminTgBot.Infrastructure.Extensions;
using AdminTgBot.Infrastructure.Models;
using AdminTgBotConsole.Models;
using DependencyInjection.Inferfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdminTgBotConsole
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
				.AddSingleton<IConfiguration>(serive => Configuration)
				.AddSingleton<ILogger>(service => LogManager.GetCurrentClassLogger())
				.AddTelegramBot(Configuration)
				.AddSingleton<IApplicationRunner, ApplicationRunner>();
		}


		public IConfiguration Configuration { get; }
	}
}
