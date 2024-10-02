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
using Telegram.Util.Core;
using AdminTgBot.Infrastructure.Conversations.Orders.Models;
using NLog.Filters;
using AdminTgBot.Infrastructure.Conversations.Administration.Models;
using User = Database.Tables.User;
using Database.Tables;
using AdminTgBot.Infrastructure.Conversations.Orders;
using MessageContracts;
using Microsoft.EntityFrameworkCore;
using Azure;
using Telegram.Util.Core.Extensions;

namespace AdminTgBot.Infrastructure.Conversations.Administration
{
    internal class AdministrationConversation : IConversation
	{
		private ApplicationContext _dataSource;
		private readonly AdminBotStateManager _stateManager;
		private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
		private const int ADMINS_BY_PAGE = 5;
		private const int GROUPS_BY_PAGE = 5;

		public AdminsFilterModel AdminsFilter { get; set; }
		public int AdminId { get; set; }

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
							case Command.AdministrationUsers:
								{
									await ShowAdminListAsync();
									return Trigger.Ignore;
								}
							case Command.BackToAdministration:
								{
									await WelcomeAsync();
									return Trigger.Ignore;
								}
							case Command.MovePaginationAdminsSearch:
								{
									int page = data["Page"]!.Value<int>();
									await ShowAdminListAsync(page);
									return Trigger.Ignore;
								}
							case Command.AdminDetails:
							case Command.BackToAdminManaging:
								{
									int adminId = data["AdminId"]!.Value<int>();
									await ShowAdminDetailsAsync(adminId);
									return Trigger.Ignore;
								}
							case Command.AdminPermissionGroups:
								{
									await ShowAdminPermissionGroupsAsync();
									return Trigger.Ignore;
								}
							case Command.MovePaginationAdminGorups:
								{
									int page = data["Page"]!.Value<int>();
									await ShowAdminPermissionGroupsAsync(page);
									return Trigger.Ignore;
								}
							case Command.SwitchGroupForAdmin:
								{
									Guid groupId = (Guid)data["G"]!;
									int page = data["P"]!.Value<int>();
									await SwitchGroupForAdminAsync(groupId, page);
									return Trigger.Ignore;
								}
							case Command.AdminGroupDetails:
								{
									Guid groupId = (Guid)data["G"]!;
									await ShowAdminGroupDetailsAsync(groupId);
									return Trigger.Ignore;
								}
						}
						break;
					}
			}

			return null;
		}

		private async Task ShowAdminGroupDetailsAsync(Guid groupId)
		{
			IQueryable<RightsInGroup> rights = _dataSource.RightInGroups
				.Where(rg => rg.GroupId == groupId);
		}

		private async Task SwitchGroupForAdminAsync(Guid groupId, int page)
		{
			AdminsInGroup? adminInGroup = await _dataSource.AdminInGroups
					.FirstOrDefaultAsync(ag => ag.AdminId == AdminId && ag.GroupId == groupId);

			if (adminInGroup == null)
			{
				AdminsInGroup adminInGroupNew = new AdminsInGroup()
				{
					AdminId = AdminId,
					GroupId = groupId,
				};

				await _dataSource.AdminInGroups.AddAsync(adminInGroupNew);
			}
            else
            {
				_dataSource.AdminInGroups.Remove(adminInGroup);
			}

			await _dataSource.SaveChangesAsync();
			await ShowAdminPermissionGroupsAsync(page);
		}

		private async Task ShowAdminPermissionGroupsAsync(int page=1)
		{
			IQueryable<GroupView> groups = _dataSource.RightGroups
				.Skip((page - 1) * GROUPS_BY_PAGE)
				.Take(GROUPS_BY_PAGE)
				.Include(g => g.AdminsInGroup)
				.Select(g => new GroupView
				{
					Id = g.Id,
					Name = g.Name,
					IsInGroup = g.AdminsInGroup!.Any(ag => ag.AdminId == AdminId),
				});

			int groupsCount = _dataSource.RightGroups.Count();

			InlineKeyboardMarkup markup = GetAdminGroupsButtons(groups, groupsCount, page);

			await _stateManager.SendMessageAsync(AdministrationText.AdminGroupManaging, replyMarkup: markup);
		}

		private InlineKeyboardMarkup GetAdminGroupsButtons(IQueryable<GroupView> groupsView, int groupsCount, int page)
		{
			List<InlineKeyboardButton[]> keyboard = new List<InlineKeyboardButton[]>();

			if (groupsView.Any())
			{
				keyboard = groupsView
					.AsEnumerable()
					.Select(group =>
					{
						InlineKeyboardButton[] row = new InlineKeyboardButton[]
						{
								new InlineKeyboardButton(group.Name)
								{
									CallbackData = JsonConvert.SerializeObject(new
									{
										Cmd = Command.AdminGroupDetails,
										G = group.Id,
									})
								},

						}
						.Union([GetSwitchGroupButton(group, page)])
						.ToArray();

						return row;
					})
					.ToList();

				InlineKeyboardButton[] pagination = TelegramHelper.GetPagination(page, groupsCount, GROUPS_BY_PAGE, Command.MovePaginationAdminGorups);
				keyboard.Add(pagination);
			}
			else
			{
				keyboard = 
					[
						[ 
							new(MessagesText.Empty)
							{
								CallbackData = JsonConvert.SerializeObject(new
								{
									Cmd = Command.Ignore,
								})
							}
						]
					];
			}

			keyboard.Add(
			[
				new InlineKeyboardButton(AdministrationText.Back)
				{
					CallbackData = JsonConvert.SerializeObject(new
					{
						Cmd = Command.BackToAdminManaging,
						AdminId = AdminId
					})
				}
			]);

			InlineKeyboardMarkup result = new InlineKeyboardMarkup(keyboard); 
			return result;
		}

		private InlineKeyboardButton GetSwitchGroupButton(GroupView groupView, int page)
		{
			string text = groupView.IsInGroup ? AdministrationText.GroupTurnOff : AdministrationText.GroupTurnOn;
			bool groupMode = !groupView.IsInGroup;
			
			InlineKeyboardButton result = new(text)
			{
				CallbackData = JsonConvert.SerializeObject(new
				{
					Cmd = Command.SwitchGroupForAdmin,
					G = groupView.Id,
					P = page
				})
			};

			return result;
		}

		private async Task ShowAdminDetailsAsync(int adminId)
		{
			AdminCredential? admin = _dataSource.AdminCredentials.FirstOrDefault(ac => ac.Id == adminId);

			if (!await CheckAdminAsync(admin, adminId))
			{
				return;
			}

			AdminId = adminId;

			int groupsCount = _dataSource.AdminInGroups
				.Where(ap => ap.AdminId == adminId).Count();
			int rigthsCount = _dataSource.AdminRights
				.Where(ap => ap.AdminId == adminId).Count();

			string text = string.Format(AdministrationText.AdminDetails,
				admin!.Login,
				admin.Name,
				groupsCount,
				rigthsCount);

			InlineKeyboardMarkup markup = GetAdminDetailsButtons(adminId);
			await _stateManager.SendMessageAsync(text, replyMarkup: markup);
		}

		private InlineKeyboardMarkup GetAdminDetailsButtons(int adminId)
		{
			InlineKeyboardButton[] keyboard =
			[
				new InlineKeyboardButton(AdministrationText.PermissionGroups)
				{
					CallbackData = JsonConvert.SerializeObject(new
					{
						Cmd = Command.AdminPermissionGroups,
					})
				},
				new InlineKeyboardButton(AdministrationText.Permissions)
				{
					CallbackData = JsonConvert.SerializeObject(new
					{
						Cmd = Command.AdminPermissions,
					})
				},
			];

			InlineKeyboardMarkup result = new InlineKeyboardMarkup(keyboard);
			return result;
		}

		private async Task<bool> CheckAdminAsync(AdminCredential? admin, int adminId)
		{
			if (admin.IsNull())
			{
				string errorText = string.Format(AdministrationText.AdminNotFound, adminId);

				await _stateManager.SendMessageAsync(errorText, isOutOfQueue: true);
				_logger.Error(errorText);

				return false;
			}

			return true;
		}

		private async Task ShowAdminListAsync(int page = 1)
		{
			if (AdminsFilter.IsNull())
			{
				AdminsFilterModel usersFilter = new AdminsFilterModel();

				AdminsFilter = usersFilter;
			}

			IQueryable<AdminCredential> admins = _dataSource.AdminCredentials.AsQueryable();

			if(AdminsFilter.Name.IsNotNullOrEmpty())
			{
				admins = admins.Where(admin => admin.Name == AdminsFilter.Name);
			}

			if(AdminsFilter.Login.IsNotNullOrEmpty())
			{
				admins = admins.Where(admin => admin.Login == AdminsFilter.Login);
			}

			IEnumerable<AdminCredential> pageAdmins = admins
						.Skip((page - 1) * ADMINS_BY_PAGE)
						.Take(ADMINS_BY_PAGE)
						.ToList();

			InlineKeyboardMarkup markup = GetAdminsSearchButtons(pageAdmins, page, admins);
			await _stateManager.SendMessageAsync(AdministrationText.ShowAdminList, replyMarkup: markup);
		}

		private InlineKeyboardMarkup GetAdminsSearchButtons(IEnumerable<AdminCredential> pageAdmins, int page, IQueryable<AdminCredential> admins)
		{
			List<IEnumerable<InlineKeyboardButton>>? keyboard = null;

			if (pageAdmins.Any())
			{
				keyboard = pageAdmins
					.Select(ac => new InlineKeyboardButton[]
					{
						new InlineKeyboardButton(string.Format(AdministrationText.AdminPreview, ac.Login, ac.Name))
						{
							CallbackData = JsonConvert.SerializeObject(new
							{
								Cmd = Command.AdminDetails,
								AdminId = ac.Id,
							})
						}
					}.AsEnumerable())
					.ToList();

				IEnumerable<InlineKeyboardButton> pagination = GetAdminsSearchPaginationButtons(page, admins);
				keyboard.Add(pagination);
			}
			else
			{
				keyboard = new List<IEnumerable<InlineKeyboardButton>>
				{
					new InlineKeyboardButton[]
					{
						new InlineKeyboardButton(AdministrationText.Empty)
						{
							CallbackData = JsonConvert.SerializeObject(new
							{
								Cmd = Command.Ignore
							})
						}
					}
				};
			}
			 
			keyboard.Add(
			[
				new InlineKeyboardButton(AdministrationText.Back)
				{
					CallbackData = JsonConvert.SerializeObject(new
					{
						Cmd = Command.BackToAdministration
					})
				}
			]);


			InlineKeyboardMarkup result = new InlineKeyboardMarkup(keyboard);
			return result;
		}

		private IEnumerable<InlineKeyboardButton> GetAdminsSearchPaginationButtons(int page, IQueryable<AdminCredential> admins)
		{
			List<InlineKeyboardButton> result = new List<InlineKeyboardButton>();

			if(page == 1)
			{
				result.Add(new InlineKeyboardButton(MessagesText.NoPagination)
				{
					CallbackData = JsonConvert.SerializeObject(new
					{
						Cmd = Command.Ignore
					})
				});
			}
			else
			{
				result.Add(new InlineKeyboardButton(MessagesText.PaginationPrevious)
				{
					CallbackData = JsonConvert.SerializeObject(new
					{
						Cmd = Command.MovePaginationAdminsSearch,
						Page = page - 1,
					})
				});
			}

			if(admins.Count() / ADMINS_BY_PAGE > page)
			{
				result.Add(new InlineKeyboardButton(MessagesText.PaginationNext)
				{
					CallbackData = JsonConvert.SerializeObject(new
					{
						Cmd = Command.MovePaginationAdminsSearch,
						Page = page + 1,
					})
				});
			}
			else
			{
				result.Add(new InlineKeyboardButton(MessagesText.NoPagination)
				{
					CallbackData = JsonConvert.SerializeObject(new
					{
						Cmd = Command.Ignore
					})
				});
			}

			return result;
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
			await _stateManager.SendMessageAsync(AdministrationText.Welcome, replyMarkup: buttons.ToInlineMarkup());
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
