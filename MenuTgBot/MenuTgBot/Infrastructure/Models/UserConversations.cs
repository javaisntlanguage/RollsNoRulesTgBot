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
        public StartConversation Start { get; set; }
        public CatalogConversation Catalog { get; set; }
        public CartConversation Cart { get; set; }
        public OrdersConversation Orders { get; set; }

        public Dictionary<string, IConversation> GetHandlers(MenuBotStateManager stateManager, MenuBotSettings config)
        {
			Dictionary<string, IConversation> result = new Dictionary<string, IConversation>();

			AddToHandlers(stateManager, result, Start);
			AddToHandlers(stateManager, result, Catalog);
			AddToHandlers(stateManager, result, Cart);
			AddToHandlers(stateManager, result, Orders, [config]);        

            return result;
        }

		private void AddToHandlers<T>(MenuBotStateManager stateManager, Dictionary<string, IConversation> result, T source, List<object>? sourceParameters = null) where T : class, IConversation, new()
		{
			Type type = typeof(T);

			sourceParameters ??= new List<object>();
			sourceParameters.Insert(0, stateManager);

			T conversation = (T)Activator.CreateInstance(type, sourceParameters.ToArray())!;

			if (source.IsNotNull())
			{
				SetPublicProperties(source, conversation);
			}

			result.Add(type.Name!, conversation);
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
