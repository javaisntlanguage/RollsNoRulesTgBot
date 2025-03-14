using Microsoft.Extensions.DependencyInjection;
using NLog;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace RabbitClient
{
    /// <summary>
    /// сервис запускает прием из очереди <see cref="TQueue"/> на получателя <see cref="TConsumer"/> 
    /// </summary>
    /// <typeparam name="TQueue"></typeparam>
    /// <typeparam name="TConsumer"></typeparam>
    public class ConsumerService<TQueue, TConsumer> : BackgroundService where TConsumer : IConsumer
    {
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;
        private IConnection _connection;
        private IModel _channel;

        public ConsumerService(
            ILogger logger,
            IServiceProvider serviceProvider,
            IConnection connection)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _connection = connection;
            _channel = _connection.CreateModel();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            string queue = typeof(TQueue).FullName!;
            TConsumer specificConsumer = _serviceProvider.GetRequiredService<TConsumer>();

            _channel.QueueDeclare(
                queue: queue,
                durable: false,
                exclusive: false,
            autoDelete: false,
                arguments: null);

            EventingBasicConsumer consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                byte[] body = ea.Body.ToArray();
                string message = Encoding.UTF8.GetString(body);

                _logger.Info($"Сообщение получено. Очередь: '{queue}'. Сообщение: '{message}'");
                await specificConsumer.ConsumeAsync(message);
            };

            _channel.BasicConsume(queue: queue,
                     autoAck: true,
                     consumer: consumer);

            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            _channel.Close();
            _connection?.Close();
            base.Dispose();
        }
    }
}
