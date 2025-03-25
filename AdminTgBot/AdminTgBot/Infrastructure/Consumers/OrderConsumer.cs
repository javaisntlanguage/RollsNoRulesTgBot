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
using RabbitClient.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using Order = Database.Tables.Order;

namespace AdminTgBot.Infrastructure.Consumers
{
    internal class OrderConsumer : IConsumer<IOrder>
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

        public async Task ConsumeAsync(IOrder message)
        {
            await using ApplicationContext db = _contextFactory.CreateDbContext();
            AdminState[] admins = db.AdminStates
                .Select(x => x)
                .ToArray();

            Order? order = db.Orders.Find(message.OrderId);

            if (order == null)
            {
                throw new MessageHandlingException($"Не удалось найти заказ id={message.OrderId}");
            }

            if(order.State != OrderState.New)
            {
                throw new MessageHandlingException($"Заказ уже обработан id={message.OrderId}");
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
