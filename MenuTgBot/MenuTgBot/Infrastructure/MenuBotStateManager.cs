using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Util.Core.StateMachine;
using MenuTgBot.Infrastructure.Conversations;
using Telegram.Util.Core;
using Database;
using Helper;
using Newtonsoft.Json;
using Database.Tables;
using MenuTgBot.Infrastructure.Conversations.Catalog;
using Microsoft.EntityFrameworkCore;
using MenuTgBot.Infrastructure.Models;
using NLog;
using MenuTgBot.Infrastructure.Conversations.Cart;
using MenuTgBot.Infrastructure.Conversations.Start;
using MenuTgBot.Infrastructure.Conversations.Orders;
using Database.Enums;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Util.Core.Exceptions;
using Microsoft.EntityFrameworkCore.Internal;
using Telegram.Util.Core.Interfaces;
using Database.Classes;
using Microsoft.Extensions.Options;
using MenuTgBot.Infrastructure.Interfaces;

namespace MenuTgBot.Infrastructure
{
    internal class MenuBotStateManager : StateManager
    {
        private readonly StateMachine<State, Trigger> _machine;
		private readonly MenuBotSettings _config;
        private Dictionary<string, IConversation> _handlers;

		public State CurrentState { get; private set; }

		public MenuBotStateManager(ITelegramBotClient botClient,
			IDbContextFactory<ApplicationContext> contextFactory,
			IMenuHandler menuHandler,
			IMenuBotStateMachineBuilder stateMachineBuilder,
			IOptions<MenuBotSettings> options,
			long chatId) : base(botClient, contextFactory, menuHandler)
		{
			_message = new Message();
			_machine = stateMachineBuilder.Build(
				stateAccessor: () => CurrentState,
				stateMutator: s =>
				{
					CurrentState = s;
				},
				messageHandler: NextStateMessageAsync,
                queryHandler: NextStateQueryAsync
			);
			_handlers = new Dictionary<string, IConversation>();
			_config = options.Value;

			ChatId = chatId;
		}
		#region Private Methods

		/// <summary>
		/// назначение классов-обработчиков команд
		/// </summary>
		public override void ConfigureHandlers()
        {
            SetHandler(new StartConversation(this));
            SetHandler(new CatalogConversation(this));
            SetHandler(new CartConversation(this));
            SetHandler(new OrdersConversation(this, _config));
        }

        /// <summary>
        /// сохранить состояние пользователя
        /// </summary>
        /// <returns></returns>
        private async Task SaveStateAsync(ApplicationContext dataSource)
        {
            string data = JsonConvert.SerializeObject(new
            {
                Cart = GetHandler<CartConversation>(),
                Orders = GetHandler<OrdersConversation>()
            });

            await dataSource.UserStates_Set(ChatId, (int)CurrentState, data, _lastMessageId);
        }

        private void RecoverHandlers(string? data)
        {
            if (data.IsNotNullOrEmpty())
            {
                try
                {
                    UserConversations conversations = JsonConvert.DeserializeObject<UserConversations>(data);
                    _handlers = conversations.GetHandlers(this, _config);
                }
                catch (Exception ex)
                {
                    _logger.Error($"Не удалось восстановить пользователя {ChatId}. Data={data}", ex);
                }
            }
        }

        /// <summary>
        /// продвижение по конечному автомату
        /// </summary>
        /// <param name="trigger"></param>
        /// <returns></returns>
        private async Task<bool> CallTriggerAsync(Trigger? trigger)
        {
            if (trigger.IsNotNull())
            {
                await _machine.FireAsync(trigger.Value);
                return true;
            }

            return false;
        }

        /// <summary>
        /// установка обработчика
        /// </summary>
        /// <param name="conversation"></param>
        private void SetHandler(IConversation conversation)
        {
            _handlers[conversation.GetType().Name] = conversation;
        }

        #endregion Private Methods

        #region Public Methods

        /// <summary>
        /// обработка команды /start
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task StartAsync(Message message)
        {
            _message = message;

            await _machine.FireAsync(Trigger.CommandStartStarted);
        }

        /// <summary>
        /// обработка команды каталог
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task ShowCatalogAsync(Message message)
        {
            _message = message;

            await _machine.FireAsync(Trigger.CommandShopCatalogStarted);
        }

        /// <summary>
        /// обработка команды Корзина
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task ShowCartAsync(Message message)
        {
            _message = message;

            await _machine.FireAsync(Trigger.CommandCartStarted);
        }

        /// <summary>
        /// обработка команды Заказы
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task ShowOrdersAsync(Message message)
        {
            _message = message;

            await _machine.FireAsync(Trigger.CommandOrdersStarted);
        }

        /// <summary>
        /// обработка сообщения
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public override async Task<bool> NextStateAsync(Message message)
        {
            await base.NextStateAsync(message);

            await using ApplicationContext dataSource = await _contextFactory.CreateDbContextAsync();

            bool result = await _handlers.Values
                .ToAsyncEnumerable()
                .AnyAwaitAsync(async x => await CallTriggerAsync(await x.TryNextStepAsync(dataSource, message)));

            await SaveStateAsync(dataSource);
            return result;
        }

        /// <summary>
        /// обработка запроса
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public override async Task<bool> NextStateAsync(CallbackQuery query)
        {
            await base.NextStateAsync(query);

            if (_lastMessageId.IsNotNull() && _query.Message.MessageId != _lastMessageId)
            {
                throw new NotLastMessageException();
            }

            await using ApplicationContext dataSource = await _contextFactory.CreateDbContextAsync();

            bool result = await _handlers.Values
				.ToAsyncEnumerable()
				.AnyAwaitAsync(async x =>
					await CallTriggerAsync(
						await (x.TryNextStepAsync(dataSource, query) ??
								Task.FromResult<Trigger?>(null))));

			await SaveStateAsync(dataSource);
            return result;
        }

        /// <summary>
        /// получение обработчика
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetHandler<T>() where T : IConversation
        {
            string key = typeof(T).Name;
            if (_handlers.ContainsKey(key)) return (T)_handlers[key];

            return default;
        }

		/// <summary>
		/// восстановление состояния
		/// </summary>
		/// <param name="userState"></param>
		/// <returns></returns>
		public async Task StateRecoveryAsync(ApplicationContext dataSource)
		{
			UserState userState = await dataSource.UserStates
					.FirstOrDefaultAsync(us => us.UserId == ChatId);

			if (userState == null)
			{
				CurrentState = State.New;
			}
			else
			{
				CurrentState = (State)userState.StateId;
				_lastMessageId = userState.LastMessageId;

				RecoverHandlers(userState.Data);
			}
		}

		#endregion Public Methods
	}

    public enum Trigger
    {
        CommandStartStarted,
        CommandCartStarted,
        CommandShopCatalogStarted,
        Ignore,
        RoleSet,
        ProductAddedToCart,
        AddToCartFromCategory,
        AddToCartFromCategoryShowProduct,
        ToCatalogState,
        DecreasedCountCart,
        IncreasedCountCart,
        EnterLogin,
        EnterPassword,
        ClientTookOrder,
        CommandOrdersStarted,
        EnterAddressCity,
        EnterAddressStreet,
        EnterAddressHouseNumber,
        EnterAddressFlat,
        EnterAddressComment,
        EnterAddressBuilding,
        AddressAddedNextPhone,
        EmptyPhone,
        BackFromPhone,
        ChangePhone,
        SendedSms,
        EnteredSms,
        AddressAddedReturnToDeliverySettings,
        CancelAddNewAddress,
    }

    public enum State
    {
        New,
        CommandStart,
        CommandShopCatalog,
        Menu,
        CommandCart,
        CommandShopContacts,
        CatalogCartActions,
        CommandOrders,
        OrderNewAddressCityEditor,
        OrderNewAddressStreetEditor,
        OrderNewAddressHouseNumberEditor,
        OrderNewAddressFlatEditor,
        OrderNewAddressCommentEditor,
        OrderNewAddressBuildingEditor,
        OrderPhone,
        SmsPhone
    }
}
