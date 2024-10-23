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
using System.Text.RegularExpressions;

namespace AdminTgBot.Infrastructure.Conversations.Administration
{
    internal class AdministrationConversation : IConversation
	{
		private ApplicationContext _dataSource;
		private readonly AdminBotStateManager _stateManager;
		private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
		private const int ADMINS_BY_PAGE = 5;
		private const int GROUPS_BY_PAGE = 5;
		private const int RIGHTS_BY_PAGE = 5;

		public AdminsFilterModel AdminsFilter { get; set; }
		public int AdminId { get; set; }
		public Guid GroupId { get; set; }
		public RightGroup? NewRightGroup { get; set; }

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
				case State.AdministrationEnterRightGroupName:
					{
						await EnterRightGroupNameAsync(message.Text);
						return Trigger.EnterRightGroupDescription;
					}
				case State.AdministrationEnterRightGroupDescription:
					{
						await EnterRightGroupDescriptionAsync(message.Text);
						await SuggestApproveAddingRightGroupAsync();
						return Trigger.BackToAdministration;
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
							case Command.AdministrationRights:
								{
									await ShowRightsMenuAsync();
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
							case Command.MovePaginationAdminGroups:
								{
									int page = data["Page"]!.Value<int>();
									await ShowAdminPermissionGroupsAsync(page);
									return Trigger.Ignore;
								}
							case Command.MovePaginationRightGroups:
								{
									int page = data["Page"]!.Value<int>();
									await ShowRightGroupsAsync(page);
									return Trigger.Ignore;
								}
							case Command.MovePaginationAdminRights:
								{
									int page = data["Page"]!.Value<int>();
									await ShowAdminRightsAsync(page);
									return Trigger.Ignore;
								}
							case Command.SwitchGroupForAdmin:
								{
									Guid groupId = (Guid)data["G"]!;
									int page = data["P"]!.Value<int>();
									await SwitchGroupForAdminAsync(groupId);
									await ShowAdminPermissionGroupsAsync(page);
									return Trigger.Ignore;
								}
							case Command.SwitchRightForAdmin:
								{
									Guid rightId = (Guid)data["R"]!;
									int page = data["P"]!.Value<int>();
									await SwitchRightForAdminAsync(rightId);
									await ShowAdminRightsAsync(page);
									return Trigger.Ignore;
								}
							case Command.SwitchRightForGroup:
								{
									Guid rightId = (Guid)data["R"]!;
									int page = data["P"]!.Value<int>();
									await SwitchRightForGroupAsync(rightId);
									await ShowGroupRightsAsync(page);
									return Trigger.Ignore;
								}
							case Command.AdminGroupDetails:
								{
									Guid groupId = (Guid)data["G"]!;
									await ShowAdminGroupDetailsAsync(groupId);
									return Trigger.Ignore;
								}
							case Command.AdminPermissions:
								{
									await ShowAdminRightsAsync();
									return Trigger.Ignore;
								}
							case Command.AdministrationRightGroups:
								{
									await ShowRightGroupsAsync();
									return Trigger.Ignore;
								}
							case Command.AddRightGroup:
								{
									await AddRightGroupNameAsync();
									return Trigger.EnterRightGroupName;
								}
							case Command.AddRightGroupApprove:
								{
									await SaveRightGroupAsync();
									await ShowRightGroupsAsync();
									return Trigger.Ignore;
								}
							case Command.RightGroupDetails:
								{
									Guid groupId = (Guid)data["GroupId"]!;
									await ShowRightGroupDetailsAsync(groupId);
									return Trigger.Ignore;
								}
							case Command.BackToRightGroups:
								{
									await ShowRightGroupsAsync();
									return Trigger.Ignore;
								}
							case Command.DeleteGroup:
								{
									await ShowDeleteGroupApproveAsync();
									return Trigger.Ignore;
								}
							case Command.DeleteGroupApprove:
								{
									await DeleteGroupApproveAsync();
									await ShowRightGroupsAsync();
									return Trigger.Ignore;
								}
							case Command.ManageGroupRights:
								{
									await ShowGroupRightsAsync();
									return Trigger.Ignore;
								}
							case Command.MovePaginationRightsInGroup:
								{
									int page = data["P"]!.Value<int>();
									await ShowGroupRightsAsync(page);
									return Trigger.Ignore;
								}
							case Command.AdminGroupRightDetails:
								{
									Guid rightId = (Guid)data["RightId"]!;
									await ShowAdminGroupRightDetailsAsync(rightId);
									return Trigger.Ignore;
								}
							case Command.RightOfGroupDetails:
								{
									Guid rightId = (Guid)data["RightId"]!;
									await ShowGroupRightDetailsAsync(rightId);
									return Trigger.Ignore;
								}
						}
						break;
					}
				case State.AdministrationEnterRightGroupName:
					{
						switch (command)
						{
							case Command.BackToRightGroups:
								{
									await ShowRightGroupsAsync();
									return Trigger.BackToAdministration;
								}
						}
						break;
					}
				case State.AdministrationEnterRightGroupDescription:
					{
						switch (command)
						{
							case Command.AddRightGroup:
								{
									await AddRightGroupNameAsync();
									return Trigger.BackToEnterRightGroupName;
								}
						}
						break;
					}
			}

			return null;
		}

		private async Task ShowGroupRightDetailsAsync(Guid rightId)
		{
			Right? right = await _dataSource.Rights.FirstOrDefaultAsync(r => r.RigthId == rightId);

			if (!await CheckRightAsync(right, rightId))
			{
				return;
			}

			InlineKeyboardMarkup markup = GetGroupRightDetailsButtons();
			string text = string.Format(AdministrationText.RightDetails, right!.Name, right.Description);
			
			await _stateManager.SendMessageAsync(text, markup);
		}

		private async Task ShowAdminGroupRightDetailsAsync(Guid rightId)
		{
			Right? right = await _dataSource.Rights.FirstOrDefaultAsync(r => r.RigthId == rightId);

			if (!await CheckRightAsync(right, rightId))
			{
				return;
			}

			InlineKeyboardMarkup markup = GetAdminGroupRightDetailsButtons();
			string text = string.Format(AdministrationText.RightDetails, right!.Name, right.Description);
			
			await _stateManager.SendMessageAsync(text, markup);
		}

		private InlineKeyboardMarkup GetGroupRightDetailsButtons()
		{
			InlineKeyboardButton keyboard = new(AdministrationText.Back)
			{
				CallbackData = JsonConvert.SerializeObject(new
				{
					Cmd = Command.ManageGroupRights,
					GroupId = GroupId
				})
			}; 

			InlineKeyboardMarkup result = new (keyboard);
			return result;
		}

		private InlineKeyboardMarkup GetAdminGroupRightDetailsButtons()
		{
			InlineKeyboardButton keyboard = new(AdministrationText.Back)
			{
				CallbackData = JsonConvert.SerializeObject(new
				{
					Cmd = Command.AdminPermissions,
				})
			}; 

			InlineKeyboardMarkup result = new (keyboard);
			return result;
		}

		private async Task<bool> CheckRightAsync(Right? right, Guid rightId)
		{
			if (right == null)
			{
				string errorText = string.Format(AdministrationText.RightNotFound, rightId);

				await _stateManager.SendMessageAsync(errorText, isOutOfQueue: true);
				_logger.Error(errorText);

				return false;
			}

			return true;
		}

		private async Task ShowDeleteGroupApproveAsync()
		{
			InlineKeyboardMarkup markup = GetDeleteGroupApproveButtons();
			await _stateManager.SendMessageAsync(AdministrationText.DeleteGroupApprove, markup);
		}

		private InlineKeyboardMarkup GetDeleteGroupApproveButtons()
		{
			InlineKeyboardButton[] keyboard =
			[
				new (MessagesText.Yes)
				{
					CallbackData = JsonConvert.SerializeObject(new
					{
						Cmd = Command.DeleteGroupApprove,
					})
				},
				new (MessagesText.No)
				{
					CallbackData = JsonConvert.SerializeObject(new
					{
						Cmd = Command.BackToRightGroups,
					})
				},
			];

			InlineKeyboardMarkup result = new (keyboard);
			return result;
		}

		private async Task DeleteGroupApproveAsync()
		{
			RightGroup? group = await _dataSource.RightGroups.FirstOrDefaultAsync(g => g.Id == GroupId);

			if (!await CheckRightGroupAsync(group, GroupId))
			{
				return;
			}

			_dataSource.RightGroups.Remove(group!);
			await _dataSource.SaveChangesAsync();

			string text = string.Format(AdministrationText.GroupDeleted, group!.Name);
			await _stateManager.SendMessageAsync(text);
		}

		private async Task ShowRightGroupDetailsAsync(Guid groupId)
		{
			RightGroup? group = await _dataSource.RightGroups
				.FirstOrDefaultAsync(g => g.Id == groupId);

			if (!await CheckRightGroupAsync(group, groupId))
			{
				return;
			}

			GroupId = groupId;

			InlineKeyboardMarkup markup = GetRightGroupDetailsButtons(groupId);
			string text = string.Format(AdministrationText.GroupRights, group!.Name, group.Description);

			await _stateManager.SendMessageAsync(text, markup: markup);
		}

		private InlineKeyboardMarkup GetRightGroupDetailsButtons(Guid groupId)
		{
			InlineKeyboardButton[][] keyboard =
			[
				[
					new (AdministrationText.DeleteGroup)
					{
						CallbackData = JsonConvert.SerializeObject(new
						{
							Cmd = Command.DeleteGroup,
						})
					}
				],
				[
					new (AdministrationText.ManageGroupRights)
					{
						CallbackData = JsonConvert.SerializeObject(new
						{
							GroupId = groupId,
							Cmd = Command.ManageGroupRights,
						})
					}
				],
				[
					new (AdministrationText.Back)
					{
						CallbackData = JsonConvert.SerializeObject(new
						{
							Cmd = Command.BackToRightGroups,
						})
					}
				],
			];

			InlineKeyboardMarkup result = new (keyboard);
			return result;
		}

		private async Task SaveRightGroupAsync()
		{
			if (NewRightGroup == null)
			{
				await _stateManager.SendMessageAsync(AdministrationText.NewRightGroupLost);
				return;
			}

			await _dataSource.RightGroups.AddAsync(NewRightGroup);
			await _dataSource.SaveChangesAsync();

			string text = string.Format(AdministrationText.RightGroupAdded, NewRightGroup.Name);
			await _stateManager.SendMessageAsync(text);
		}

		private async Task SuggestApproveAddingRightGroupAsync()
		{
			InlineKeyboardMarkup markup = GetApproveAddingRightGroupButtons();
			await _stateManager.SendMessageAsync(AdministrationText.ApproveAddingRightGroup, markup: markup);
		}

		private InlineKeyboardMarkup GetApproveAddingRightGroupButtons()
		{
			InlineKeyboardButton[] buttons =
			[
				new(MessagesText.Yes)
				{
					CallbackData = JsonConvert.SerializeObject(new
					{
						Cmd = Command.AddRightGroupApprove,
					})
				},
				new(MessagesText.No)
				{
					CallbackData = JsonConvert.SerializeObject(new
					{
						Cmd = Command.AdministrationRightGroups,
					})
				},
			];

			InlineKeyboardMarkup result = new(buttons);  
			return result;
		}

		private async Task EnterRightGroupDescriptionAsync(string? text)
		{
			_stateManager.CheckTextAndLength(text, maxLength: RightGroup.DESCRIPTION_MAX_LENGTH);

			if (NewRightGroup == null)
			{
				await _stateManager.SendMessageAsync(AdministrationText.NewRightGroupLost);
				return;
			}

			NewRightGroup.Description = text!;
		}

		private async Task EnterRightGroupNameAsync(string? text)
		{
			_stateManager.CheckTextAndLength(text,
				minLength: RightGroup.NAME_MIN_LENGTH,
				maxLength: RightGroup.NAME_MAX_LENGTH);

			NewRightGroup = new RightGroup()
			{
				Id = Guid.NewGuid(),
				Name = text!
			};

			InlineKeyboardMarkup markup = GetEnterRightGroupNameButtons();

			await _stateManager.SendMessageAsync(AdministrationText.EnterRightGroupDescription, markup: markup);
		}

		private InlineKeyboardMarkup GetEnterRightGroupNameButtons()
		{
			InlineKeyboardButton button = new(AdministrationText.BackToEnterRightGroupName)
			{
				CallbackData = JsonConvert.SerializeObject(new
				{
					Cmd = Command.AddRightGroup,
				})
			};

			InlineKeyboardMarkup result = new(button);
			return result;
		}

		private async Task AddRightGroupNameAsync()
		{
			NewRightGroup = null;

			InlineKeyboardMarkup markup = GetAddRightGroupNameButtons();
			await _stateManager.SendMessageAsync(AdministrationText.EnterRightGroupName, markup: markup);
		}

		private InlineKeyboardMarkup GetAddRightGroupNameButtons()
		{
			InlineKeyboardButton button = new InlineKeyboardButton(AdministrationText.BackFromAddRightGroupName)
			{
				CallbackData = JsonConvert.SerializeObject(new
				{
					Cmd = Command.BackToRightGroups,
				})
			};

			InlineKeyboardMarkup result = new InlineKeyboardMarkup(button);

			return result;
		}

		private async Task ShowAdminGroupDetailsAsync(Guid groupId)
		{
			var rights = _dataSource.RightInGroups
				.Where(rg => rg.GroupId == groupId)
				.Include(rg => rg.Group)
				.Include(rg => rg.AdminRight)
				.Select(rg => new
				{
					GroupName = rg.Group!.Name,
					RigthName = rg.AdminRight!.Name
				});

			string text;

			if (rights.Any())
			{
				string groupName = rights.First().GroupName;
				string rightsInGroup = string.Join(", ", rights.Select(r => r.RigthName));
				text = string.Format(AdministrationText.AdminGroupDetails, groupName, rightsInGroup);
			}
			else
			{
				text = AdministrationText.EmptyGroup;
			}

			InlineKeyboardMarkup markup = GetAdminGroupDetailsButtons(groupId);

			await _stateManager.SendMessageAsync(text, markup: markup);
		}

		private InlineKeyboardMarkup GetAdminGroupDetailsButtons(Guid groupId)
		{
			InlineKeyboardButton button = new InlineKeyboardButton(AdministrationText.Back)
			{
				CallbackData = JsonConvert.SerializeObject(new
				{
					Cmd = Command.AdminPermissionGroups,
				})
			};

			InlineKeyboardMarkup result = new InlineKeyboardMarkup(button);

			return result;
		}

		private async Task SwitchRightForGroupAsync(Guid rightId)
		{
			RightsInGroup? rightInGroup = await _dataSource.RightInGroups
					.FirstOrDefaultAsync(ag => ag.GroupId == GroupId && ag.RightId == rightId);

			if (rightInGroup == null)
			{
				RightsInGroup rightInGroupNew = new RightsInGroup()
				{
					GroupId = GroupId,
					RightId = rightId,
				};

				await _dataSource.RightInGroups.AddAsync(rightInGroupNew);
			}
			else
			{
				_dataSource.RightInGroups.Remove(rightInGroup);
			}

			await _dataSource.SaveChangesAsync();
		}

		private async Task SwitchRightForAdminAsync(Guid rightId)
		{
			AdminRight? adminRight = await _dataSource.AdminRights
					.FirstOrDefaultAsync(ag => ag.AdminId == AdminId && ag.RightId == rightId);

			if (adminRight == null)
			{
				AdminRight adminRightNew = new AdminRight()
				{
					AdminId = AdminId,
					RightId = rightId,
				};

				await _dataSource.AdminRights.AddAsync(adminRightNew);
			}
			else
			{
				_dataSource.AdminRights.Remove(adminRight);
			}

			await _dataSource.SaveChangesAsync();
		}

		private async Task SwitchGroupForAdminAsync(Guid groupId)
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
		}

		private async Task ShowGroupRightsAsync(int page = 1)
		{
			RightGroup? group = await _dataSource.RightGroups
				.FirstOrDefaultAsync(g => g.Id == GroupId);

			if (group == null)
			{
				await _stateManager.SendMessageAsync(AdministrationText.RightGroupLost);
				return;
			}

			IQueryable<RightGroupView> rightsInGroup = _dataSource.Rights
				.Skip((page - 1) * RIGHTS_BY_PAGE)
				.Take(RIGHTS_BY_PAGE)
				.Include(r => r.RightInGroups)
				.Select(r => new RightGroupView
				{
					Id = r.RigthId,
					Name = r.Name,
					IsInGroup = r.RightInGroups!.Any(rg => rg.GroupId == GroupId),
				});

			int rightsCount = _dataSource.Rights.Count();

			InlineKeyboardMarkup markup = GetGroupRightsButtons(rightsInGroup, rightsCount, page);

			await _stateManager.SendMessageAsync(AdministrationText.GroupRightsManaging, markup: markup);
		}

		private async Task ShowAdminRightsAsync(int page = 1)
		{
			IQueryable<RightView> rights = _dataSource.Rights
				.Skip((page - 1) * RIGHTS_BY_PAGE)
				.Take(RIGHTS_BY_PAGE)
				.Include(right => right.AdminRights)
				.Select(right => new RightView
				{
					Id = right.RigthId,
					Name = right.Name,
					AdminHasRight = right.AdminRights!.Any(ag => ag.AdminId == AdminId),
				}); ;

			int rightsCount = _dataSource.Rights.Count();

			InlineKeyboardMarkup markup = GetAdminRightsButtons(rights, rightsCount, page);

			await _stateManager.SendMessageAsync(AdministrationText.AdminRightsManaging, markup: markup);
		}

		private async Task ShowRightGroupsAsync(int page = 1)
		{
			IQueryable<RightGroup> groups = _dataSource.RightGroups
				.Skip((page - 1) * GROUPS_BY_PAGE)
				.Take(GROUPS_BY_PAGE);

			int groupsCount = _dataSource.RightGroups.Count();

			InlineKeyboardMarkup markup = GetRightGroupsButtons(groups, groupsCount, page);

			await _stateManager.SendMessageAsync(AdministrationText.GroupManaging, markup: markup);
		}

		private async Task ShowAdminPermissionGroupsAsync(int page=1)
		{
			IQueryable<AdminGroupView> groups = _dataSource.RightGroups
				.Skip((page - 1) * GROUPS_BY_PAGE)
				.Take(GROUPS_BY_PAGE)
				.Include(g => g.AdminsInGroup)
				.Select(g => new AdminGroupView
				{
					Id = g.Id,
					Name = g.Name,
					IsInGroup = g.AdminsInGroup!.Any(ag => ag.AdminId == AdminId),
				});

			int groupsCount = _dataSource.RightGroups.Count();

			InlineKeyboardMarkup markup = GetAdminGroupsButtons(groups, groupsCount, page);

			await _stateManager.SendMessageAsync(AdministrationText.AdminGroupManaging, markup: markup);
		}

		private InlineKeyboardMarkup GetAdminRightsButtons(IQueryable<RightView> rights, int rightsCount, int page)
		{
			List<InlineKeyboardButton[]> keyboard = new List<InlineKeyboardButton[]>();

			if (rights.Any())
			{
				keyboard = rights
					.AsEnumerable()
					.Select(right =>
					{
						InlineKeyboardButton[] row = new InlineKeyboardButton[]
						{
								new InlineKeyboardButton(right.Name)
								{
									CallbackData = JsonConvert.SerializeObject(new
									{
										Cmd = Command.AdminGroupRightDetails,
										RightId = right.Id,
									})
								},

						}
						.Union([GetSwitchRightButton(right, page)])
						.ToArray();

						return row;
					})
					.ToList();

				InlineKeyboardButton[] pagination = TelegramHelper.GetPagination(page, rightsCount, GROUPS_BY_PAGE, Command.MovePaginationAdminRights);
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

		private InlineKeyboardMarkup GetGroupRightsButtons(IQueryable<RightGroupView> rightsInGroup, int rightsCount, int page)
		{
			List<InlineKeyboardButton[]> keyboard = new List<InlineKeyboardButton[]>();

			if (rightsInGroup.Any())
			{
				keyboard = rightsInGroup
					.AsEnumerable()
					.Select(right =>
					{
						InlineKeyboardButton[] row = new InlineKeyboardButton[]
						{
								new InlineKeyboardButton(right.Name)
								{
									CallbackData = JsonConvert.SerializeObject(new
									{
										Cmd = Command.RightOfGroupDetails,
										RightId = right.Id,
									})
								},

						}
						.Union([GetSwitchRightInGroupButton(right, page)])
						.ToArray();

						return row;
					})
					.ToList();

				InlineKeyboardButton[] pagination = TelegramHelper.GetPagination(page, rightsCount, GROUPS_BY_PAGE, Command.MovePaginationRightsInGroup);
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
						Cmd = Command.RightGroupDetails,
						GroupId = GroupId,
					})
				}
			]);

			InlineKeyboardMarkup result = new InlineKeyboardMarkup(keyboard);
			return result;
		}

		private InlineKeyboardMarkup GetRightGroupsButtons(IQueryable<RightGroup> groups, int groupsCount, int page)
		{
			List<InlineKeyboardButton[]> keyboard = new List<InlineKeyboardButton[]>();

			if (groups.Any())
			{
				keyboard = groups
					.AsEnumerable()
					.Select(group =>
					{
						InlineKeyboardButton[] row = new InlineKeyboardButton[]
						{
								new InlineKeyboardButton(group.Name)
								{
									CallbackData = JsonConvert.SerializeObject(new
									{
										Cmd = Command.RightGroupDetails,
										GroupId = group.Id,
									})
								},
						}
						.ToArray();

						return row;
					})
					.ToList();

				InlineKeyboardButton[] pagination = TelegramHelper.GetPagination(page, groupsCount, GROUPS_BY_PAGE, Command.MovePaginationRightGroups);
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

			keyboard.AddRange(
			[
				[
					new InlineKeyboardButton(AdministrationText.AddRightGroup)
					{
						CallbackData = JsonConvert.SerializeObject(new
						{
							Cmd = Command.AddRightGroup,
						})
					}
				],
				[
					new InlineKeyboardButton(AdministrationText.Back)
					{
						CallbackData = JsonConvert.SerializeObject(new
						{
							Cmd = Command.AdministrationRights,
						})
					}
				]
			]);

			InlineKeyboardMarkup result = new InlineKeyboardMarkup(keyboard);
			return result;
		}

		private InlineKeyboardMarkup GetAdminGroupsButtons(IQueryable<AdminGroupView> groupsView, int groupsCount, int page)
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

				InlineKeyboardButton[] pagination = TelegramHelper.GetPagination(page, groupsCount, GROUPS_BY_PAGE, Command.MovePaginationAdminGroups);
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

		private InlineKeyboardButton GetSwitchRightButton(RightView rightView, int page)
		{
			string text = rightView.AdminHasRight ? AdministrationText.GroupTurnOff : AdministrationText.GroupTurnOn;
			bool groupMode = !rightView.AdminHasRight;

			InlineKeyboardButton result = new(text)
			{
				CallbackData = JsonConvert.SerializeObject(new
				{
					Cmd = Command.SwitchRightForAdmin,
					R = rightView.Id,
					P = page
				})
			};

			return result;
		}

		private InlineKeyboardButton GetSwitchRightInGroupButton(RightGroupView rightGroupView, int page)
		{
			string text = rightGroupView.IsInGroup ? AdministrationText.GroupTurnOff : AdministrationText.GroupTurnOn;
			bool groupMode = !rightGroupView.IsInGroup;

			InlineKeyboardButton result = new(text)
			{
				CallbackData = JsonConvert.SerializeObject(new
				{
					Cmd = Command.SwitchRightForGroup,
					R = rightGroupView.Id,
					P = page
				})
			};

			return result;
		}
		private InlineKeyboardButton GetSwitchGroupButton(AdminGroupView groupView, int page)
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
			await _stateManager.SendMessageAsync(text, markup: markup);
		}

		private InlineKeyboardMarkup GetAdminDetailsButtons(int adminId)
		{
			InlineKeyboardButton[][] keyboard =
			[
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
				],
				[
					new InlineKeyboardButton(AdministrationText.Back)
					{
						CallbackData = JsonConvert.SerializeObject(new
						{
							Cmd = Command.AdministrationUsers,
						})
					},
				]
			];

			InlineKeyboardMarkup result = new InlineKeyboardMarkup(keyboard);
			return result;
		}

		private async Task<bool> CheckRightGroupAsync(RightGroup? group, Guid groupId)
		{
			if (group == null)
			{
				string errorText = string.Format(AdministrationText.RightGroupNotFound, groupId);

				await _stateManager.SendMessageAsync(errorText, isOutOfQueue: true);
				_logger.Error(errorText);

				return false;
			}

			return true;
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
			await _stateManager.SendMessageAsync(AdministrationText.ShowAdminList, markup: markup);
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

		private async Task ShowRightsMenuAsync()
		{
			InlineKeyboardButton[][] buttons = GetRightsMenuButtons();
			InlineKeyboardMarkup markup = new InlineKeyboardMarkup(buttons);

			await _stateManager.SendMessageAsync(AdministrationText.RightsMenu, markup: markup);
		}

		private InlineKeyboardButton[][] GetRightsMenuButtons()
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
				],
				[
					new InlineKeyboardButton(AdministrationText.Back)
					{
						CallbackData = JsonConvert.SerializeObject(new
						{
							Cmd = Command.BackToAdministration,
						})
					},
				],
			];

			return result;
		}

		private async Task WelcomeAsync()
		{
			InlineKeyboardButton[][] buttons = GetMenuButtons();
			await _stateManager.SendMessageAsync(AdministrationText.Welcome, markup: buttons.ToInlineMarkup());
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
							Cmd = Command.AdministrationRights,
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
