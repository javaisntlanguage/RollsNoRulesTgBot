using AdminTgBot.Infrastructure.Models;
using Database;
using Database.Enums;
using Database.Tables;
using Helper;
using MessageContracts;
using Microsoft.EntityFrameworkCore;
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
    internal class OrderConsumer : IConsumer, IOrder
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly TelegramBotClient _botClient;
        private readonly string _connectionString;

        public OrderConsumer(TelegramBotClient telegramBotClient, string connectionString) 
        {
            _botClient = telegramBotClient;
            _connectionString = connectionString;
        }

        public void Consume(string message)
        {
            using ApplicationContext db = new ApplicationContext(_connectionString);
            AdminState[] admins = db.AdminStates
                .Select(x => x)
                .ToArray();

            int orderId = JsonConvert.DeserializeObject<int>(message);
            Order order = db.Orders.Find(orderId);

            if (order.IsNull())
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
                .Include(oc => oc.Product)
                .Where(oc => oc.OrderId == order.Id)
                .Select(oc => string.Format(OrderConsumerText.OrderPosition, oc.Product.Name, oc.Count))
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
                        Cmd = Command.AcceptOrder,
                        OrderId = orderId
                    })
                },
                new InlineKeyboardButton(OrderConsumerText.Decline)
                {
                    CallbackData = JsonConvert.SerializeObject(new
                    {
                        Cmd = Command.DeclineOrder,
                        OrderId = orderId
                    })
                },
            };

            return result;
        }
    }
}
