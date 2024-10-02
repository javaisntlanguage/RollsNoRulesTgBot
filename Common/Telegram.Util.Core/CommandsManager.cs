using Database;
using Helper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Util.Core.Enums;
using Telegram.Util.Core.Interfaces;

namespace Telegram.Util.Core
{
    public abstract class CommandsManager : ICommandsManager
	{
		protected ITelegramBotClient _botClient;
		protected Dictionary<long, StateManager> _stateManagers;
		protected IMenuHandler _menuHandler;

		protected CommandsManager(ITelegramBotClient botClient, 
			IMenuHandler menuHandler)
		{
			_botClient = botClient;
			_menuHandler = menuHandler;
		}



		/// <summary>
		/// назначить состояние пользователю
		/// </summary>
		/// <param name="userId"></param>
		/// <returns></returns>
		protected abstract Task InitStateManagerIfNotExistsAsync(long chatId);

		/// <summary>
		/// парсинг команды от пользователя
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		protected IBotCommandHandler? GetCommand(Message message)
		{
			long userId = message.From!.Id;
			IBotCommandHandler? command = null;

			command = _menuHandler.Commands
				.FirstOrDefault(x =>
					x.Command == message.Text);
			return command;
		}

		/// <summary>
		/// обработка сообщения от пользователя
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		public async Task<bool> ProcessMessageAsync(Message message)
		{
			long chatId = message.Chat.Id;
			await InitStateManagerIfNotExistsAsync(chatId);

			IBotCommandHandler? command = GetCommand(message);

			if (command != null)
			{
				await command.StartCommandAsync(_stateManagers[chatId], message);
				return true;
			}

			return await _stateManagers[chatId].NextStateAsync(message);
		}

		/// <summary>
		/// обработка запроса от пользователя
		/// </summary>
		/// <param name="query"></param>
		/// <returns></returns>
		public async Task<bool> ProcessQueryAsync(CallbackQuery query)
		{
			long chatId = query.Message!.Chat.Id;
			await InitStateManagerIfNotExistsAsync(chatId);

			return await _stateManagers[chatId].NextStateAsync(query);
		}
	}
}
