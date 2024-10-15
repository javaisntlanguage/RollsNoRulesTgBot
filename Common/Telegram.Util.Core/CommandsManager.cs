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
		private readonly IStateManagerFactory<StateManager> _stateManagerFactory;

		protected CommandsManager(ITelegramBotClient botClient, 
			IMenuHandler menuHandler,
			IStateManagerFactory<StateManager> stateManagerFactory)
		{
			_botClient = botClient;
			_menuHandler = menuHandler;
			_stateManagerFactory = stateManagerFactory;
			_stateManagers = new Dictionary<long, StateManager>();
		}



		/// <summary>
		/// назначить состояние пользователю
		/// </summary>
		/// <param name="userId"></param>
		/// <returns></returns>
		protected void InitStateManagerIfNotExists(long chatId)
		{
			if (!_stateManagers.ContainsKey(chatId))
			{
				_stateManagers[chatId] = _stateManagerFactory.Create(chatId);
			}
		}

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
			InitStateManagerIfNotExists(chatId);

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
			InitStateManagerIfNotExists(chatId);

			return await _stateManagers[chatId].NextStateAsync(query);
		}
	}
}
