using Azure;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Util.Core.Enums;
using Telegram.Util.Core.Resources;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Telegram.Util.Core
{
	public class TelegramHelper
	{
		public const string PRICE_FORMAT = "0.## ₽";
		public static InlineKeyboardButton[] GetPagination<TCommand>(
			int page,
			int pagesCount,
			int pageSize,
			TCommand command) where TCommand : Enum
		{
			List<InlineKeyboardButton> result = new List<InlineKeyboardButton>();

			if (page == 1)
			{
				result.Add(new InlineKeyboardButton(Text.NoPagination)
				{
					CallbackData = JsonConvert.SerializeObject(new
					{
						Cmd = CommandsDefault.Ignore
					})
				});
			}
			else
			{
				result.Add(new InlineKeyboardButton(Text.PaginationPrevious)
				{
					CallbackData = JsonConvert.SerializeObject(new
					{
						Cmd = command,
						Page = page - 1,
					})
				});
			}

			if (pagesCount / pageSize > page)
			{
				result.Add(new InlineKeyboardButton(Text.PaginationNext)
				{
					CallbackData = JsonConvert.SerializeObject(new
					{
						Cmd = command,
						Page = page + 1,
					})
				});
			}
			else
			{
				result.Add(new InlineKeyboardButton(Text.NoPagination)
				{
					CallbackData = JsonConvert.SerializeObject(new
					{
						Cmd = CommandsDefault.Ignore
					})
				});
			}

			return result.ToArray();
		}
	}
}
