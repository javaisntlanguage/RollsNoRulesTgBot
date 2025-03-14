using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            return services;
        }
    }
}
