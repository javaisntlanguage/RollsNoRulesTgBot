using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Util.Core.StateMachine;
using Telegram.Util.Core;
using Database;
using Helper;
using Newtonsoft.Json;
using Database.Tables;
using Microsoft.EntityFrameworkCore;
using NLog;
using Database.Enums;
using AdminTgBot.Infrastructure.Conversations;
using AdminTgBot.Infrastructure.Models;
using AdminTgBot.Infrastructure.Conversations.Start;
using AdminTgBot.Infrastructure.Conversations.CatalogEditor;
using AdminTgBot.Infrastructure.Exceptions;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Security.AccessControl;
using File = Telegram.Bot.Types.File;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using Microsoft.EntityFrameworkCore.Internal;
using Newtonsoft.Json.Linq;
using AdminTgBot.Infrastructure.Conversations.Orders;
using System.Data;
using AdminTgBot.Infrastructure.Conversations.BotOwner;
using Database.Classes;
using Telegram.Util.Core.StateMachine.Exceptions;
using AdminTgBot.Infrastructure.Conversations.Lkk;

namespace AdminTgBot.Infrastructure
{
    internal class AdminBotStateManager : StateManager
    {
        private readonly IDbContextFactory<ApplicationContext> _contextFactory;
        private readonly StateMachine<State, Trigger> _machine;
        private Dictionary<string, IConversation> _handlers;
        private static readonly Dictionary<State, Guid> Rights;

		public State CurrentState { get; private set; }
		public bool IsAuth { get; set; }
        public int? AdminId { get; set; }

        static AdminBotStateManager()
        {
            Rights = new Dictionary<State, Guid>()
            {
                { State.CommandOrders, RightHelper.Orders },
            };

		}

        private AdminBotStateManager(ITelegramBotClient botClient, IDbContextFactory<ApplicationContext> contextFactory,
            AdminBotCommandsManager commandsManager, long chatId)
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
        protected override void ConfigureHandlers()
        {
            SetHandler(new StartConversation(this));
            SetHandler(new CatalogEditorConversation(this));
            SetHandler(new OrdersConversation(this));
            SetHandler(new BotOwnerConversation(this));
            SetHandler(new LkkConversation(this));
        }

        protected override void ConfigureMachine()
        {
            _machine.Configure(State.New)
            .Permit(Trigger.CommandButtonsStarted, State.CommandStart)
            .Permit(Trigger.CommandStartStarted, State.CommandStart)
            .Permit(Trigger.CommandCatalogEditorStarted, State.CommandCatalogEditor)
            .Permit(Trigger.CommandOrdersStarted, State.CommandOrders)
			.Permit(Trigger.CommandBotOwnerStarted, State.CommandBotOwner)
			.Permit(Trigger.CommandLkkStarted, State.CommandLkk)
            .Ignore(Trigger.Ignore);

            _machine.Configure(State.CommandButtons)
            .SubstateOf(State.New)
            .OnEntryFromAsync(Trigger.CommandButtonsStarted, NextStateMessageAsync)
            .Ignore(Trigger.Ignore);

			_machine.Configure(State.CommandStart)
            .SubstateOf(State.New) 
            .Permit(Trigger.EnterLogin, State.StartLogin)
            .Permit(Trigger.EnterPassword, State.StartPassword)
            .OnEntryFromAsync(Trigger.CommandStartStarted, NextStateMessageAsync);

            _machine.Configure(State.StartLogin)
            .SubstateOf(State.CommandStart);

            _machine.Configure(State.StartPassword)
            .SubstateOf(State.StartLogin)
            .Permit(Trigger.EndOfConversation, State.New);

            _machine.Configure(State.CommandCatalogEditor)
            .SubstateOf(State.New)
            .Permit(Trigger.EditProductName, State.ProductNameEditor)
            .Permit(Trigger.EditProductDescription, State.ProductDescriptionEditor)
            .Permit(Trigger.EditProductPrice, State.ProductPriceEditor)
            .Permit(Trigger.EditProductPhoto, State.ProductPhotoEditor)
            .Permit(Trigger.EnterProductName, State.NewProductNameEditor)
            .Permit(Trigger.EnterProductDescription, State.NewProductDescriptionEditor)
            .Permit(Trigger.EnterProductPrice, State.NewProductPriceEditor)
            .Permit(Trigger.EnterProductPhoto, State.NewProductPhotoEditor)
            .Permit(Trigger.EditCategoryName, State.CategoryNameEditor)
            .Permit(Trigger.EnterCategoryName, State.NewCategoryNameEditor)
            .OnEntryFromAsync(Trigger.CommandCatalogEditorStarted, NextStateMessageAsync);

            _machine.Configure(State.ProductNameEditor)
            .SubstateOf(State.CommandCatalogEditor)
            .Permit(Trigger.ReturnToProductAttributeEditor, State.CommandCatalogEditor);

            _machine.Configure(State.ProductDescriptionEditor)
            .SubstateOf(State.CommandCatalogEditor)
            .Permit(Trigger.ReturnToProductAttributeEditor, State.CommandCatalogEditor);

            _machine.Configure(State.ProductPriceEditor)
            .SubstateOf(State.CommandCatalogEditor)
            .Permit(Trigger.ReturnToProductAttributeEditor, State.CommandCatalogEditor);

            _machine.Configure(State.ProductPhotoEditor)
            .SubstateOf(State.CommandCatalogEditor)
            .Permit(Trigger.ReturnToProductAttributeEditor, State.CommandCatalogEditor);

            _machine.Configure(State.NewProductNameEditor)
            .SubstateOf(State.CommandCatalogEditor);

            _machine.Configure(State.NewProductDescriptionEditor)
            .SubstateOf(State.CommandCatalogEditor);

            _machine.Configure(State.NewProductPriceEditor)
            .SubstateOf(State.CommandCatalogEditor);

            _machine.Configure(State.NewProductPhotoEditor)
            .SubstateOf(State.CommandCatalogEditor)
            .Permit(Trigger.ReturnToCalatog, State.CommandCatalogEditor);

			_machine.Configure(State.CategoryNameEditor)
            .SubstateOf(State.CommandCatalogEditor)
            .Permit(Trigger.ReturnToCatalogEditor, State.CommandCatalogEditor);

			_machine.Configure(State.NewCategoryNameEditor)
		    .SubstateOf(State.CommandCatalogEditor)
			.Permit(Trigger.ReturnToCatalogEditor, State.CommandCatalogEditor);

			_machine.Configure(State.CommandOrders)
			.SubstateOf(State.New)
			.OnEntryFromAsync(Trigger.CommandOrdersStarted, NextStateMessageAsync)
            .Permit(Trigger.WaitForDateFrom, State.FilterDateFrom)
            .Permit(Trigger.WaitForDateTo, State.FilterDateTo)
            .Permit(Trigger.WaitForOrderFilterId, State.FilterId);

			_machine.Configure(State.FilterDateFrom)
			.SubstateOf(State.CommandOrders)
            .Permit(Trigger.BackToOrderFilter, State.CommandOrders);

			_machine.Configure(State.FilterDateTo)
			.SubstateOf(State.CommandOrders)
            .Permit(Trigger.BackToOrderFilter, State.CommandOrders);

			_machine.Configure(State.FilterId)
			.SubstateOf(State.CommandOrders)
            .Permit(Trigger.BackToOrderFilter, State.CommandOrders);

			_machine.Configure(State.CommandBotOwner)
			.SubstateOf(State.New)
			.OnEntryFromAsync(Trigger.CommandBotOwnerStarted, NextStateMessageAsync)
            .Permit(Trigger.SelectMenu, State.BotOwnerSelectMenu);

			_machine.Configure(State.BotOwnerSelectMenu)
			.SubstateOf(State.CommandBotOwner)
			.OnEntryFromAsync(Trigger.SelectMenu, NextStateMessageAsync)
            .Permit(Trigger.EnterLogin, State.BotOwnerEnterLogin)
            .PermitReentry(Trigger.SelectMenu);

			_machine.Configure(State.BotOwnerEnterLogin)
			.SubstateOf(State.CommandBotOwner)
            .Permit(Trigger.EnterPassword, State.BotOwnerEnterPassword)
            .Permit(Trigger.SelectMenu, State.BotOwnerSelectMenu);

			_machine.Configure(State.BotOwnerEnterPassword)
			.SubstateOf(State.CommandBotOwner)
            .Permit(Trigger.EnterAdminName, State.BotOwnerEnterAdminName)
			.Permit(Trigger.SelectMenu, State.BotOwnerSelectMenu);

            _machine.Configure(State.BotOwnerEnterAdminName)
            .SubstateOf(State.CommandBotOwner)
            .Permit(Trigger.SelectMenu, State.BotOwnerSelectMenu);

            _machine.Configure(State.CommandLkk)
            .SubstateOf(State.New)
			.OnEntryFromAsync(Trigger.CommandLkkStarted, NextStateMessageAsync)
;
		}

		/// <summary>
		/// сохранить состояние пользователя
		/// </summary>
		/// <returns></returns>
		private async Task SaveStateAsync(ApplicationContext dataSource)
        {
            string data = JsonConvert.SerializeObject(new AdminConversations
            {
                Start = GetHandler<StartConversation>()!,
                CatalogEditor = GetHandler<CatalogEditorConversation>()!,
                Orders = GetHandler<OrdersConversation>()!,
				BotOwner = GetHandler<BotOwnerConversation>()!,
				Lkk = GetHandler<LkkConversation>()!,
            });

            await dataSource.SetAdminState(ChatId, (int)CurrentState, data, _lastMessageId);
        }

        /// <summary>
        /// восстановление состояния
        /// </summary>
        /// <param name="userState"></param>
        /// <returns></returns>
        private async Task StateRecoveryAsync(ApplicationContext dataSource)
        {
            AdminState? adminState = await dataSource.AdminStates
                    .FirstOrDefaultAsync(us => us.UserId == ChatId);

            if (adminState == null)
            {
                CurrentState = State.New;
            }
            else
            {
                CurrentState = (State)adminState.StateId;
                _lastMessageId = adminState.LastMessageId;

                IsAuth = !_machine.IsInState(State.CommandStart) && CurrentState != State.New;

                RecoverHandlers(adminState.Data, dataSource);

				StartConversation start = GetHandler<StartConversation>()!;
                AdminId = start.AdminId;
            }
        }

        private void RecoverHandlers(string data, ApplicationContext dataSource)
        {
            if (data.IsNotNullOrEmpty())
            {
                try
                {
                    AdminConversations conversations = JsonConvert.DeserializeObject<AdminConversations>(data)!;

                    _handlers = conversations.GetHandlers(dataSource, this);
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
                await _machine.FireAsync(trigger!.Value);
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
        /// сообщения вне очереди
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
		private bool IsOutOfQueue(CallbackQuery query)
		{
			JObject data = JObject.Parse(query.Data!);
            return data["OutOfQueue"]?.Value<bool?>() ?? false;
		}

		private bool HasRight(Guid right)
		{
			if(!AdminId.HasValue)
            {
                return false;
            }

			using ApplicationContext dataSource = _contextFactory.CreateDbContext();
            return dataSource.HasRight(AdminId.Value, right);
		}

		private void CheckRights()
		{
            if(Rights.TryGetValue(CurrentState, out Guid right))
            {
                if(!HasRight(right))
                {
                    throw new GuardException($"Нет прав. AdminId={AdminId} State={CurrentState}, Right={right}");
                }
            }
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
        /// обработка команды Редактор товаров
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task CatalogEditorAsync(Message message)
        {
            _message = message;

            await _machine.FireAsync(Trigger.CommandCatalogEditorStarted);
        }
        /// <summary>
        /// обработка команды Заказы
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task OrdersAsync(Message message)
        {
            _message = message;

            await _machine.FireAsync(Trigger.CommandOrdersStarted);
        }
        /// <summary>
        /// обработка команды отображения кнопок
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task ButtonsAsync(Message message)
        {
            _message = message;

            await _machine.FireAsync(Trigger.CommandButtonsStarted);
        }

		/// <summary>
		/// обработка команды владельца бота
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		public async Task BotOwnerAsync(Message message)
		{
			_message = message;

			await _machine.FireAsync(Trigger.CommandBotOwnerStarted);
		}

		/// <summary>
		/// обработка команды личного кабинета админа
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		public async Task LkkAsync(Message message)
		{
			_message = message;

			await _machine.FireAsync(Trigger.CommandLkkStarted);
		}

		public async Task<string> GetFileAsync(string fileId)
        {
            await using MemoryStream stream = new MemoryStream();
            await _botClient.GetInfoAndDownloadFileAsync(fileId, stream);
            stream.Position = 0;
            byte[] file = stream.ToArray();
            string result = Convert.ToBase64String(file);

            return result;
        }

        public static async Task<AdminBotStateManager> CreateAsync(ITelegramBotClient botClient,
            IDbContextFactory<ApplicationContext> contextFactory, AdminBotCommandsManager commandsManager, long chatId)
        {
            await using ApplicationContext dataSource = await contextFactory.CreateDbContextAsync();

            AdminBotStateManager stateManager = new AdminBotStateManager(botClient, contextFactory, commandsManager, chatId);
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

#if !DEBUG
            CheckAuth();
#endif
            await using ApplicationContext dataSource = await _contextFactory.CreateDbContextAsync();

            CheckRights();

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

#if !DEBUG
            CheckAuth();
#endif

            await using ApplicationContext dataSource = await _contextFactory.CreateDbContextAsync();

			CheckRights();

			if (_lastMessageId.IsNotNull() && _query.Message!.MessageId != _lastMessageId)
            {
                if (!IsOutOfQueue(_query))
                {
                    return false;
                }
            }

            bool result = await _handlers.Values
                .ToAsyncEnumerable()
                .AnyAwaitAsync(async x => await CallTriggerAsync(await x.TryNextStepAsync(dataSource, query)));

            await SaveStateAsync(dataSource);
            return result;
        }

		/// <summary>
		/// получение обработчика
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T? GetHandler<T>() where T : IConversation
        {
            string key = typeof(T).Name;
            if (_handlers.ContainsKey(key)) return (T)_handlers[key];

            return default;
        }

        public void CheckAuth()
        {
            if(!IsAuth && !_machine.IsInState(State.CommandStart))
            {
                throw new AuthException();
            }
        }
		#endregion Public Methods
	}

    public enum Trigger
    {
        CommandStartStarted,
        EndOfConversation,
        EnterLogin,
        EnterPassword,
        Ignore,
        CommandCatalogEditorStarted,
        EditProductName,
        EditProductDescription,
        EditProductPrice,
        EditProductPhoto,
        ReturnToProductAttributeEditor,
        EnterProductName,
        EnterProductDescription,
        EnterProductPrice,
        EnterProductPhoto,
        ReturnToCalatog,
		CommandOrdersStarted,
		WaitForDateFrom,
		BackToOrderFilter,
		WaitForDateTo,
		WaitForOrderFilterId,
		CommandButtonsStarted,
		EditCategoryName,
		ReturnToCatalogEditor,
		EnterCategoryName,
		CommandBotOwnerStarted,
		EnterAdminName,
		SuggestConfirmAddSuperAdmin,
		SelectMenu,
		CommandLkkStarted,
	}

    public enum State
    {
        New,
        CommandStart,
        StartLogin,
        StartPassword,
        CommandCatalogEditor,
        ProductNameEditor,
        ProductDescriptionEditor,
        ProductPriceEditor,
        ProductPhotoEditor,
        NewProductNameEditor,
        NewProductDescriptionEditor,
        NewProductPriceEditor,
        NewProductPhotoEditor,
		CommandOrders,
		FilterDateFrom,
		FilterDateTo,
		FilterId,
		CommandButtons,
		CategoryNameEditor,
		NewCategoryNameEditor,
		CommandBotOwner,
		BotOwnerEnterLogin,
		BotOwnerEnterPassword,
		BotOwnerEnterAdminName,
		BotOwnerSelectMenu,
		CommandLkk,
	}
}
