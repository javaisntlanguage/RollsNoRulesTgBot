using AdminTgBot.Infrastructure.Conversations.BotOwner;
using AdminTgBot.Infrastructure.Models;
using Database;
using Database.Tables;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Helper;
using Telegram.Bot.Types.ReplyMarkups;
using Newtonsoft.Json;

namespace AdminTgBot.Infrastructure.Conversations.Lkk
{
	internal class LkkConversation : IConversation
	{
		private ApplicationContext _dataSource;
		private readonly AdminBotStateManager _stateManager;
		private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

		public AdminCredential? NewAdminCredential { get; set; }

		public LkkConversation()
		{
			_stateManager = null!;
			_dataSource = null!;

		}

		public LkkConversation(AdminBotStateManager statesManager)
		{
			_stateManager = statesManager;
			_dataSource = null!;
		}

		public async Task<Trigger?> TryNextStepAsync(ApplicationContext dataSource, Message message)
		{
			_dataSource = dataSource;

			switch (_stateManager.CurrentState)
			{
				case State.CommandLkk:
					{
						if (message.Text == MessagesText.CommandLkk)
						{
							await WelcomeAsync();
							return Trigger.Ignore;
						}
						break;
					}
			}

			return null!;
		}

		public async Task<Trigger?> TryNextStepAsync(ApplicationContext dataSource, CallbackQuery query)
		{
			_dataSource = dataSource;

			JObject data = JObject.Parse(query.Data!);
			Command command = data.GetEnumValue<Command>("Cmd");

			switch (_stateManager.CurrentState)
			{
				
			}

			return null;
		}

		private async Task WelcomeAsync()
		{
			InlineKeyboardButton[][] buttons = GetWelcomeButtons();
			InlineKeyboardMarkup markup = new InlineKeyboardMarkup(buttons);

			await _stateManager.SendMessageAsync(LkkConversationText.Welcome, replyMarkup: markup);
		}

		private InlineKeyboardButton[][] GetWelcomeButtons()
		{
			InlineKeyboardButton[][] result =
			[
				[
					new InlineKeyboardButton(LkkConversationText.EditName)
					{
						CallbackData = JsonConvert.SerializeObject(new
							{
								Cmd = Command.EditName,
							})
					},
				],
				[
					new InlineKeyboardButton(LkkConversationText.EditPassword)
					{
						CallbackData = JsonConvert.SerializeObject(new
							{
								Cmd = Command.EditPassword,
							})
					},
				]
			];

			return result;
		}
	}
}
