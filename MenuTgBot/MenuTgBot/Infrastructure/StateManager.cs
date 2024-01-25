using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;
using Robox.Telegram.Util.Core.StateMachine;
using MenuTgBot.Infrastructure.Conversations;
using Robox.Telegram.Util.Core;
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

namespace MenuTgBot.Infrastructure
{
    internal class StateManager
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly ITelegramBotClient _botClient;
        private readonly ApplicationContext _dataSource;
        private readonly StateMachine<State, Trigger> _machine;
        private Dictionary<string, IConversation> _handlers;

        public long UserId { get; private set; }
        public long ChatId { get; private set; }
        public IEnumerable<RolesList> Roles { get; set; }
        private Message _message;
        private CallbackQuery _query;
        private State _state;

        private StateManager(ITelegramBotClient botClient, ApplicationContext dataSource, CommandsManager commandsManager,
            long userId, long chatId)
        {
            UserId = userId;
            ChatId = chatId;

            _message = new Message();
            CommandsManager = commandsManager;
            Commands = CommandsManager.Commands.ToDictionary(command => command.Conversation.Name, command => command);
            _machine = new StateMachine<State, Trigger>(() => _state,
                s =>
                {
                    _state = s;
                });
            _handlers = new Dictionary<string, IConversation>();
            _botClient = botClient;
            _dataSource = dataSource;
            ConfigureCommands();
            ConfigureMachine();
            ConfigureHandlers();
        }

        public CommandsManager CommandsManager { get; }
        public Dictionary<string, BotCommandHandler> Commands { get; }

        #region Private Methods

        /// <summary>
        /// назначение классов-обработчиков команд
        /// </summary>
        private void ConfigureHandlers()
        {
            SetHandler(new StartConversation(_botClient, _dataSource, this));
            SetHandler(new ShopCatalogConversation(_botClient, _dataSource, this));
            SetHandler(new CartConversation(_botClient, _dataSource, this));
        }
        /// <summary>
        /// назначение обработчиков кнопок меню
        /// </summary>
        private void ConfigureCommands()
        {
            Commands[nameof(StartConversation)].TriggerAction = StartAsync;
            Commands[nameof(ShopCatalogConversation)].TriggerAction = ShopCalatogAsync;
            Commands[nameof(CartConversation)].TriggerAction = CartAsync;
            /*Commands[nameof(ShopContactsConversation)].TriggerAction = ShopContactsAsync;*/
        }

        /// <summary>
        /// конфигурация конечного автомата
        /// для всех новых триггеров добавлять:
        /// Permit (для обработки)
        /// Ignore (для пропуска)
        /// но добавлять одно из этого обязательно
        /// </summary>
        private void ConfigureMachine()
        {
            _machine.Configure(State.New)
            .Permit(Trigger.CommandStartStarted, State.CommandStart)
            .Permit(Trigger.CommandShopCatalogStarted, State.CommandShopCatalog)
            .Permit(Trigger.CommandCartStarted, State.CommandCart)
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
            .SubstateOf(State.New)
            .Permit(Trigger.ToCatalogState, State.CommandShopCatalog)
            .OnEntryFromAsync(Trigger.AddToCartFromCategoryShowProduct, NextStateQueryAsync)
            .OnEntryFromAsync(Trigger.DecreasedCountCart, NextStateQueryAsync)
            .OnEntryFromAsync(Trigger.IncreasedCountCart, NextStateQueryAsync);


            _machine.Configure(State.CommandCart)
            .SubstateOf(State.New)
            .OnEntryFromAsync(Trigger.CommandCartStarted, NextStateMessageAsync);
        }

        /// <summary>
        /// обработка команды /start
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private async Task StartAsync(Message message)
        {
            _message = message;

            await _machine.FireAsync(Trigger.CommandStartStarted);
        }

        /// <summary>
        /// обработка команды каталог
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private async Task ShopCalatogAsync(Message message)
        {
            _message = message;

            await _machine.FireAsync(Trigger.CommandShopCatalogStarted);
        }

        /// <summary>
        /// обработка команды Корзина
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private async Task CartAsync(Message message)
        {
            _message = message;

            await _machine.FireAsync(Trigger.CommandCartStarted);
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
        private async Task SaveStateAsync()
        {
            string data = JsonConvert.SerializeObject(new
            {
                Cart = GetHandler<CartConversation>()
            });

            await _dataSource.UserStates_Set(UserId, (int)_state, data, Roles);
        }

        /// <summary>
        /// восстановление состояния
        /// </summary>
        /// <param name="userState"></param>
        /// <returns></returns>
        private async Task StateRecoveryAsync()
        {
            UserState userState = await _dataSource.UserStates
                    .AsNoTracking()
                    .FirstOrDefaultAsync(us => us.UserId == UserId);

            if (userState.IsNull())
            {
                _state = State.New;
                Roles = new[] { RolesList.User };
            }
            else
            {
                Roles = await _dataSource.GetUserRoles(UserId);
                _state = (State)userState.StateId;

                await RecoverHandlersAsync();
            }
        }

        private async Task RecoverHandlersAsync()
        {
            string data = (await _dataSource.UserStates
                .AsNoTracking()
                .FirstOrDefaultAsync(us => us.UserId == UserId))?.Data;

            if (data.IsNotNullOrEmpty())
            {
                try
                {
                    UserConversations conversations = JsonConvert.DeserializeObject<UserConversations>(data);
                    _handlers = conversations.GetHandlers(_botClient, _dataSource, this);
                }
                catch (Exception ex)
                {
                    _logger.Error($"Не удалось восстановить пользователя {UserId}. Data={data}", ex);
                }
            }
        }

        internal State GetState()
        {
            return _state;
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
        /// удаление обработчика для пользователя
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private void RemoveHandler<T>() where T : IConversation
        {
            string key = typeof(T).Name;

            _handlers.RemoveIfExists(key);
        }

        #endregion Private Methods

        #region Public Methods

        public static async Task<StateManager> Create(ITelegramBotClient botClient, 
            ApplicationContext dataSource, CommandsManager commandsManager,long userId, long chatId)
        {
            StateManager stateManager = new StateManager(botClient, dataSource, commandsManager, userId, chatId);
            await stateManager.StateRecoveryAsync();

            return stateManager;
        }

        public async Task NextStateMessageAsync()
        {
            await NextStateAsync(_message);
        }

        public async Task NextStateQueryAsync()
        {
            await NextStateAsync(_query);
        }

        /// <summary>
        /// обработка сообщения
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<bool> NextStateAsync(Message message)
        {
            _message = message;
            _query = null!;

            bool result = await _handlers.Values
                .ToAsyncEnumerable()
                .AnyAwaitAsync(async x => await CallTriggerAsync(await x.TryNextStepAsync(message)));

            await SaveStateAsync();
            return result;
        }

        /// <summary>
        /// обработка запроса
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public async Task<bool> NextStateAsync(CallbackQuery query)
        {
            _message = null!;
            _query = query;

            bool result = await _handlers.Values
                .ToAsyncEnumerable()
                .AnyAwaitAsync(async x => await CallTriggerAsync(await x.TryNextStepAsync(query)));

            await SaveStateAsync();
            return result;
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
    }

    public enum State
    {
        New,
        CommandStart,
        CommandShopCatalog,
        Menu,
        CommandCart,
        CommandShopContacts,
        CatalogCartActions
    }
}
