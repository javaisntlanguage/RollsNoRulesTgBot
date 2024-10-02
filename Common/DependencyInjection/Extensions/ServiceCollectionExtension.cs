using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
		private const string ConfigureServicesMethodName = "ConfigureServices";

		public static IServiceCollection UseStartup<TStartup>(this IServiceCollection services, IConfiguration configuration)
			where TStartup : class
		{
			Type startupType = typeof(TStartup);
			MethodInfo? cfgServicesMethod = startupType.GetMethod(ConfigureServicesMethodName, new Type[] { typeof(IServiceCollection) });
			bool hasConfigCtor = startupType.GetConstructor(new Type[] { typeof(IConfiguration) }) != null;
			TStartup startup = hasConfigCtor
						? (TStartup)Activator.CreateInstance(typeof(TStartup), configuration)!
						: (TStartup)Activator.CreateInstance(typeof(TStartup), null)!;

			cfgServicesMethod?.Invoke(startup, new object[] { services });

			return services;
		}
	}
}
