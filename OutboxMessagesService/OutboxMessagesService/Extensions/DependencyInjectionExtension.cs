using Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitClient;
using RabbitClient.Connection;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutboxMessagesService.Extensions
{
    public static class DependencyInjectionExtension
    {
        public static IServiceCollection AddOutboxMessagesService(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .Configure<RabbitMqSettings>(configuration.GetSection("RabbitMqSettings"));
            services
                .AddSingleton<IDbContextFactory<ApplicationContext>, ContextFactory>()
                .AddSingleton<IBaseConnectionCreator, BaseConnectionCreator>()
                .AddSingleton<IConnection>(service => service.GetRequiredService<IBaseConnectionCreator>().Create())
                .AddSingleton<IRabbitEventHandler, RabbitEventHandler>()
                .AddHostedService<OutboxMessagesService>();

            return services;
        }
    }
}
