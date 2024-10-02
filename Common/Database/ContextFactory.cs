using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using System.Configuration;

namespace Database
{
    public class ContextFactory : IDbContextFactory<ApplicationContext>
    {
        IConfiguration _configuration;

		public ContextFactory(IConfiguration configuraion)
        {
			_configuration = configuraion;
        }

        public ApplicationContext CreateDbContext()
        {
            DbContextOptionsBuilder<ApplicationContext> optionsBuilder = new DbContextOptionsBuilder<ApplicationContext>();

            optionsBuilder.UseSqlServer(_configuration.GetConnectionString("RollsNoRules"));

            return new ApplicationContext(optionsBuilder.Options);
        }
    }
}
