using AdminTgBot.Infrastructure.Conversations;
using AdminTgBot.Infrastructure.Conversations.CatalogEditor;
using AdminTgBot.Infrastructure.Conversations.Orders;
using AdminTgBot.Infrastructure.Conversations.Start;
using Database;
using Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;

namespace AdminTgBot.Infrastructure.Models
{
    internal class AdminConversations
    {
        public required StartConversation Start { get; set; }
        public required CatalogEditorConversation CatalogEditor { get; set; }
        public required OrdersConversation Orders { get; set; }

        internal Dictionary<string, IConversation> GetHandlers(ITelegramBotClient botClient, ApplicationContext dataSource, AdminBotStateManager statesManager)
        {
            Dictionary<string, IConversation> result = new Dictionary<string, IConversation>();

            StartConversation startConversation = new StartConversation(statesManager);

            if (Start.IsNotNull())
            {
				SetPublicProperties(Start, startConversation);
			}
            
            result.Add(nameof(StartConversation), startConversation);

            CatalogEditorConversation catalogEditorConversation = new CatalogEditorConversation(statesManager);

            if (CatalogEditor.IsNotNull())
            {
				SetPublicProperties(CatalogEditor, catalogEditorConversation);
			}

            result.Add(nameof(CatalogEditorConversation), catalogEditorConversation);

            OrdersConversation ordersConversation = new OrdersConversation(statesManager);

            if (Orders.IsNotNull())
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
