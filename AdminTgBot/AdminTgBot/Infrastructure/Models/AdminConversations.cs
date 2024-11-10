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

		internal Dictionary<string, IConversation> GetHandlers(ApplicationContext dataSource, AdminBotStateManager stateManager, AdminSettings config)
        {
            Dictionary<string, IConversation> result = new Dictionary<string, IConversation>();

			AddToHandlers(stateManager, result, Start);
			AddToHandlers(stateManager, result, CatalogEditor);
			AddToHandlers(stateManager, result, Orders);
			AddToHandlers(stateManager, result, BotOwner, [config]);
			AddToHandlers(stateManager, result, Lkk);
			AddToHandlers(stateManager, result, Administration);

            return result;
        }

        private void AddToHandlers<T>(AdminBotStateManager stateManager, Dictionary<string, IConversation> result, T source, List<object>? sourceParameters=null) where T : class, IConversation, new()
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
