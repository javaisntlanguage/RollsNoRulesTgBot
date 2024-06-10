using AdminTgBot.Infrastructure.Models;
using Database;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Helper;
using Database.Tables;
using NLog;
using Database.Enums;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.EntityFrameworkCore;
using AdminTgBot.Infrastructure.Conversations.Orders.Models;
using Microsoft.EntityFrameworkCore.Query;
using Telegram.Bot.Types.ReplyMarkups;
using Newtonsoft.Json;
using AdminTgBot.Infrastructure.Conversations.CatalogEditor;
using System.Security.Policy;
using WebDav;
using Azure;
using AdminTgBot.Infrastructure.Consumers;

namespace AdminTgBot.Infrastructure.Conversations.Orders
{
	internal class OrdersConversation : IConversation
	{
		private const int ORDERS_BY_PAGE = 5;
		private readonly AdminBotStateManager _stateManager;
		private ApplicationContext _dataSource;
		private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

		public OrdersFilter Filter { get; set; }

		public OrdersConversation()
		{
			Filter = new OrdersFilter();
		}

		public OrdersConversation(AdminBotStateManager statesManager) : this()
		{
			_stateManager = statesManager;
		}

		public async Task<Trigger?> TryNextStepAsync(ApplicationContext dataSource, Message message)
		{
			_dataSource = dataSource;

			switch (_stateManager.CurrentState)
			{
				case State.CommandOrders:
					{
						if (message.Text == MessagesText.CommandOrders)
						{
							await ShowNewOrdersAsync();
						}
						return Trigger.Ignore;
					}
			}

			return null;
		}

		public async Task<Trigger?> TryNextStepAsync(ApplicationContext dataSource, CallbackQuery query)
		{
			_dataSource = dataSource;

			JObject data = JObject.Parse(query.Data);
			Command command = data.GetEnumValue<Command>("Cmd");

			switch (command)
			{
				case Command.AcceptOrder:
					{
						int orderId = data["OrderId"].Value<int>();
						await AcceptOrderAsync(orderId);
						return Trigger.Ignore;
					}
				case Command.DeclineOrder:
					{
						int orderId = data["OrderId"].Value<int>();
						await DeclineOrderAsync(orderId);
						return Trigger.Ignore;
					}
			}

			switch (_stateManager.CurrentState)
			{
				case State.CommandOrders:
					{
						switch (command)
						{
							case Command.MovePaginationOrders:
								{
									int page = data["Page"].Value<int>();
									await ShowOrdersByFilterAsync(page);
									return Trigger.Ignore;
								}
							case Command.ChangeFilter:
								{
									await ShowFilterButtonsAsync();
									return Trigger.Ignore;
								}
							case Command.OrderDetails:
								{
									int orderId = data["OrderId"].Value<int>();
									int page = data["Page"].Value<int>();
									await ShowOrderDetailsAsync(orderId, page);
									return Trigger.Ignore;
								}
							case Command.BackToOrders:
								{
									int? orderId = data["OrderId"].Value<int?>();
									int page = data["Page"].Value<int>();
									await ShowOrdersByFilterAsync(page, orderId);
									return Trigger.Ignore;
								}
						}
						break;
					}
			}

			return null;
		}

		private async Task ShowOrderDetailsAsync(int orderId, int page)
		{
			Order? order = await (_dataSource.Orders
				.Include(o => o.OrderCartList))
				.FirstOrDefaultAsync(o => o.Id == orderId);

			if (!await CheckOrderAsync(order, orderId))
			{
				return;
			}

			string cart = string.Join("\n", order!.OrderCartList.Select(ocl => string.Format(OrdersText.CartItem,
				ocl.ProductName,
				ocl.Price,
				ocl.Count,
				ocl.Price * ocl.Count)));

			string text = string.Format(OrdersText.OrderDetails,
				order.Number, 
				order.Id, 
				order.DateFrom.ToString(OrdersFilter.DATE_FORMAT), 
				OrdersText.ResourceManager.GetString($"OrderState{order.State.ToString()}"),
				cart,
				order.Sum);

			InlineKeyboardButton[] back = GetBackToOrdersButton(orderId, page);
			List<InlineKeyboardButton[]> keyboard = new List<InlineKeyboardButton[]>();

			if (order.State == OrderState.New)
			{
				InlineKeyboardButton[] orderActions = GetNewOrderActionButtons(order.Id);
				keyboard.Add(orderActions);
			}

			keyboard.Add(back);

			InlineKeyboardMarkup markup = new InlineKeyboardMarkup(keyboard);

			await _stateManager.SendMessageAsync(text, replyMarkup: markup);
		}

		private InlineKeyboardButton[] GetNewOrderActionButtons(int orderId)
		{
			InlineKeyboardButton[] result = new InlineKeyboardButton[]
			{
				new InlineKeyboardButton(OrderConsumerText.Accept)
				{
					CallbackData = JsonConvert.SerializeObject(new
					{
						Cmd = Command.AcceptOrder,
						OrderId = orderId
					})
				},
				new InlineKeyboardButton(OrderConsumerText.Decline)
				{
					CallbackData = JsonConvert.SerializeObject(new
					{
						Cmd = Command.DeclineOrder,
						OrderId = orderId
					})
				},
			};

			return result;
		}

		private async Task ShowFilterButtonsAsync()
		{
			InlineKeyboardButton[] states = GetStateFilterButton();
			InlineKeyboardButton[] dates = GetDateFilterButtons();
			InlineKeyboardButton[] id = GetIdFilterButton();
			InlineKeyboardButton[] back = GetBackToOrdersButton();

			InlineKeyboardButton[][] keyboard = new InlineKeyboardButton[][]
			{
				states,
				dates,
				id,
				back
			};
			InlineKeyboardMarkup markup = new InlineKeyboardMarkup(keyboard);

			await _stateManager.SendMessageAsync(OrdersText.ChooseFilter, replyMarkup: markup);
			
		}

		private InlineKeyboardButton[] GetBackToOrdersButton(int? orderId = null, int page = 1)
		{
			InlineKeyboardButton[] result = new InlineKeyboardButton[]
			{
				new InlineKeyboardButton(OrdersText.BackToOrders)
				{
					CallbackData = JsonConvert.SerializeObject(new
					{
						Cmd = Command.BackToOrders,
						OrderId = orderId,
						Page = page
					})
				}
			};

			return result;
		}

		private InlineKeyboardButton[] GetIdFilterButton()
		{
			string text = Filter.Id.HasValue ?
				string.Format(OrdersText.IdFilterHasValue, Filter.Id) :
				OrdersText.IdFilterEmpty;

			InlineKeyboardButton[] result = new InlineKeyboardButton[]
			{
				new InlineKeyboardButton(text)
				{
					CallbackData = JsonConvert.SerializeObject(new
					{
						Cmd = Command.ChangeFilterId,
					})
				}
			};

			return result;
		}

		private InlineKeyboardButton[] GetDateFilterButtons()
		{
			string textDateFrom = Filter.DateFrom.HasValue ?
				string.Format(OrdersText.DateFromFilterHasValue, Filter.DateFrom.Value.ToString(OrdersFilter.DATE_FORMAT)) :
				OrdersText.DateFromFilterEmpty;

			string textDateTo = Filter.DateTo.HasValue ?
				string.Format(OrdersText.DateToFilterHasValue, Filter.DateTo.Value.ToString(OrdersFilter.DATE_FORMAT)) :
				OrdersText.DateToFilterEmpty;

			InlineKeyboardButton[] result = new InlineKeyboardButton[]
			{
				new InlineKeyboardButton(textDateFrom)
				{
					CallbackData = JsonConvert.SerializeObject(new
					{
						Cmd = Command.ChangeFilterDateFrom,
					})
				},
				new InlineKeyboardButton(textDateTo)
				{
					CallbackData = JsonConvert.SerializeObject(new
					{
						Cmd = Command.ChangeFilterDateTo,
					})
				},
			};

			return result;
		}

		private InlineKeyboardButton[] GetStateFilterButton()
		{
			string text;

			int stateFilterCount = BoolNullableToInt(Filter.IsNew) +
					BoolNullableToInt(Filter.IsApproved) +
					BoolNullableToInt(Filter.IsCompleted) +
					BoolNullableToInt(Filter.IsDeclined) +
					BoolNullableToInt(Filter.IsError);

			if (stateFilterCount > 0)
			{
				text = string.Format(OrdersText.StateFilterHasValue, stateFilterCount);
			}
			else
			{
				text = OrdersText.StateFilterEmpty;
			}

			InlineKeyboardButton[] result = new InlineKeyboardButton[]
			{
				new InlineKeyboardButton(text)
				{
					CallbackData = JsonConvert.SerializeObject(new
					{
						Cmd = Command.ChangeFilterState,
					})
				}
			};

			return result;
		}

		/// <summary>
		/// есть ли значение у объекта
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		private int BoolNullableToInt(bool? obj)
		{
			return obj.HasValue ? 1 : 0;
		}

		private async Task ShowNewOrdersAsync()
		{
			OrdersFilter ordersFilter = new OrdersFilter()
			{
				IsNew = true,
			};
			Filter = ordersFilter;

			await ShowOrdersByFilterAsync(1);
		}

		private async Task ShowOrdersByFilterAsync(int page, int? orderId = null)
		{
			IQueryable<Order> orders = _dataSource.Orders
				.Include(o => o.OrderCartList)
				.AsQueryable();

			if (Filter.Id.HasValue)
			{
				orders = orders.Where(o => o.Id == Filter.Id);
			}
			if (Filter.DateFrom.HasValue)
			{
				orders = orders.Where(o => o.DateFrom >= Filter.DateFrom);
			}
			if(Filter.DateTo.HasValue)
			{
				orders = orders.Where(o => o.DateFrom <= Filter.DateTo);

			}
			if(Filter.HasStateFilter)
			{
				OrderState actualStates = OrderState.None;

				if(Filter.IsNew ?? false)
				{
					actualStates = actualStates | OrderState.New;
				}
				if(Filter.IsApproved ?? false)
				{
					actualStates = actualStates | OrderState.Approved;
				}
				if(Filter.IsCompleted ?? false)
				{
					actualStates = actualStates | OrderState.Completed;
				}
				if(Filter.IsDeclined ?? false)
				{
					actualStates = actualStates | OrderState.Declined;
				}
				if(Filter.IsError ?? false)
				{
					actualStates = actualStates | OrderState.Error;
				}

				orders = orders.Where(o => (o.State & actualStates) == o.State);
			}

			List<Order> orderedOrders = orders
				.OrderByDescending(o => o.DateFrom)
				.ToList();

			IEnumerable<Order> filteredOrders = filteredOrders = orderedOrders
						.Skip((page - 1) * ORDERS_BY_PAGE)
						.Take(ORDERS_BY_PAGE)
						.ToList(); ;

			if (orderId.IsNotNull())
			{
				int pageCache = page;
				bool isPageFound = false;
				int totalPages = (int)Math.Ceiling(orderedOrders.Count() / (double)ORDERS_BY_PAGE);
				while (!isPageFound && page <= totalPages)
				{
					if (filteredOrders.Any(fo => fo.Id == orderId))
					{
						isPageFound = true;
					}
					else
					{
						filteredOrders = orderedOrders
							.Skip((page) * ORDERS_BY_PAGE)
							.Take(ORDERS_BY_PAGE)
							.ToList();
						page++;
					}
				}

				if(!isPageFound)
				{
					page = pageCache;
					filteredOrders = orderedOrders
							.Skip((page - 1) * ORDERS_BY_PAGE)
							.Take(ORDERS_BY_PAGE)
							.ToList();

					await _stateManager.SendMessageAsync(OrdersText.BadOrderIdForFilter);
				}
			}

			string text = string.Format(OrdersText.FilterTitle, Filter.ToString());
			List<IEnumerable<InlineKeyboardButton>> orderButtons = GetOrderButtons(filteredOrders, page);
			List<InlineKeyboardButton> pagination = GetPagination(page, orders);
			List<InlineKeyboardButton> changefilter = GetChangeFilterButton();
			orderButtons.Add(pagination);
			orderButtons.Add(changefilter);

			InlineKeyboardMarkup markup = new InlineKeyboardMarkup(orderButtons);
			await _stateManager.SendMessageAsync(text, replyMarkup: markup);
		}

		private List<InlineKeyboardButton> GetChangeFilterButton()
		{
			List<InlineKeyboardButton> result = new List<InlineKeyboardButton>()
			{
				new InlineKeyboardButton(OrdersText.ChangeFilter)
				{
					CallbackData = JsonConvert.SerializeObject(new
					{
						Cmd = Command.ChangeFilter,
					})
				}
			};

			return result;
		}

		private List<InlineKeyboardButton> GetPagination(int page, IQueryable<Order> orders)
		{
			List<InlineKeyboardButton> result = new List<InlineKeyboardButton>();

			if(page > 1)
			{
				result.Add(new InlineKeyboardButton(MessagesText.PaginationPrevious)
				{
					CallbackData = JsonConvert.SerializeObject(new
					{
						Cmd = Command.MovePaginationOrders,
						Page = page - 1,
					})
				});
			}
			else
			{
				result.Add(GetNoPaginationButton());
			}

			if (orders.Count() > page * ORDERS_BY_PAGE)
			{
				result.Add(new InlineKeyboardButton(MessagesText.PaginationNext)
				{
					CallbackData = JsonConvert.SerializeObject(new
					{
						Cmd = Command.MovePaginationOrders,
						Page = page + 1,
					})
				});
			}
			else
			{
				result.Add(GetNoPaginationButton());
			}
			
			return result;
		}

		/// <summary>
		/// кнопка пустой пагинации
		/// </summary>
		/// <returns></returns>
		private InlineKeyboardButton GetNoPaginationButton()
		{
			InlineKeyboardButton result = new InlineKeyboardButton(MessagesText.NoPagination)
			{
				CallbackData = JsonConvert.SerializeObject(new
				{
					Cmd = Command.Ignore
				})
			};

			return result;
		}

		private List<IEnumerable<InlineKeyboardButton>> GetOrderButtons(IEnumerable<Order> filteredOrders, int page)
		{
			List<IEnumerable<InlineKeyboardButton>> result = null;
			if (filteredOrders.Any())
			{
				result = filteredOrders
					.Select(fo => new InlineKeyboardButton[]
					{
						new InlineKeyboardButton(string.Format(OrdersText.OrderPreview, fo.Number, fo.Id, fo.Sum))
						{
							CallbackData = JsonConvert.SerializeObject(new
							{
								Cmd = Command.OrderDetails,
								OrderId = fo.Id,
								Page = page
							})
						}
					}.AsEnumerable())
					.ToList();
			}
			else
			{
				result = new List<IEnumerable<InlineKeyboardButton>>
				{
					new InlineKeyboardButton[]
					{
						new InlineKeyboardButton(OrdersText.OrdersFilterEmpty)
						{
							CallbackData = JsonConvert.SerializeObject(new
							{
								Cmd = Command.Ignore
							})
						}
					}
				};
			}

			return result;
		}

		private async Task<bool> CheckOrderAsync(Order? order, int orderId)
		{
			if (order.IsNull())
			{
				string errorText = string.Format(OrdersText.OrderNotFound, orderId);

				await _stateManager.SendMessageAsync(errorText, isOutOfQueue: true);
				_logger.Error(errorText);

				return false;
			}

			return true;
		}

		private async Task DeclineOrderAsync(int orderId)
		{
			Order? order = await _dataSource.Orders.FirstOrDefaultAsync(o => o.Id == orderId);

			if (!await CheckOrderAsync(order, orderId))
			{
				return;
			}

			order!.State = OrderState.Declined;
			_dataSource.SaveChanges();

			string text = string.Format(OrdersText.OrderDeclined, order.Number, orderId);
			await _stateManager.SendMessageAsync(text, isOutOfQueue: true);
		}

		private async Task AcceptOrderAsync(int orderId)
		{
			Order? order = await _dataSource.Orders.FirstOrDefaultAsync(o => o.Id == orderId);

			if(order.IsNull())
			{
				string errorText = string.Format(OrdersText.OrderNotFound, orderId);

				await _stateManager.SendMessageAsync(errorText, isOutOfQueue: true);
				_logger.Error(errorText);

				return;
			}

			order!.State = OrderState.Approved;
			_dataSource.SaveChanges();

			string text = string.Format(OrdersText.OrderApproved, order.Number, orderId);
			await _stateManager.SendMessageAsync(text, isOutOfQueue: true);
		}
	}
}
