using Database;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdminTgBotConsole.Models
{
    public class ApplicationContextFactory : IDesignTimeDbContextFactory<ApplicationContext>
    {
        ApplicationContext IDesignTimeDbContextFactory<ApplicationContext>.CreateDbContext(string[] args)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
                .Build();

			DbContextOptionsBuilder<ApplicationContext> builder = new();
			string? connectionString = configuration["ConnectionString"];

            builder.UseSqlServer(connectionString);

            return new ApplicationContext(builder.Options);
        }
    }
}
