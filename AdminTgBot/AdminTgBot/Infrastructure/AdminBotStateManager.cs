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
using AdminTgBot.Infrastructure.Conversations;
using AdminTgBot.Infrastructure.Models;
using AdminTgBot.Infrastructure.Conversations.Start;
using AdminTgBot.Infrastructure.Conversations.CatalogEditor;
using AdminTgBot.Infrastructure.Exceptions;
using Newtonsoft.Json.Linq;
using AdminTgBot.Infrastructure.Conversations.Orders;
using AdminTgBot.Infrastructure.Conversations.BotOwner;
using Database.Classes;
using Telegram.Util.Core.StateMachine.Exceptions;
using AdminTgBot.Infrastructure.Conversations.Lkk;
using AdminTgBot.Infrastructure.Conversations.Administration;
using Telegram.Util.Core.Interfaces;
using AdminTgBot.Infrastructure.Interfaces;
using Microsoft.Extensions.Options;
using Telegram.Util.Core.Exceptions;

namespace AdminTgBot.Infrastructure
{
    internal class AdminBotStateManager : StateManager
    {
        private readonly StateMachine<State, Trigger> _machine;
        private Dictionary<string, IConversation> _handlers;
		private AdminSettings _config;
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

        public AdminBotStateManager(ITelegramBotClient botClient, 
            IDbContextFactory<ApplicationContext> contextFactory,
			IMenuHandler menuHandler,
			IAdminStateMachineBuilder stateMachineBuilder,
            IOptions<AdminSettings> options,
			long chatId) : base(botClient, contextFactory, menuHandler)
		{
			_message = new Message();
            _machine = stateMachineBuilder.Build(
                stateAccessor: () => CurrentState,
                stateMutator: s =>
			    {
				    CurrentState = s;
			    }, 
                messageHandler: NextStateMessageAsync
            );
            _handlers = new Dictionary<string, IConversation>();
            _config = options.Value;

            ChatId = chatId;
		}
        #region Private Methods
        public override void ConfigureHandlers()
        {
            SetHandler(new StartConversation(this));
            SetHandler(new CatalogEditorConversation(this));
            SetHandler(new OrdersConversation(this));
            SetHandler(new BotOwnerConversation(this, _config));
            SetHandler(new LkkConversation(this));
            SetHandler(new AdministrationConversation(this));
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
				Administration = GetHandler<AdministrationConversation>()!,
            });

            await dataSource.SetAdminState(ChatId, (int)CurrentState, data, _lastMessageId);
        }

        /// <summary>
        /// восстановление состояния
        /// </summary>
        /// <param name="userState"></param>
        /// <returns></returns>
        public async Task StateRecoveryAsync(ApplicationContext dataSource)
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

        private void RecoverHandlers(string? data, ApplicationContext dataSource)
        {
            if (data.IsNotNullOrEmpty())
            {
                try
                {
                    AdminConversations conversations = JsonConvert.DeserializeObject<AdminConversations>(data!)!;

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
            if(Rights.TryGetValue(CurrentState, out Guid rightId))
            {
                if(!HasRight(rightId))
                {
                    throw new GuardException($"Нет прав. AdminId={AdminId} State={CurrentState}, Right={rightId}");
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

		/// <summary>
		/// обработка команды администрирования бота
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		internal async Task AdministrationAsync(Message message)
		{
			_message = message;

			await _machine.FireAsync(Trigger.CommandAdministrationStarted);
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
                    throw new NotLastMessageException();
                }
            }

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
        ReturnToCatalog,
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
		SuggestEditName,
		BackToLkk,
		SuggestEditPassword,
		EnterNewPassword,
		SuggestConfirmNewAdminPassword,
		CommandAdministrationStarted,
		EnterRightGroupName,
		BackToAdministration,
		EnterRightGroupDescription,
		BackToEnterRightGroupName,
		SellLocationEnterName,
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
		LkkEnterName,
		LkkEnterOldPassword,
		LkkEnterNewPassword,
		LkkConfirmNewPassword,
		CommandAdministration,
		AdministrationEnterRightGroupName,
		AdministrationEnterRightGroupDescription,
		CatalogEditorSellLocationEnterName,
	}
}
