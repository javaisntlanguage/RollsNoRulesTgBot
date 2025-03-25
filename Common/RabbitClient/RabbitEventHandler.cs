using Helper;
using MessageContracts;
using Newtonsoft.Json;
using NLog;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace RabbitClient
{
    public class RabbitEventHandler : IRabbitEventHandler
    {
        private readonly IConnection _connection;
        private readonly ILogger _logger;

        public RabbitEventHandler(ILogger logger, IConnection connection)
        {
            _logger = logger;
            _connection = connection;
        }

        public void Publish<TQueue>(object message)
        {
            string queue = nameof(TQueue);
            Publish(queue, message);
        }

        public void Publish(string queue, object message)
        {
            IModel channel = _connection.CreateModel();
            Publish(channel, queue, message);
        }

        public IModel PublishWithTransaction<TQueue>(object message)
        {
            string queue = nameof(TQueue);
            IModel result = PublishWithTransaction(queue, message);

            return result;
        }
        public IModel PublishWithTransaction(string queue, object message)
        {
            IModel channel = _connection.CreateModel();
            channel.TxSelect();
            Publish(channel, queue, message);

            return channel;
        }

        private IModel Publish(IModel channel, string queue, object message)
        {
            channel.QueueDeclare(
                queue: queue,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            string sMessage = message is string ? message.ToString()! : JsonConvert.SerializeObject(message);
            byte[] body = Encoding.UTF8.GetBytes(sMessage);

            channel.BasicPublish(exchange: string.Empty,
                     routingKey: queue,
                     mandatory: false,
                     basicProperties: null,
                     body: body);

            _logger.Info($"Сообщение отправлено. Очередь: '{queue}'. Сообщение: '{sMessage}'");

            return channel;
        }
    }
}
