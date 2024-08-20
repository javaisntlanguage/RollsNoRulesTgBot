using AdminTgBot.Infrastructure.Conversations;
using AdminTgBot.Infrastructure.Conversations.Administration;
using AdminTgBot.Infrastructure.Conversations.BotOwner;
using AdminTgBot.Infrastructure.Conversations.CatalogEditor;
using AdminTgBot.Infrastructure.Conversations.Lkk;
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
        public required BotOwnerConversation BotOwner { get; set; }
		public required LkkConversation Lkk { get; set; }
		public required AdministrationConversation Administration { get; set; }

		internal Dictionary<string, IConversation> GetHandlers(ApplicationContext dataSource, AdminBotStateManager statesManager)
        {
            Dictionary<string, IConversation> result = new Dictionary<string, IConversation>();

			AddToHandlers(statesManager, result, Start);
			AddToHandlers(statesManager, result, CatalogEditor);
			AddToHandlers(statesManager, result, Orders);
			AddToHandlers(statesManager, result, BotOwner);
			AddToHandlers(statesManager, result, Lkk);
			AddToHandlers(statesManager, result, Administration);

            return result;
        }

        private void AddToHandlers<T>(AdminBotStateManager statesManager, Dictionary<string, IConversation> result, T source) where T : class, IConversation, new()
		{
			Type type = typeof(T);
			T conversation = (T)Activator.CreateInstance(type, statesManager)!;

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
