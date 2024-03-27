using Helper;
using Newtonsoft.Json;
using NLog;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace RabbitClient
{
    public class RabbitEventHandler
    {
        ConnectionFactory ConnectionFactory { get; set; }
        IConnection _connection;
        private Logger _logger;

        public RabbitEventHandler(Logger logger)
        {
            _logger = logger;
            ConnectionFactory = new ConnectionFactory { HostName = "localhost" };
            _connection = ConnectionFactory.CreateConnection();
        }

        public void Publish<TQueue>(object message)
        {
            IModel channel = _connection.CreateModel();
            string queue = typeof(TQueue).FullName;

            channel.QueueDeclare(
                queue: queue,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            string sMessage = JsonConvert.SerializeObject(message);
            byte[] body = Encoding.UTF8.GetBytes(sMessage);

            channel.BasicPublish(exchange: string.Empty,
                     routingKey: queue,
                     mandatory: false,
                     basicProperties: null,
                     body: body);

            _logger.Info($"Сообщение отправлено. Очередь: '{queue}'. Сообщение: '{sMessage}'");
        }

        public void Consume<TQueue>(IConsumer consumerObj)
        {
            IModel channel = _connection.CreateModel();
            string queue = typeof(TQueue).FullName;

            channel.QueueDeclare(
                queue: queue,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            EventingBasicConsumer consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                byte[] body = ea.Body.ToArray();
                string message = Encoding.UTF8.GetString(body);

                _logger.Info($"Сообщение получено. Очередь: '{queue}'. Сообщение: '{message}'");

                consumerObj.Consume(message);
            };

            channel.BasicConsume(queue: queue,
                     autoAck: true,
                     consumer: consumer);
        }
    }
}
