using DependencyInjection.Inferfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using OutboxMessagesService.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ILogger = NLog.ILogger;

namespace OutboxMessagesServiceConsole
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
                .AddOutboxMessagesService(Configuration);
        }


        public IConfiguration Configuration { get; }
    }
}
