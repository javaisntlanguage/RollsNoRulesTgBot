using AdminTgBot.Infrastructure.Conversations.CatalogEditor.Models;
using AdminTgBot.Infrastructure.Models;
using Database;
using Database.Classes;
using Database.Tables;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Helper;
using NLog;
using Microsoft.EntityFrameworkCore;

namespace AdminTgBot.Infrastructure.Conversations.BotOwner
{
	internal class BotOwnerConversation : IConversation
	{
		private ApplicationContext _dataSource;
		private readonly AdminBotStateManager _stateManager;
		private readonly AdminSettings _config;
		private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

		public AdminCredential? NewAdminCredential { get; set; }

		public BotOwnerConversation()
		{
			_stateManager = null!;
			_dataSource = null!;
			_config = null!;
		}

		public BotOwnerConversation(AdminBotStateManager statesManager, AdminSettings config)
		{
			_stateManager = statesManager;
			_config = config;
			_dataSource = null!;
		}

		public async Task<Trigger?> TryNextStepAsync(ApplicationContext dataSource, Message message)
		{
			_dataSource = dataSource;

			switch (_stateManager.CurrentState)
			{
				case State.CommandBotOwner:
					{
						if(message.Text == _config.TelegramBotToken)
						{
							await _stateManager.SendMessageAsync(BotOwnerText.Welcome);
							return Trigger.SelectMenu;
						}
						break;
					}
				case State.BotOwnerSelectMenu:
					{
						return await SelectMenuAsync();
					}
				case State.BotOwnerEnterLogin:
					{
						return await SetLoginAndSuggestEnterPassword(message.Text);
					}
				case State.BotOwnerEnterPassword:
					{
						return await SetPasswordAndSuggestEnterName(message.Text);
					}
				case State.BotOwnerEnterAdminName:
					{
						await SetNameAndCreateAdmin(message.Text);
						return Trigger.Ignore;
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
				case State.BotOwnerEnterLogin:
					{
						switch(command)
						{
							case Command.BackToMenuActions:
								{
									return Trigger.SelectMenu;
								}
						}
						break;
					}
				case State.BotOwnerEnterPassword:
					{
						switch(command)
						{
							case Command.BackToMenuActions:
								{
									return Trigger.SelectMenu;
								}
						}
						break;
					}
				case State.BotOwnerEnterAdminName:
					{
						switch(command)
						{
							case Command.BackToMenuActions:
								{
									return Trigger.SelectMenu;
								}
							case Command.AddSuperAdminConfirm:
								{
									await AddSuperAdminAsync();
									return Trigger.SelectMenu;
								}
						}
						break;
					}
				case State.BotOwnerSelectMenu:
					{
						switch (command)
						{
							case Command.BackToMenuActions:
								{
									return Trigger.SelectMenu;
								}
							case Command.AddSuperAdmin:
								{
									return await SuggestCreateNewSuperAdminAsync();
								}
							case Command.ResetPasswordSuperAdmin:
								{
									return await ChooseSuperAdminForResetPasswordAsync();
								}
							case Command.ResetPasswordSuperAdminConcrete:
								{
									int adminId = data["AdminId"]!.Value<int>();
									await ResetSuperAdminPasswordAsync(adminId);
									return Trigger.SelectMenu;
								}
						}
						break;
					}
			}

			return null;
		}

		private async Task ResetSuperAdminPasswordAsync(int adminId)
		{
			AdminCredential? admin = _dataSource.AdminCredentials.FirstOrDefault(ac => ac.Id == adminId);

			if (admin == null)
			{
				string errorText = string.Format(BotOwnerText.SuperAdminNotFound, adminId);
				await _stateManager.SendMessageAsync(errorText);
				_logger.Error(errorText);

				return;
			}

			RandomSeq randomSeq = new RandomSeq();
			string newPassword = randomSeq.GenString(12);
			string text = string.Format(BotOwnerText.NewPasswordReset, newPassword);

			admin.SetPassword(newPassword);
			_dataSource.AdminCredentials.Update(admin);
			await _dataSource.SaveChangesAsync();

			await _stateManager.SendMessageAsync(text);
		}

		private async Task<Trigger?> ChooseSuperAdminForResetPasswordAsync()
		{
			IEnumerable<AdminCredential> superAdmins = _dataSource.AdminRights
				.Where(ap => ap.RightId == RightHelper.SuperAdmin)
				.Include(ap => ap.Admin)
				.Select(ap => ap.Admin!)
				.ToArray();

			if(!superAdmins.Any()) 
			{
				await _stateManager.SendMessageAsync(BotOwnerText.SuperAdminsNotFound);
				return Trigger.SelectMenu;
			}
			else if(superAdmins.Count() == 1)
			{
				await ResetSuperAdminPasswordAsync(superAdmins.First().Id);
				return Trigger.SelectMenu;
			}

			InlineKeyboardButton[][] adminButtons = GetResetSuperAdminPasswordButtons(superAdmins);
			InlineKeyboardMarkup markup = new(adminButtons);

			await _stateManager.SendMessageAsync(BotOwnerText.ChooseSuperAdminForResetPassowrd, markup: markup);
			return Trigger.Ignore;
		}

		private InlineKeyboardButton[][] GetResetSuperAdminPasswordButtons(IEnumerable<AdminCredential> superAdmins)
		{
			InlineKeyboardButton[][] result = superAdmins
				.Select(sa => new InlineKeyboardButton[]
				{
					new InlineKeyboardButton(sa.Name)
					{
						CallbackData = JsonConvert.SerializeObject(new
						{
							Cmd = Command.ResetPasswordSuperAdminConcrete,
							AdminId = sa.Id,
						})
					}
				})
				.Union(new InlineKeyboardButton[][]
				{
					new InlineKeyboardButton[]
					{
						new InlineKeyboardButton(BotOwnerText.BackToMenuActions)
						{
							CallbackData = JsonConvert.SerializeObject(new
							{
								Cmd = Command.BackToMenuActions,
							})
						} 
					}
				})
				.ToArray();

			return result;
		}

		private async Task AddSuperAdminAsync()
		{
			if(NewAdminCredential == null)
			{
				throw new Exception("Неожиданно NewAdminCredential=null при создании супер админа");
			}

			try
			{
				await _dataSource.AddAsync(NewAdminCredential);
				await _dataSource.SaveChangesAsync();

				AdminRight adminPermission = new()
				{
					RightId = RightHelper.SuperAdmin,
					AdminId = NewAdminCredential.Id,
				};

				await _dataSource.AddAsync(adminPermission);
				await _dataSource.SaveChangesAsync();

				await _stateManager.SendMessageAsync(BotOwnerText.AddSuperAdminSuccess);
			}
			catch (Exception ex)
			{
				_logger.Error(ex);
				await _stateManager.SendMessageAsync(BotOwnerText.AddSuperAdminError);
			}
		}

		private async Task SetNameAndCreateAdmin(string? messageText)
		{
			if (NewAdminCredential == null)
			{
				throw new Exception("Неожиданно NewAdminCredential=null при вводе имени");
			}

			_stateManager.CheckText(messageText);

			InlineKeyboardMarkup? markup = GetBackToMenuActionsButton();

			if (messageText!.Length < AdminCredential.NAME_MIN_LENGTH)
			{
				string errorText = string.Format(MessagesText.ValueTooShort, AdminCredential.NAME_MIN_LENGTH);
				await _stateManager.SendMessageAsync(errorText, markup: markup);
			}

			if (messageText!.Length > AdminCredential.NAME_MAX_LENGTH)
			{
				string errorText = string.Format(MessagesText.ValueTooLong, AdminCredential.NAME_MAX_LENGTH);
				await _stateManager.SendMessageAsync(errorText, markup: markup);
			}

			NewAdminCredential.Name = messageText;

			string text = string.Format(BotOwnerText.ConfirmAddSuperAdmin, NewAdminCredential.Login, NewAdminCredential.Name);
			InlineKeyboardButton[] confirmButtons = GetConfirmAddSuperAdminButtons();
			markup = new InlineKeyboardMarkup(confirmButtons);

			await _stateManager.SendMessageAsync(text, markup: markup);
		}

		private InlineKeyboardButton[] GetConfirmAddSuperAdminButtons()
		{
			InlineKeyboardButton[] result =
				[
					new InlineKeyboardButton(MessagesText.Yes)
					{
						CallbackData = JsonConvert.SerializeObject(new
						{
							Cmd = Command.AddSuperAdminConfirm,
						})
					},
					new InlineKeyboardButton(MessagesText.No)
					{
						CallbackData = JsonConvert.SerializeObject(new
						{
							Cmd = Command.BackToMenuActions,
						})
					},
				];

			return result;
		}

		private async Task<Trigger?> SetPasswordAndSuggestEnterName(string? messageText)
		{
			if(NewAdminCredential == null)
			{
				throw new Exception("Неожиданно NewAdminCredential=null при вводе пароля");
			}

			_stateManager.CheckText(messageText);

			InlineKeyboardMarkup? markup = GetBackToMenuActionsButton();

			if (messageText!.Length < AdminCredential.PASSWORD_MIN_LENGTH)
			{
				string errorText = string.Format(MessagesText.ValueTooShort, AdminCredential.PASSWORD_MIN_LENGTH);
				await _stateManager.SendMessageAsync(errorText, markup: markup);
				return Trigger.Ignore;
			}

			NewAdminCredential.SetPassword(messageText!);

			await _stateManager.SendMessageAsync(BotOwnerText.EnterName, markup: markup);

			return Trigger.EnterAdminName;
		}

		private async Task<Trigger?> SetLoginAndSuggestEnterPassword(string? messageText)
		{
			_stateManager.CheckText(messageText);

			InlineKeyboardMarkup? markup = GetBackToMenuActionsButton();

			if(messageText!.Length > AdminCredential.LOGIN_MAX_LENGTH)
			{
				string errorText = string.Format(MessagesText.ValueTooLong, AdminCredential.LOGIN_MAX_LENGTH);
				await _stateManager.SendMessageAsync(errorText, markup: markup);
				return Trigger.Ignore;
			}

			if(messageText.Length < AdminCredential.LOGIN_MIN_LENGTH)
			{
				string errorText = string.Format(MessagesText.ValueTooShort, AdminCredential.LOGIN_MIN_LENGTH);
				await _stateManager.SendMessageAsync(errorText, markup: markup);
				return Trigger.Ignore;
			}

			if(_dataSource.AdminCredentials.Any(ac => ac.Login == messageText))
			{
				string errorText = string.Format(BotOwnerText.LoginAlreadyExists, AdminCredential.LOGIN_MAX_LENGTH);
				await _stateManager.SendMessageAsync(errorText, markup: markup);
				return Trigger.Ignore;
			}

			NewAdminCredential = new AdminCredential();
			NewAdminCredential.Login = messageText;

			await _stateManager.SendMessageAsync(BotOwnerText.EnterPassword, markup: markup);

			return Trigger.EnterPassword;
		}

		private async Task<Trigger> SelectMenuAsync()
		{
			bool hasSuperAdmins = _dataSource.AdminRights.Any(ap => ap.RightId == RightHelper.SuperAdmin);

			if (!hasSuperAdmins)
			{
				return await SuggestCreateNewSuperAdminAsync();
			}

			InlineKeyboardButton[][] actions = GetMenuActions();
			InlineKeyboardMarkup markup = new InlineKeyboardMarkup(actions);

			await _stateManager.SendMessageAsync(BotOwnerText.SelectMenu, markup: markup);

			return Trigger.Ignore;
		}

		private InlineKeyboardButton[][] GetMenuActions()
		{
			InlineKeyboardButton[][] result =
				[
					[
						new InlineKeyboardButton(BotOwnerText.AddSuperAdmin)
						{
							CallbackData = JsonConvert.SerializeObject(new
							{
								Cmd = Command.AddSuperAdmin,
							})
						},
					],
					[
						new InlineKeyboardButton(BotOwnerText.ResetPassword)
						{
							CallbackData = JsonConvert.SerializeObject(new
							{
								Cmd = Command.ResetPasswordSuperAdmin,
							})
						},
					]
				];

			return result;
		}

		private async Task<Trigger> SuggestCreateNewSuperAdminAsync()
		{
			InlineKeyboardMarkup? markup = GetBackToMenuActionsButton();
			await _stateManager.SendMessageAsync(BotOwnerText.EnterLogin, markup: markup);
			return Trigger.EnterLogin;
		}

		private InlineKeyboardMarkup? GetBackToMenuActionsButton()
		{
			InlineKeyboardMarkup? result = null;

			if(_dataSource.AdminRights.Any(ap => ap.RightId == RightHelper.SuperAdmin))
			{
				InlineKeyboardButton[][] buttons = 
					[
						[
							new InlineKeyboardButton(BotOwnerText.BackToMenuActions)
							{
								CallbackData = JsonConvert.SerializeObject(new
								{
									Cmd = Command.BackToMenuActions,
								})
							}
						]
					];

				result = new InlineKeyboardMarkup(buttons);
			}

			return result;
		}
	}
}
