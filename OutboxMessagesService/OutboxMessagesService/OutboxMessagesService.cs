using Database;
using Database.Tables;
using Helper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using NLog;
using RabbitClient;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace OutboxMessagesService
{
    public class OutboxMessagesService : BackgroundService
    {
        private readonly IDbContextFactory<ApplicationContext> _contextFactory;
        private readonly ILogger _logger;
        private readonly IRabbitEventHandler _rabbitEventHandler;

        public OutboxMessagesService(
            IDbContextFactory<ApplicationContext> contextFactory,
            ILogger logger,
            IRabbitEventHandler rabbitEventHandler)
        {
            _contextFactory = contextFactory;
            _logger = logger;
            _rabbitEventHandler = rabbitEventHandler;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            TimeSpan interval = TimeSpan.FromSeconds(1);
            using PeriodicTimer timer = new PeriodicTimer(interval);

            while (!stoppingToken.IsCancellationRequested &&
                await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await PushPendingMessagesAsync();
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Не удалось отправить сообщения");
                }
            }
        }

        private async Task PushPendingMessagesAsync()
        {
            using ApplicationContext dataSource = _contextFactory.CreateDbContext();

            IEnumerable<OutboxMessage> messages = await GetPendingMessagesAsync(dataSource);
            await PublishMessagesAsync(dataSource, messages);
        }

        private async Task PublishMessagesAsync(ApplicationContext dataSource, IEnumerable<OutboxMessage> messages)
        {
            foreach (OutboxMessage message in messages)
            {
                IModel? channel = null;
                try
                {
                    channel = _rabbitEventHandler.PublishWithTransaction(message.Queue, message.Message);
                    await DeleteMessageFromDbAsync(dataSource, message);
                    channel.TxCommit();
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Не удалось отправить сообщение в брокер");
                    channel?.TxRollback();
                    await SaveMessageErrorAsync(dataSource, message, ex);
                }
            }
        }

        private async Task SaveMessageErrorAsync(ApplicationContext dataSource, OutboxMessage message, Exception error)
        {
            try
            {
                message.Error = ExcDetails.Get(error);
                dataSource.OutboxMessages.Update(message);
                await dataSource.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Не удалось сохранить сообщение об ошибке отправки сообщения в брокер");
            }
        }

        private async Task DeleteMessageFromDbAsync(ApplicationContext dataSource, OutboxMessage message)
        {
            dataSource.OutboxMessages.Remove(message);
            await dataSource.SaveChangesAsync();
        }

        private async Task<IEnumerable<OutboxMessage>> GetPendingMessagesAsync(ApplicationContext dataSource)
        {
            return await dataSource.OutboxMessages.Select(x => x)
                .ToListAsync();
        }
    }
}
