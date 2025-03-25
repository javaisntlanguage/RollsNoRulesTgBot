using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitClient
{
    public interface IRabbitEventHandler
    {
        void Publish<TQueue>(object message);
        void Publish(string queue, object message);
        /// <summary>
        /// публикует сообщение в транзакции. 
        /// Необходимо закоммитить транзакцию, чтобы опубликовать сообщение!
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="message"></param>
        /// <returns>канал, где можно закоммитить или откатить транзакцию</returns>
        IModel PublishWithTransaction(string queue, object message);
        /// <summary>
        /// публикует сообщение в транзакции. 
        /// Необходимо закоммитить транзакцию, чтобы опубликовать сообщение!
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="message"></param>
        /// <returns>канал, где можно закоммитить или откатить транзакцию</returns>
        IModel PublishWithTransaction<TQueue>(object message);
    }
}
