using AdminTgBot.Infrastructure.Conversations;
using AdminTgBot.Infrastructure.Conversations.CatalogEditor;
using AdminTgBot.Infrastructure.Conversations.Start;
using Database;
using Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;

namespace AdminTgBot.Infrastructure.Models
{
    internal class AdminConversations
    {
        public StartConversation Start { get; set; }
        public CatalogEditorConversation CatalogEditor { get; set; }

        internal Dictionary<string, IConversation> GetHandlers(ITelegramBotClient botClient, ApplicationContext dataSource, StateManager statesManager)
        {
            Dictionary<string, IConversation> result = new Dictionary<string, IConversation>();

            StartConversation start = new StartConversation(botClient, dataSource, statesManager);

            if (Start.IsNotNull())
            {
                start.Login = Start.Login;
            }
            
            result.Add(nameof(StartConversation), start);

            CatalogEditorConversation catalogEditor = new CatalogEditorConversation(dataSource, statesManager);

            if (CatalogEditor.IsNotNull())
            {
                catalogEditor.ProductId = CatalogEditor.ProductId;
                catalogEditor.AttributeValue = CatalogEditor.AttributeValue;
                catalogEditor.Product = CatalogEditor.Product;
                catalogEditor.CategoryId = CatalogEditor.CategoryId;
            }

            result.Add(nameof(CatalogEditorConversation), catalogEditor);

            return result;
        }
    }
}
