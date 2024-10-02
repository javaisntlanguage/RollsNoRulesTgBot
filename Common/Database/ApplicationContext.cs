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
using Database.Enums;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Database.Resources;

namespace Database
{
    public class ApplicationContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<UserState> UserStates { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<ProductCategory> ProductCategories { get; set; }
        public DbSet<AdminCredential> AdminCredentials { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderCart> OrderCarts { get; set; }
        public DbSet<AdminState> AdminStates { get; set; }
        public DbSet<SellLocation> SellLocations { get; set; }
        public DbSet<AdminRight> AdminRights { get; set; }
        public DbSet<Right> Rights { get; set; }
        public DbSet<RightsInGroup> RightInGroups { get; set; }
        public DbSet<AdminsInGroup> AdminInGroups { get; set; }
        public DbSet<RightGroup> RightGroups { get; set; }

        public ApplicationContext(DbContextOptions options) : base(options)
        {
        }

        public void User_Add(long id)
        {
            SqlParameter IdParam = new SqlParameter("@Id", id);

            Database.ExecuteSqlRaw("User_Add @Id", IdParam);
        }

        public async Task SetAdminState(long userId, int stateId, string data, int? lastMessageId)
        {
            AdminState state = await AdminStates
                .FirstOrDefaultAsync(adminState =>  adminState.UserId == userId);

            if(state.IsNull())
            {
                AdminState newState = new AdminState()
                {
                    UserId = userId,
                    StateId = stateId,
                    Data = data,
                    LastMessageId = lastMessageId
                };

                await AdminStates.AddAsync(newState);
            }
            else
            {
                state.StateId = stateId;
                state.Data = data;
                state.LastMessageId = lastMessageId;
            }

            await SaveChangesAsync();
        }
        public async Task UserStates_Set(long userId, int stateId, string data, int? lastMessageId)
        {
            UserState state = await UserStates
                .FirstOrDefaultAsync(state => state.UserId == userId);

            if (state.IsNull())
            {
                UserState newState = new UserState()
                {
                    UserId = userId,
                    StateId = stateId,
                    Data = data,
                    LastMessageId = lastMessageId
                };

                await UserStates.AddAsync(newState);
            }
            else
            {
                state.StateId = stateId;
                state.Data = data;
                state.LastMessageId = lastMessageId;
            }

            await SaveChangesAsync();
        }
        public async Task<Order> TakeOrderAsync(long userId, IEnumerable<OrderCart> cart, decimal sum, string phone, long? arddressId, int? sellLocationId)
        {
            IQueryable<Order> currentDayOrders = Orders
                .Where(order => order.DateFrom.Date == DateTimeOffset.Now.Date);
            int? lastOrderNumber = currentDayOrders.Any() ? 
                currentDayOrders
                    .Max(order => order.Number) :
                null;

            int newOrderNumber = lastOrderNumber.HasValue ? lastOrderNumber.Value + 1 : 1;

            Order order = new Order()
            {
                UserId = userId,
                Number = newOrderNumber,
                DateFrom = DateTimeOffset.Now,
                Phone = phone,
                Sum = sum,
                AddressId = arddressId,
                SellLocationId = sellLocationId,
            };


            await Orders.AddAsync(order);

            await SaveChangesAsync();

            cart = cart.Select(orderCart =>
            {
                OrderCart orderCartNew = orderCart;
                orderCartNew.OrderId = order.Id;
                return orderCartNew;
            });

            await OrderCarts.AddRangeAsync(cart);

            await SaveChangesAsync();

            return order;
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ApplyConfigurationsFromAssembly(typeof(ApplicationContext).Assembly);
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

		public bool HasRight(int adminId, Guid rightId)
		{
			if (AdminRights
				.Where(ar => ar.AdminId == adminId && ar.RightId == rightId)
				.Select(ar => ar.RightId)
				.Union(AdminInGroups
					.Include(ag => ag.RightInGroups)
					.Where(ag => ag.AdminId == adminId)
					.SelectMany(ag => ag.RightInGroups)
					.Where(rg => rg.RightId == rightId)
					.Select(rg => rg.RightId))
				.Any(rigth => rigth == rightId))
            {
                return true;
            }



			return false;
		}
	}
}
