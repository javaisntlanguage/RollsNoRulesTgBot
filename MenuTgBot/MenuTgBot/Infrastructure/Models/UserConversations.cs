using Database;
using MenuTgBot.Infrastructure.Conversations;
using MenuTgBot.Infrastructure.Conversations.Cart;
using MenuTgBot.Infrastructure.Conversations.Catalog;
using MenuTgBot.Infrastructure.Conversations.Start;
using Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using MenuTgBot.Infrastructure.Conversations.Orders;
using System.Reflection;

namespace MenuTgBot.Infrastructure.Models
{
    internal class UserConversations
    {
        public CartConversation Cart { get; set; }
        public OrdersConversation Orders { get; set; }

        public Dictionary<string, IConversation> GetHandlers(ApplicationContext dataSource, StateManager statesManager)
        {
            Dictionary<string, IConversation> result = new Dictionary<string, IConversation>
            {
                { nameof(StartConversation), new StartConversation(dataSource, statesManager) },
                { nameof(CatalogConversation), new CatalogConversation(dataSource, statesManager) },
            };

            CartConversation cartConversation = new CartConversation(dataSource, statesManager);
            
            if (Cart.IsNotNull())
            {
                SetPublicProperties(Cart, cartConversation);
            }

            result.Add(nameof(CartConversation), cartConversation);

            OrdersConversation ordersConversation = new OrdersConversation(dataSource, statesManager);

            if(Orders.IsNotNull())
            {
                SetPublicProperties(Orders, ordersConversation);
            }

            result.Add(nameof(OrdersConversation), ordersConversation);             

            return result;
        }

        private void SetPublicProperties<T>(T source, T target) where T : class, new()
        {
            typeof(T)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .ForEach(p => 
                p.SetValue(target, p.GetValue(source)));
        }
    }
}
