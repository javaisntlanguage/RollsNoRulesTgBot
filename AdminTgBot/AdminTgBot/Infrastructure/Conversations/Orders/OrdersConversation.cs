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
using System.Text.RegularExpressions;
using System.Globalization;

namespace AdminTgBot.Infrastructure.Conversations.Orders
{
	internal class OrdersConversation : IConversation
	{
		private const int ORDERS_BY_PAGE = 5;
		private const string EXPORT_DATE_FORMAT = "yyyy.MM.dd";
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
				case State.FilterDateFrom:
					{
						return await TrySetFilterDateAsync(message.Text, mode: false);
					}
				case State.FilterDateTo:
					{
						return await TrySetFilterDateAsync(message.Text, mode: true);
					}
				case State.FilterId:
					{
						return await TrySetFilterIdAsync(message.Text);
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
							case Command.ChangeFilterState:
								{
									await ShowOrderStatesAsync();
									return Trigger.Ignore;
								}
							case Command.ChangeFilterStateValue:
								{
									OrderState state = data.GetEnumValue<OrderState>("State");
									bool isOn = data["IsOn"].Value<bool>();
									await SetOrderFilterStateAsync(state, isOn);
									return Trigger.Ignore;
								}
							case Command.ChangeFilterDateFrom:
								{
									await ShowOrderFilterDateAsync(mode: false);
									return Trigger.WaitForDateFrom;
								}
							case Command.ChangeFilterDateTo:
								{
									await ShowOrderFilterDateAsync(mode: true);
									return Trigger.WaitForDateTo;
								}
							case Command.ChangeFilterId:
								{
									await ShowOrderFilterIdAsync();
									return Trigger.WaitForOrderFilterId;
								}
						}
						break;
					}
				case State.FilterDateTo:
				case State.FilterDateFrom:
					{
						switch (command)
						{
							case Command.ChangeFilter:
								{
									await ShowFilterButtonsAsync();
									return Trigger.BackToOrderFilter;
								}
							case Command.ChangeFilterDateReset:
								{
									bool mode = data["Mode"].Value<bool>();
									await ResetOrderFilterDateAsync(mode);
									return Trigger.BackToOrderFilter;
								}
							case Command.ChangeFilterDateSuggestion:
								{
									bool mode = data["Mode"].Value<bool>();
									DateTimeOffset date = data["Date"].Value<DateTime>();
									return await TrySetFilterDateAsync(date, mode);
								}
						}
						break;
					}
				case State.FilterId:
					{
						switch(command) 
						{
							case Command.ChangeFilter:
								{
									await ShowFilterButtonsAsync();
									return Trigger.BackToOrderFilter;
								}
							case Command.ChangeFilterIdReset:
								{
									await ResetOrderFilterIdAsync();
									return Trigger.BackToOrderFilter;
								}
						}
						break;
					}
			}

			return null;
		}

		private async Task<Trigger?> TrySetFilterIdAsync(string? text)
		{
			if (text.IsNullOrEmpty())
			{
				await _stateManager.SendMessageAsync(OrdersText.EnterText);
			}
			else if (int.TryParse(text, out int id))
			{
				Filter.Id = id;
				await ShowFilterButtonsAsync();
				return Trigger.BackToOrderFilter;
			}
			else
			{
				await _stateManager.SendMessageAsync(OrdersText.EnterInt);
			}

			await ShowOrderFilterIdAsync();

			return Trigger.Ignore;
		}

		private async Task ResetOrderFilterIdAsync()
		{
			Filter.Id = null;
			await ShowFilterButtonsAsync();
		}

		private async Task ShowOrderFilterIdAsync()
		{
			string currentValue = Filter.Id?.ToString() ?? OrdersText.NoValue;
			string text = string.Format(OrdersText.ChooseFilterId, currentValue);

			InlineKeyboardButton[] back = GetbackToFilterButton();
			InlineKeyboardButton[] reset = GetIdResetButton();
			InlineKeyboardButton[][] keyboard = new InlineKeyboardButton[][]
			{
				reset,
				back,
			};
			InlineKeyboardMarkup markup = new InlineKeyboardMarkup(keyboard);

			await _stateManager.SendMessageAsync(text, replyMarkup: markup);
		}

		private InlineKeyboardButton[] GetIdResetButton()
		{
			InlineKeyboardButton[] result = new InlineKeyboardButton[]
			{
				new InlineKeyboardButton(OrdersText.FilterReset)
				{
					CallbackData = JsonConvert.SerializeObject(new
						{
							Cmd = Command.ChangeFilterIdReset,
						})
				}
			};

			return result;
		}

		private async Task ResetOrderFilterDateAsync(bool mode)
		{
			if(mode)
			{
				Filter.DateTo = null;
			}
			else
			{
				Filter.DateFrom = null;
			}

			await ShowFilterButtonsAsync();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="text"></param>
		/// <param name="mode">
		/// false - date from
		/// true - date to
		/// </param>
		/// <returns></returns>
		private async Task<Trigger?> TrySetFilterDateAsync(DateTimeOffset date, bool mode)
		{
			if (date <= DateTimeOffset.Now.Date)
			{
				if (mode)
				{
					if (Filter.DateFrom.IsNotNull() && Filter.DateFrom > date)
					{
						await _stateManager.SendMessageAsync(OrdersText.FilterDateToBeforeDateFrom);
					}
					else
					{
						Filter.DateTo = date;

						await ShowFilterButtonsAsync();

						return Trigger.BackToOrderFilter;
					}
				}
				else
				{
					if (Filter.DateTo.IsNotNull() && Filter.DateTo < date)
					{
						await _stateManager.SendMessageAsync(OrdersText.FilterDateFromAfterDateTo);
					}
					else
					{
						Filter.DateFrom = date;

						await ShowFilterButtonsAsync();

						return Trigger.BackToOrderFilter;
					}
				}
			}
			else
			{
				await _stateManager.SendMessageAsync(OrdersText.DateFromFilterFuture);
			}

			await ShowOrderFilterDateAsync(mode);

			return Trigger.Ignore;
		}
		private async Task<Trigger?> TrySetFilterDateAsync(string? text, bool mode)
		{
            if (text.IsNullOrEmpty())
            {
				await _stateManager.SendMessageAsync(OrdersText.EnterText);    
            }
            else if (DateTimeOffset.TryParse(text, out DateTimeOffset date))
			{
				return await TrySetFilterDateAsync(date, mode);
			}
			else
			{
				await _stateManager.SendMessageAsync(OrdersText.DateParseError);
			}

			await ShowOrderFilterDateAsync(mode);

			return Trigger.Ignore;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="mode">
		/// false - date from
		/// true - date to
		/// </param>
		/// <returns></returns>
		private async Task ShowOrderFilterDateAsync(bool mode)
		{
			DateTimeOffset? date = mode ? Filter.DateTo : Filter.DateFrom;
			string currentValue = date?.ToString(OrdersFilter.DATE_FORMAT) ?? OrdersText.NoValue;
			string text = string.Format(OrdersText.ChooseFilterDate, currentValue);

			InlineKeyboardButton[][] dateSuggestions = GetDateSuggestionButtons(mode);
			InlineKeyboardButton[] reset = GetDateResetButton(mode);
			InlineKeyboardButton[] back = GetbackToFilterButton();
			InlineKeyboardButton[][] keyboard = Enumerable.Concat(dateSuggestions, new InlineKeyboardButton[][]
			{
				reset, 
				back,
			})
			.ToArray();
			InlineKeyboardMarkup markup = new InlineKeyboardMarkup(keyboard);

			await _stateManager.SendMessageAsync(text, replyMarkup: markup);
		}

		private InlineKeyboardButton[][] GetDateSuggestionButtons(bool mode)
		{
			InlineKeyboardButton[][] result = new InlineKeyboardButton[][]
			{
				new InlineKeyboardButton[]
				{
					new InlineKeyboardButton(OrdersText.Today)
					{
						CallbackData = JsonConvert.SerializeObject(new
						{
							Cmd = Command.ChangeFilterDateSuggestion,
							Mode = mode,
							Date = DateTimeOffset.Now.Date.ToString(EXPORT_DATE_FORMAT),
						})
					},
					new InlineKeyboardButton(OrdersText.Yesterday)
					{
						CallbackData = JsonConvert.SerializeObject(new
						{
							Cmd = Command.ChangeFilterDateSuggestion,
							Mode = mode,
							Date = DateTimeOffset.Now.Date.AddDays(-1).ToString(EXPORT_DATE_FORMAT)
						})
					},
				},
				new InlineKeyboardButton[]
				{
					new InlineKeyboardButton(OrdersText.WeekAgo)
					{
						CallbackData = JsonConvert.SerializeObject(new
						{
							Cmd = Command.ChangeFilterDateSuggestion,
							Mode = mode,
							Date = DateTimeOffset.Now.Date.AddWeeks(-1).ToString(EXPORT_DATE_FORMAT)
						})
					},
					new InlineKeyboardButton(OrdersText.MonthAgo)
					{
						CallbackData = JsonConvert.SerializeObject(new
						{
							Cmd = Command.ChangeFilterDateSuggestion,
							Mode = mode,
							Date = DateTimeOffset.Now.Date.AddMonths(-1).ToString(EXPORT_DATE_FORMAT)
						})
					},
				},
			};

			return result;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="mode">
		/// false - date from
		/// true - date to
		/// </param>
		/// <returns></returns>
		private InlineKeyboardButton[] GetDateResetButton(bool mode)
		{
			InlineKeyboardButton[] result = new InlineKeyboardButton[]
			{
				new InlineKeyboardButton(OrdersText.FilterReset)
				{
					CallbackData = JsonConvert.SerializeObject(new
					{
						Cmd = Command.ChangeFilterDateReset,
						Mode = mode
					})
				}
			};

			return result;
		}

		private async Task SetOrderFilterStateAsync(OrderState state, bool isOn)
		{
			switch (state)
			{
				case OrderState.New:
					{
						Filter.IsNew = isOn ? true : null;
						break;
					}
				case OrderState.Approved:
					{
						Filter.IsApproved = isOn ? true : null;
						break;
					}
				case OrderState.Completed:
					{
						Filter.IsCompleted = isOn ? true : null;
						break;
					}
				case OrderState.Declined:
					{
						Filter.IsDeclined = isOn ? true : null;
						break;
					}
				case OrderState.Error:
					{
						Filter.IsError = isOn ? true : null;
						break;
					}
				default:
					{
						throw new Exception($"Неподдерживаемый OrderState: {state.ToString()}");
					}
			}

			await ShowOrderStatesAsync();
		}

		private async Task ShowOrderStatesAsync()
		{
			List<InlineKeyboardButton[]> states = GetOrderStateButtons();
			InlineKeyboardButton[] back = GetbackToFilterButton();
			states.Add(back);
			InlineKeyboardMarkup markup = new InlineKeyboardMarkup(states);

			await _stateManager.SendMessageAsync(OrdersText.ChooseOrderState, replyMarkup: markup);
		}

		private InlineKeyboardButton[] GetbackToFilterButton()
		{
			InlineKeyboardButton[] result = new InlineKeyboardButton[]
			{
				new InlineKeyboardButton(OrdersText.BackToFilter)
				{
					CallbackData = JsonConvert.SerializeObject(new
						{
							Cmd = Command.ChangeFilter,
						})
				}
			};

			return result;
		}

		private List<InlineKeyboardButton[]> GetOrderStateButtons()
		{
			Dictionary<OrderState, bool> activeStates = new Dictionary<OrderState, bool>()
			{
				{ OrderState.New, Filter.IsNew.HasValue },
				{ OrderState.Approved, Filter.IsApproved.HasValue },
				{ OrderState.Completed, Filter.IsCompleted.HasValue },
				{ OrderState.Declined, Filter.IsDeclined.HasValue },
				{ OrderState.Error, Filter.IsError.HasValue },
			};
			string stateMode;
			string text;

			List<InlineKeyboardButton[]> result = activeStates.Select(aS =>
			{
				stateMode = aS.Value ? "OrderStateOn" : "OrderStateOff";
				text = OrdersText.ResourceManager.GetString($"OrderState{aS.Key.ToString()}") +
					   OrdersText.ResourceManager.GetString(stateMode);

				return new InlineKeyboardButton[]
				{
					new InlineKeyboardButton(text)
					{
						CallbackData = JsonConvert.SerializeObject(new
						{
							Cmd = Command.ChangeFilterStateValue,
							State = aS.Key,
							IsOn = !aS.Value
						})
					},
				};
			})
			.ToList();

			return result;
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
			if (Filter.IsNull())
			{
				OrdersFilter ordersFilter = new OrdersFilter()
				{
					IsNew = true,
				};

				Filter = ordersFilter;
			}

			await ShowOrdersByFilterAsync();
		}

		private async Task ShowOrdersByFilterAsync(int page = 1, int? orderId = null)
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
						.ToList();

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
