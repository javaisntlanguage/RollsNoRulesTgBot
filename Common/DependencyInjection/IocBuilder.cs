using DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace DependencyInjection
{
	public class IocBuilder
	{
		public IConfigurationBuilder CreateConfigurationBuilder(string basePath, string appSettingsFilename)
		{
			return new ConfigurationBuilder()
				.SetBasePath(basePath)
				.AddJsonFile(appSettingsFilename, false, false)
				.AddEnvironmentVariables();
		}

		public IServiceCollection CreateIocContainer()
		{
			ServiceCollection services = new();
			return services;
		}
	}
}
