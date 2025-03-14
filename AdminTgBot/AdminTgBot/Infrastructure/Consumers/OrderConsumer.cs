using AdminTgBot.Infrastructure.Models;
using Database;
using Database.Enums;
using Database.Tables;
using Helper;
using MessageContracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Newtonsoft.Json;
using NLog;
using RabbitClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace AdminTgBot.Infrastructure.Consumers
{
    internal class OrderConsumer : IOrderConsumer
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IDbContextFactory<ApplicationContext> _contextFactory;
        private readonly ILogger _logger;

        public OrderConsumer(
            ITelegramBotClient telegramBotClient,
            IDbContextFactory<ApplicationContext> contextFactory,
            ILogger logger) 
        {
            _botClient = telegramBotClient;
            _contextFactory = contextFactory;
            _logger = logger;
        }

        public async Task ConsumeAsync(string message)
        {
            await using ApplicationContext db = _contextFactory.CreateDbContext();
            AdminState[] admins = db.AdminStates
                .Select(x => x)
                .ToArray();

            int orderId = JsonConvert.DeserializeObject<int>(message);
            Order? order = db.Orders.Find(orderId);

            if (order == null)
            {
                _logger.Error($"Не удалось найти заказ id={orderId}");
                return;
            }

            if(order.State != OrderState.New)
            {
                _logger.Warn($"Заказ уже обработан id={orderId}");
                return;
            }

            string[] orderCart = db.OrderCarts
                .Where(oc => oc.OrderId == order.Id)
                .Select(oc => string.Format(OrderConsumerText.OrderPosition, oc.ProductName, oc.Count))
                .ToArray();

            string sCart = string.Join('\n', orderCart);
            string text = string.Format(OrderConsumerText.OrderDetails, order.Number, sCart);

            InlineKeyboardMarkup markup = new InlineKeyboardMarkup(GetOrderButtons(order.Id));

            try
            {
                admins
                    .ForEach(admin => _botClient.SendTextMessageAsync(admin.UserId, text, replyMarkup: markup));
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }

            _logger.Info($"Заказ id='{order.Id}' получен. {text}");
        }

        private InlineKeyboardButton[] GetOrderButtons(int orderId)
        {
            InlineKeyboardButton[] result = new InlineKeyboardButton[]
            {
                new InlineKeyboardButton(OrderConsumerText.Accept)
                {
                    CallbackData = JsonConvert.SerializeObject(new
                    {
						IsOutOfQueue = true,
						Cmd = Command.AcceptOrder,
                        OrderId = orderId
                    })
                },
                new InlineKeyboardButton(OrderConsumerText.Decline)
                {
                    CallbackData = JsonConvert.SerializeObject(new
                    {
						IsOutOfQueue = true,
						Cmd = Command.DeclineOrder,
                        OrderId = orderId
                    })
                },
            };

            return result;
        }
    }
}
