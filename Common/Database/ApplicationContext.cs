using Database.Tables;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Helper;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Database
{
    public class ApplicationContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<UserState> UserStates { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserInRole> UserInRoles { get; set; }
        public DbSet<ProductCategories> ProductCategories { get; set; }

        public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options)
        {
        }

        public void User_Add(long id)
        {
            SqlParameter IdParam = new SqlParameter("@Id", id);

            Database.ExecuteSqlRaw("User_Add @Id", IdParam);
        }
        public async Task UserStates_Set(long userId, int stateId, string data, IEnumerable<RolesList> roles)
        {
            SqlParameter userIdParam = new SqlParameter("@UserId", userId);
            SqlParameter stateIdParam = new SqlParameter("@StateId", stateId);
            SqlParameter dataParam = new SqlParameter("@Data", data);

            Database.ExecuteSqlRaw("UserStates_Set @UserId, @StateId, @Data", userIdParam, stateIdParam, dataParam);
            IQueryable<Role> newRoles = UserInRoles
                .AsNoTracking()
                .Where(ur => ur.UserId == userId && !roles.Contains((RolesList)ur.RoleId) && (RolesList)ur.RoleId != RolesList.None)
                .Select(ur => ur.Role);

            if(newRoles.IsNotEmpty())
            {
                await UserInRoles.AddRangeAsync(newRoles.Select(role => new UserInRole { UserId = userId, RoleId = role.Id }));
                await SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<RolesList>> GetUserRoles(long userId)
        {
            return (await UserInRoles
                .AsNoTracking()
                .Where(ur => ur.UserId == userId)
                .Select(ur => (RolesList)ur.Role.Id)
                .ToListAsync())
                .AsEnumerable();
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            SetDecimal(builder);
        }

        private void SetDecimal(ModelBuilder builder)
        {
            IEnumerable<IMutableProperty> decimalProps = builder.Model
                .GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => (System.Nullable.GetUnderlyingType(p.ClrType) ?? p.ClrType) == typeof(decimal));

            foreach (var property in decimalProps)
            {
                property.SetPrecision(18);
                property.SetScale(2);
            }
        }
    }
}
