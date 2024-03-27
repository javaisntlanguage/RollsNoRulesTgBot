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

namespace MenuTgBot.Infrastructure
{
    internal class StateManager
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly ITelegramBotClient _botClient;
        private readonly ApplicationContext _dataSource;
        private readonly StateMachine<State, Trigger> _machine;
        private Dictionary<string, IConversation> _handlers;
        private CommandsManager _commandsManager;
        private Message _message;
        private CallbackQuery _query;
        private int? _lastMessageId;

        public State CurrentState { get; private set; }
        public long ChatId { get; private set; }
        public HashSet<RolesList> Roles { get; set; }


        private StateManager(ITelegramBotClient botClient, ApplicationContext dataSource, CommandsManager commandsManager, long chatId)
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
            _dataSource = dataSource;
            ConfigureMachine();
            ConfigureHandlers();
        }
        #region Private Methods

        /// <summary>
        /// назначение классов-обработчиков команд
        /// </summary>
        private void ConfigureHandlers()
        {
            SetHandler(new StartConversation(_dataSource, this));
            SetHandler(new CatalogConversation(_dataSource, this));
            SetHandler(new CartConversation(_dataSource, this));
            SetHandler(new OrdersConversation(_dataSource, this));
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
            .Permit(Trigger.CommandAdminStarted, State.CommandAdmin)
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

            _machine.Configure(State.CommandAdmin)
            .SubstateOf(State.New)
            .Permit(Trigger.EnterLogin, State.AdminLogin)
            .OnEntryFromAsync(Trigger.CommandAdminStarted, NextStateMessageAsync);

            _machine.Configure(State.AdminLogin)
            .SubstateOf(State.CommandAdmin)
            .OnEntryFromAsync(Trigger.EnterLogin, NextStateMessageAsync);

            _machine.Configure(State.AdminPassword)
            .SubstateOf(State.CommandAdmin)
            .OnEntryFromAsync(Trigger.EnterPassword, NextStateMessageAsync);

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
            .Permit(Trigger.AddressAdded, State.OrderPhone)
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
            .SubstateOf(State.CommandOrders);

            _machine.Configure(State.OrderPhone)
            .SubstateOf(State.CommandOrders)
            .OnEntryFromAsync(Trigger.AddressAdded, NextStateQueryAsync)
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
        private async Task SaveStateAsync()
        {
            string data = JsonConvert.SerializeObject(new
            {
                Cart = GetHandler<CartConversation>(),
                Orders = GetHandler<OrdersConversation>()
            });

            await _dataSource.UserStates_Set(ChatId, (int)CurrentState, data, _lastMessageId);
        }

        /// <summary>
        /// восстановление состояния
        /// </summary>
        /// <param name="userState"></param>
        /// <returns></returns>
        private async Task StateRecoveryAsync()
        {
            UserState userState = await _dataSource.UserStates
                    .FirstOrDefaultAsync(us => us.UserId == ChatId);

            if (userState.IsNull())
            {
                CurrentState = State.New;
                Roles = new() { RolesList.User };
            }
            else
            {
                Roles = _dataSource
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
                    _handlers = conversations.GetHandlers(_dataSource, this);
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

        public static async Task<StateManager> Create(ITelegramBotClient botClient,
            string connectionString, CommandsManager commandsManager, long chatId)
        {
            ApplicationContext dataSource = new ApplicationContext(connectionString);

            StateManager stateManager = new StateManager(botClient, dataSource, commandsManager, chatId);
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

            if (_lastMessageId.IsNotNull() && _query.Message.MessageId != _lastMessageId)
            {
                throw new NotLastMessageException();
            }

            bool result = await _handlers.Values
                .ToAsyncEnumerable()
                .AnyAwaitAsync(async x => await CallTriggerAsync(await x.TryNextStepAsync(query)));

            await SaveStateAsync();
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

        public async Task<Message> SendMessageAsync(string text, ParseMode? parseMode = null, IReplyMarkup? replyMarkup = null, string photo = null)
        {
            Message result = null;
            if (photo.IsNullOrEmpty())
            {
                result = await _botClient.SendTextMessageAsync(ChatId, text, parseMode: parseMode, replyMarkup: replyMarkup);
            }
            else
            {
                await using Stream stream = new MemoryStream(Convert.FromBase64String(photo));
                InputFileStream inputFile = InputFile.FromStream(stream);
                result = await _botClient.SendPhotoAsync(ChatId, inputFile, caption: text, parseMode: parseMode, replyMarkup: replyMarkup);
            }

            _lastMessageId = result.MessageId;

            return result;
        }

        public async Task<Message> EditMessageReplyMarkupAsync(int messageId, InlineKeyboardMarkup replyMarkup)
        {
            return await _botClient.EditMessageReplyMarkupAsync(ChatId, messageId, replyMarkup);
        }

        public async Task<Message> ShowButtonMenuAsync(string text, IEnumerable<KeyboardButton> additionalButtons = null)
        {
            Message result = await _commandsManager.ShowButtonMenuAsync(ChatId, text, additionalButtons);
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
        CommandAdminStarted,
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
        AddressAdded,
        EmptyPhone,
        BackFromPhone,
        ChangePhone,
        SendedSms,
        EnteredSms,
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
        CommandAdmin,
        AdminLogin,
        AdminPassword,
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
