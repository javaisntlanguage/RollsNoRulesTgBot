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

namespace MenuTgBot.Infrastructure.Models
{
    internal class UserConversations
    {
        public CartConversation Cart { get; set; }

        internal Dictionary<string, IConversation> GetHandlers(ITelegramBotClient botClient, ApplicationContext dataSource, StateManager statesManager)
        {
            Dictionary<string, IConversation> result = new Dictionary<string, IConversation>
            {
                { nameof(StartConversation), new StartConversation(botClient, dataSource, statesManager) },
                { nameof(ShopCatalogConversation), new ShopCatalogConversation(botClient, dataSource, statesManager) }
            };

            CartConversation cartConversation = new CartConversation(botClient, dataSource, statesManager);
            
            if (Cart.IsNotNull())
            {
                cartConversation.Cart = Cart.Cart;

            }

            result.Add(nameof(CartConversation), cartConversation);             

            return result;
        }
    }
}
