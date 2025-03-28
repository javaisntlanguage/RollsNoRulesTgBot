﻿using Microsoft.Extensions.DependencyInjection;
using NLog;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using MessageContracts;
using Helper.Converters;
using Newtonsoft.Json.Linq;
using static System.Net.Mime.MediaTypeNames;
using System.Text.RegularExpressions;

namespace RabbitClient
{
    /// <summary>
    /// сервис запускает прием из очереди <see cref="TQueue"/> на получателя <see cref="TConsumer"/> 
    /// </summary>
    /// <typeparam name="TQueue">очередь</typeparam>
    /// <typeparam name="TConsumer">получатель сообщений</typeparam>
    public class ConsumerService<TQueue, TConsumer> : BackgroundService 
        where TConsumer : IConsumer<TQueue> 
        where TQueue : IMessage
    {
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConnection _connection;
        private readonly MessageJsonSerializerSettings _messageJsonSerializerSettings;
        private readonly IModel _channel;

        public ConsumerService(
            ILogger logger,
            IServiceProvider serviceProvider,
            IConnection connection,
            MessageJsonSerializerSettings messageJsonSerializerSettings)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _connection = connection;
            _messageJsonSerializerSettings = messageJsonSerializerSettings;
            _channel = _connection.CreateModel();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            string queue = typeof(TQueue).Name!;
            IIdempotentConsumer<TQueue, TConsumer> idempotentConsumer = _serviceProvider
                .GetRequiredService<IIdempotentConsumer<TQueue, TConsumer>>();

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

                TQueue messageObject = JsonConvert.DeserializeObject<TQueue>(message, _messageJsonSerializerSettings)!;

                _logger.Info($"Сообщение получено. Очередь: '{queue}'. Сообщение: '{message}'");
                await idempotentConsumer.HandleAsync(messageObject);
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
