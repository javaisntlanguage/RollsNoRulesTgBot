using Database;
using Database.Tables;
using MessageContracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitClient
{
    public class IdempotentConsumer<TMessage, TConsumer> : IIdempotentConsumer<TMessage, TConsumer>
        where TMessage : IMessage
        where TConsumer : IConsumer<TMessage>
    {
        private readonly ILogger _logger;
        private readonly IDbContextFactory<ApplicationContext> _contextFactory;
        private IConsumer<TMessage> _consumer;

        public IdempotentConsumer(
            ILogger logger,
            IDbContextFactory<ApplicationContext> contextFactory,
            IConsumer<TMessage> consumer)
        {
            _logger = logger;
            _contextFactory = contextFactory;
            _consumer = consumer;
        }

        public async Task HandleAsync(TMessage message)
        {
            ApplicationContext dataSource = await _contextFactory.CreateDbContextAsync();
            string messageBody = JsonConvert.SerializeObject(message);

            OutboxMessageProcessed messageProcessed = new OutboxMessageProcessed()
            {
                Id = message.Id,
                Queue = nameof(TConsumer),
                Message = messageBody,
            };

            try
            {
                if (await dataSource.IsOutboxMessageProcessedAsync(message.Id))
                {
                    return;
                }

                await dataSource.MarkOutboxMessageAsProcessed(messageProcessed);
                
                await _consumer.ConsumeAsync(message);
            }
            catch(Exception ex)
            {
                _logger.Error(ex, "Error while handling Outbox Message");
                await dataSource.SetOutboxMessageError(messageProcessed, ex);
            }
        }
    }
}
