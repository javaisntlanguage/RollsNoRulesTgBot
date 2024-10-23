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
using Microsoft.EntityFrameworkCore;
using Database.Classes;

namespace AdminTgBot.Infrastructure.Conversations.Lkk
{
	internal class LkkConversation : IConversation
	{
		private ApplicationContext _dataSource;
		private readonly AdminBotStateManager _stateManager;
		private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

		public AdminCredential? NewAdminCredential { get; set; }
		public string? NewPassword { get; set; }

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
				case State.LkkEnterName:
					{
						return await EditNameAsync(message.Text);
					}
				case State.LkkEnterOldPassword:
					{
						return await SuggestEnterNewPasswordAsync(message);
					}
				case State.LkkEnterNewPassword:
					{
						return await SuggestConfirmNewPasswordAsync(message);
					}
				case State.LkkConfirmNewPassword:
					{
						return await ConfirmNewPasswordAsync(message);
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
				case State.CommandLkk:
					{
						switch (command)
						{
							case Command.EditName:
								{
									await SuggestEditNameAsync();
									return Trigger.SuggestEditName;
								}
							case Command.EditPassword:
								{
									await SuggestEditPasswordAsync();
									return Trigger.SuggestEditPassword;
								}
						}
						break;
					}
			}

			return null;
		}

		private async Task<Trigger> ConfirmNewPasswordAsync(Message message)
		{
			await _stateManager.DeleteMessageAsync(message.MessageId);

			if (NewPassword == null)
			{
				await _stateManager.SendMessageAsync(LkkConversationText.NewPasswordLost);
				await WelcomeAsync();
				return Trigger.Ignore;
			}

			string? messageText = message.Text;
			_stateManager.CheckText(messageText);

			if(messageText != NewPassword)
			{
				await _stateManager.SendMessageAsync(LkkConversationText.ConfirmNewPasswordNotSame);
				return Trigger.Ignore;
			}

			AdminCredential? admin = GetAdmin();
			admin.SetPassword(messageText);

			_dataSource.Update(admin);
			await _dataSource.SaveChangesAsync();

			await _stateManager.SendMessageAsync(LkkConversationText.EditPasswordSuccess);
			await WelcomeAsync();

			return Trigger.BackToLkk;
		}

		private async Task<Trigger> SuggestConfirmNewPasswordAsync(Message message)
		{
			await _stateManager.DeleteMessageAsync(message.MessageId);

			string? messageText = message.Text;
			_stateManager.CheckTextAndLength(messageText, AdminCredential.PASSWORD_MIN_LENGTH, AdminCredential.PASSWORD_MAX_LENGTH);

			AdminCredential? admin = GetAdmin();
			if(DBHelper.GetPasswordHash(messageText!) == admin.PasswordHash)
			{
				await _stateManager.SendMessageAsync(LkkConversationText.SamePasswordError);
				return Trigger.Ignore;
			}

			NewPassword = messageText;

			await _stateManager.SendMessageAsync(LkkConversationText.ConfirmNewPassword);
			return Trigger.SuggestConfirmNewAdminPassword;
		}

		private async Task<Trigger?> SuggestEnterNewPasswordAsync(Message message)
		{
			await _stateManager.DeleteMessageAsync(message.MessageId);

			string? messageText = message.Text;
			_stateManager.CheckText(messageText);

			AdminCredential? admin = GetAdmin();
			if (admin.PasswordHash != DBHelper.GetPasswordHash(messageText!))
			{ 
				InlineKeyboardButton button = GetForgotPasswordButton();
				InlineKeyboardMarkup markup = new InlineKeyboardMarkup(button);

				await _stateManager.SendMessageAsync(LkkConversationText.WrongOldPassword, markup: markup);
				return Trigger.Ignore;
			}

			await _stateManager.SendMessageAsync(LkkConversationText.EnterNewPassword);

			return Trigger.EnterNewPassword;
		}

		private async Task SuggestEditPasswordAsync()
		{
			InlineKeyboardButton button = GetForgotPasswordButton();
			InlineKeyboardMarkup markup = new InlineKeyboardMarkup(button);

			await _stateManager.SendMessageAsync(LkkConversationText.SuggestEditPassword, markup: markup);
		}

		private InlineKeyboardButton GetForgotPasswordButton()
		{
			InlineKeyboardButton result = new InlineKeyboardButton(LkkConversationText.EditPasswordForgot)
			{
				CallbackData = JsonConvert.SerializeObject(new
				{
					Cmd = Command.ForgotPassword,
				})
			};

			return result;
		}

		private async Task<Trigger?> EditNameAsync(string? messageText)
		{
			AdminCredential? admin = GetAdmin();

			_stateManager.CheckTextAndLength(messageText, AdminCredential.NAME_MIN_LENGTH, AdminCredential.NAME_MAX_LENGTH);

			if (messageText == admin.Name)
			{
				await _stateManager.SendMessageAsync(LkkConversationText.SameNameError);
				return Trigger.Ignore;
			}

			admin.Name = messageText!;

			_dataSource.AdminCredentials.Update(admin);
			await _dataSource.SaveChangesAsync();

			await _stateManager.SendMessageAsync(LkkConversationText.EditNameSuccess);
			await WelcomeAsync();

			return Trigger.BackToLkk;
		}

		private async Task SuggestEditNameAsync()
		{
			string name = GetAdmin().Name;
			string text = string.Format(LkkConversationText.SuggestEditName, name);

			await _stateManager.SendMessageAsync(text);
		}

		private async Task WelcomeAsync()
		{
			string name = GetAdmin().Name;
			string text = string.Format(LkkConversationText.Welcome, name);

			InlineKeyboardButton[][] buttons = GetWelcomeButtons();
			InlineKeyboardMarkup markup = new InlineKeyboardMarkup(buttons);

			await _stateManager.SendMessageAsync(text, markup: markup);
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

		private AdminCredential GetAdmin()
		{
			AdminCredential? admin = _dataSource.AdminCredentials.FirstOrDefault(ac => ac.Id == _stateManager.AdminId);

			if (admin == null)
			{
				throw new Exception($"Не удалось найти админа. id={_stateManager.AdminId}");
			}

			return admin;
		}
	}
}
