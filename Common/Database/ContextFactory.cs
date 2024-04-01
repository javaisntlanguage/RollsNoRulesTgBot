using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database
{
    public class ContextFactory : IDbContextFactory<ApplicationContext>
    {
        #region Protected properties

        protected string ConnectionString { get; set; }

        #endregion
        #region Public constructors

        public ContextFactory(string connectionString)
        {
            ConnectionString = connectionString;
        }

        #endregion
        #region Public methods

        public ApplicationContext CreateDbContext()
        {
            DbContextOptionsBuilder<ApplicationContext> optionsBuilder = new DbContextOptionsBuilder<ApplicationContext>();

            optionsBuilder.UseSqlServer(ConnectionString);

            return new ApplicationContext(optionsBuilder.Options);
        }

        #endregion
    }
}
