using AdminTgBot.Infrastructure.Models;
using Database;
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

namespace AdminTgBot.Infrastructure.Conversations.Administration
{
	internal class AdministrationConversation : IConversation
	{
		private ApplicationContext _dataSource;
		private readonly AdminBotStateManager _stateManager;
		private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

		public AdministrationConversation()
		{
			_stateManager = null!;
			_dataSource = null!;

		}

		public AdministrationConversation(AdminBotStateManager statesManager)
		{
			_stateManager = statesManager;
			_dataSource = null!;
		}

		public async Task<Trigger?> TryNextStepAsync(ApplicationContext dataSource, Message message)
		{
			_dataSource = dataSource;

			switch (_stateManager.CurrentState)
			{
				case State.CommandAdministration:
					{
						if (message.Text == MessagesText.CommandAdministration)
						{
							await WelcomeAsync();
							return Trigger.Ignore;
						}
						break;
					}
			}

			return null;
		}

		public async Task<Trigger?> TryNextStepAsync(ApplicationContext dataSource, CallbackQuery query)
		{
			_dataSource = dataSource;

			JObject data = JObject.Parse(query.Data!);
			Command command = data.GetEnumValue<Command>("Cmd");

			switch (_stateManager.CurrentState)
			{
				case State.CommandAdministration:
					{
						switch(command)
						{
							case Command.AdministrationRigths:
								{
									await ShowRigthsMenuAsync();
									return Trigger.Ignore;
								}
						}
						break;
					}
			}

			return null;
		}

		private async Task ShowRigthsMenuAsync()
		{
			InlineKeyboardButton[][] buttons = GetRigthsMenuButtons();
		}

		private InlineKeyboardButton[][] GetRigthsMenuButtons()
		{
			InlineKeyboardButton[][] result =
			[
				[
					new InlineKeyboardButton(AdministrationText.RightGroups)
					{
						CallbackData = JsonConvert.SerializeObject(new
						{
							Cmd = Command.AdministrationRightGroups,
						})
					},
					new InlineKeyboardButton(AdministrationText.UserRights)
					{
						CallbackData = JsonConvert.SerializeObject(new
						{
							Cmd = Command.AdministrationUserRights,
						})
					},
				]
			];

			return result;
		}

		private async Task WelcomeAsync()
		{
			InlineKeyboardButton[][] buttons = GetMenuButtons();

			await _stateManager.SendMessageAsync(AdministrationText.Welcome);
		}

		private InlineKeyboardButton[][] GetMenuButtons()
		{
			InlineKeyboardButton[][] result =
			[
				[
					new InlineKeyboardButton(AdministrationText.Rights)
					{
						CallbackData = JsonConvert.SerializeObject(new
						{
							Cmd = Command.AdministrationRigths,
						})
					},
					new InlineKeyboardButton(AdministrationText.Users)
					{
						CallbackData = JsonConvert.SerializeObject(new
						{
							Cmd = Command.AdministrationUsers,
						})
					},
				]
			];

			return result;
		}
	}
}
