using Helper;
using System.Net;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using WebDav;
using static System.Net.Mime.MediaTypeNames;

namespace Telegram.Util.Core
{
    public static class TelegramHelper
    {
        public const string PRICE_FORMAT = "#.## ₽";
        public static string GetPhotoFileId(this Message message)
        {
            if (message.Photo.IsNotNull())
            {
                return message.Photo.Last().FileId;
            }

            if (message.Document.IsNotNull() && Path.GetExtension(message.Document.FileName).ToLower().In(
                "jpg",
                "jpeg",
                "ico",
                "png",
                "webp"))
            {
                return message.Document.FileId;
            }
            return null;
        }

        public static string GetCurrencySymbol(string currency)
        {
            switch (currency)
            {
                case "RU":
                case "RUB":
                    return "₽";
                case "KZ":
                case "KZT":
                    return "₸";
                default:
                    throw new Exception("Неизвестная валюта");
            }
        }

        public static string GetPriceFormat(this decimal price)
        {
            string currencySymbol = "₽";

            return $"{price.ToString("0.##")} {currencySymbol}";
        }
    }
}
