﻿using System;
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

namespace MenuTgBot.Infrastructure
{
    internal class MenuBotStateManager : StateManager
    {
        private readonly IDbContextFactory<ApplicationContext> _contextFactory;
        private StateMachine<State, Trigger> _machine;
        private Dictionary<string, IConversation> _handlers;

        public State CurrentState { get; private set; }
        public HashSet<RolesList> Roles { get; set; }


        private MenuBotStateManager(ITelegramBotClient botClient, IDbContextFactory<ApplicationContext> contextFactory, MenuBotCommandsManager commandsManager, long chatId)
        {
            ChatId = chatId;

            _message = new Message();
            _commandsManager = commandsManager;
            _machine = new StateMachine<State, Trigger>(() => CurrentState,
                s =>
                {
                    CurrentState = s;
                });
            _handlers = new Dictionary<string, IConversation>();
            _botClient = botClient;
            _contextFactory = contextFactory;
            ConfigureMachine();
            ConfigureHandlers();
        }
        #region Private Methods

        /// <summary>
        /// назначение классов-обработчиков команд
        /// </summary>
        protected override void ConfigureHandlers()
        {
            SetHandler(new StartConversation(this));
            SetHandler(new CatalogConversation(this));
            SetHandler(new CartConversation(this));
            SetHandler(new OrdersConversation(this));
        }

        /// <summary>
        /// конфигурация конечного автомата
        /// для всех новых триггеров добавлять:
        /// Permit (для обработки)
        /// Ignore (для пропуска)
        /// но добавлять одно из этого обязательно
        /// </summary>
        protected override void ConfigureMachine()
        {
            _machine.Configure(State.New)
            .Permit(Trigger.CommandStartStarted, State.CommandStart)
            .Permit(Trigger.CommandShopCatalogStarted, State.CommandShopCatalog)
            .Permit(Trigger.CommandCartStarted, State.CommandCart)
            .Permit(Trigger.CommandOrdersStarted, State.CommandOrders)
            .Ignore(Trigger.Ignore);

            _machine.Configure(State.CommandStart)
            .SubstateOf(State.New) 
            .OnEntryFromAsync(Trigger.CommandStartStarted, CatalogAfterSetRoleAsync);

            _machine.Configure(State.CommandShopCatalog)
            .SubstateOf(State.New)
            .PermitReentry(Trigger.ChooseCategory)
            .PermitReentry(Trigger.ProductAddedToCart)
            .Permit(Trigger.DecreasedCountCart, State.CatalogCartActions)
            .Permit(Trigger.IncreasedCountCart, State.CatalogCartActions)
            .Permit(Trigger.AddToCartFromCategoryShowProduct, State.CatalogCartActions)
            .OnEntryFromAsync(Trigger.ChooseCategory, NextStateQueryAsync)
            .OnEntryFromAsync(Trigger.CommandShopCatalogStarted, NextStateMessageAsync);

            _machine.Configure(State.CatalogCartActions)
            .SubstateOf(State.CommandShopCatalog)
            .Permit(Trigger.ToCatalogState, State.CommandShopCatalog)
            .OnEntryFromAsync(Trigger.AddToCartFromCategoryShowProduct, NextStateQueryAsync)
            .OnEntryFromAsync(Trigger.DecreasedCountCart, NextStateQueryAsync)
            .OnEntryFromAsync(Trigger.IncreasedCountCart, NextStateQueryAsync);

            _machine.Configure(State.CommandCart)
            .SubstateOf(State.New)
            .Permit(Trigger.ClientTookOrder, State.CommandOrders)
            .OnEntryFromAsync(Trigger.CommandCartStarted, NextStateMessageAsync);

            _machine.Configure(State.CommandOrders)
            .SubstateOf(State.New)
            .OnEntryFromAsync(Trigger.ClientTookOrder, NextStateQueryAsync)
            .OnEntryFromAsync(Trigger.CommandOrdersStarted, NextStateMessageAsync)
            .Permit(Trigger.EnterAddressCity, State.OrderNewAddressCityEditor)
            .Permit(Trigger.EnterAddressStreet, State.OrderNewAddressStreetEditor)
            .Permit(Trigger.EnterAddressHouseNumber, State.OrderNewAddressHouseNumberEditor)
            .Permit(Trigger.EnterAddressBuilding, State.OrderNewAddressBuildingEditor)
            .Permit(Trigger.EnterAddressFlat, State.OrderNewAddressFlatEditor)
            .Permit(Trigger.EnterAddressComment, State.OrderNewAddressCommentEditor)
            .Permit(Trigger.AddressAddedNextPhone, State.OrderPhone)
            .Permit(Trigger.EmptyPhone, State.OrderPhone)
            .Permit(Trigger.ChangePhone, State.OrderPhone);

            _machine.Configure(State.OrderNewAddressCityEditor)
            .SubstateOf(State.CommandOrders);

            _machine.Configure(State.OrderNewAddressStreetEditor)
            .SubstateOf(State.CommandOrders);

            _machine.Configure(State.OrderNewAddressHouseNumberEditor)
            .SubstateOf(State.CommandOrders);

            _machine.Configure(State.OrderNewAddressBuildingEditor)
            .SubstateOf(State.CommandOrders);

            _machine.Configure(State.OrderNewAddressFlatEditor)
            .SubstateOf(State.CommandOrders);

            _machine.Configure(State.OrderNewAddressCommentEditor)
            .SubstateOf(State.CommandOrders)
            .Permit(Trigger.AddressAddedReturnToDeliverySettings, State.CommandOrders)
            .Permit(Trigger.CancelAddNewAddress, State.CommandOrders);

            _machine.Configure(State.OrderPhone)
            .SubstateOf(State.CommandOrders)
            .OnEntryFromAsync(Trigger.AddressAddedNextPhone, NextStateQueryAsync)
            .OnEntryFromAsync(Trigger.EmptyPhone, NextStateQueryAsync)
            .Permit(Trigger.SendedSms, State.SmsPhone)
            .Permit(Trigger.BackFromPhone, State.CommandOrders);

            _machine.Configure(State.SmsPhone)
            .SubstateOf(State.OrderPhone)
            .Permit(Trigger.EnteredSms, State.CommandOrders);


        }

        private async Task CatalogAfterSetRoleAsync()
        {
            await NextStateMessageAsync();
            await _machine.FireAsync(Trigger.CommandShopCatalogStarted);
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

        /// <summary>
        /// восстановление состояния
        /// </summary>
        /// <param name="userState"></param>
        /// <returns></returns>
        private async Task StateRecoveryAsync(ApplicationContext dataSource)
        {
            UserState userState = await dataSource.UserStates
                    .FirstOrDefaultAsync(us => us.UserId == ChatId);

            if (userState.IsNull())
            {
                CurrentState = State.New;
                Roles = new() { RolesList.User };
            }
            else
            {
                Roles = dataSource
                    .GetUserRoles(ChatId)
                    .ToHashSet();

                CurrentState = (State)userState.StateId;
                _lastMessageId = userState.LastMessageId;

                RecoverHandlers(userState.Data);
            }
        }

        private void RecoverHandlers(string data)
        {
            if (data.IsNotNullOrEmpty())
            {
                try
                {
                    UserConversations conversations = JsonConvert.DeserializeObject<UserConversations>(data);
                    _handlers = conversations.GetHandlers(this);
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

        public static async Task<MenuBotStateManager> CreateAsync(ITelegramBotClient botClient,
            IDbContextFactory<ApplicationContext> contextFactory, MenuBotCommandsManager commandsManager, long chatId)
        {
            await using ApplicationContext dataSource = await contextFactory.CreateDbContextAsync();
            MenuBotStateManager stateManager = new MenuBotStateManager(botClient, contextFactory, commandsManager, chatId);

            await stateManager.StateRecoveryAsync(dataSource);

            return stateManager;
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
       
        #endregion Public Methods
    }

    public enum Trigger
    {
        CommandStartStarted,
        CommandCartStarted,
        CommandShopCatalogStarted,
        Ignore,
        RoleSet,
        ChooseCategory,
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
