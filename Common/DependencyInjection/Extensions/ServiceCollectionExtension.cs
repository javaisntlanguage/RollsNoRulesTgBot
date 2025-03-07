using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DependencyInjection.Extensions
{
	public static class ServiceCollectionExtension
	{
		private const string CONFIGURE_SERVICES_METHOD_NAME = "ConfigureServices";

        /// <summary>
        /// использует метод <see cref="CONFIGURE_SERVICES_METHOD_NAME"/> класса TStartup
        /// </summary>
        /// <typeparam name="TStartup"></typeparam>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IServiceCollection UseStartup<TStartup>(this IServiceCollection services, IConfiguration configuration)
			where TStartup : class
		{
			Type startupType = typeof(TStartup);
			MethodInfo? cfgServicesMethod = startupType.GetMethod(CONFIGURE_SERVICES_METHOD_NAME, new Type[] { typeof(IServiceCollection) });
			bool hasConfigCtor = startupType.GetConstructor(new Type[] { typeof(IConfiguration) }) != null;
			TStartup startup = hasConfigCtor
						? (TStartup)Activator.CreateInstance(typeof(TStartup), configuration)!
						: (TStartup)Activator.CreateInstance(typeof(TStartup), null)!;

			cfgServicesMethod?.Invoke(startup, new object[] { services });

			return services;
		}

		/// <summary>
		/// создает хост
		/// </summary>
		/// <param name="serviceCollection"></param>
		/// <returns></returns>
		public static IHost BuildHost(this IServiceCollection serviceCollection)
		{
            IHost host = Host.CreateDefaultBuilder()
				.ConfigureServices(services =>
				{
                    services.Add(serviceCollection);	
                })
				.Build();
			return host;
        }
    }
}
