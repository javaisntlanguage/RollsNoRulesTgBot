using System.Net;
using WebDav;

namespace Robox.Telegram.Util.Core
{
    public static class TelegramHelper
    {

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
