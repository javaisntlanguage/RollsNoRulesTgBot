using Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace MenuTgBot.Infrastructure.Conversations
{
    internal interface IConversation
    {
        /// <summary>
        /// попытка связать сообщение с текущим обработчиком
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public Task<Trigger?> TryNextStepAsync(ApplicationContext dataSource, Message message);
        /// <summary>
        /// попытка связать запрос с текущим обработчиком
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public Task<Trigger?> TryNextStepAsync(ApplicationContext dataSource, CallbackQuery query);
    }
}
