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

namespace AdminTgBot.Infrastructure
{
    internal class StateManager
    {
        private readonly ApplicationContext _dataSource;
        private readonly StateMachine<State, Trigger> _machine;
        private readonly ITelegramBotClient _botClient;
        private Dictionary<string, IConversation> _handlers;
        private int? _lastMessageId;
        private Message _message;
        private CallbackQuery _query;
        private State _state;

        public static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public long UserId { get; private set; }
        public long ChatId { get; private set; }
        public bool IsAuth { get; set; }
        public HashSet<RolesList> Roles { get; set; }

        private StateManager(ITelegramBotClient botClient, ApplicationContext dataSource, CommandsManager commandsManager,
            long userId, long chatId)
        {
            UserId = userId;
            ChatId = chatId;

            _message = new Message();
            CommandsManager = commandsManager;
            _machine = new StateMachine<State, Trigger>(() => _state,
                s =>
                {
                    _state = s;
                });
            _handlers = new Dictionary<string, IConversation>();
            _botClient = botClient;
            _dataSource = dataSource;

            ConfigureMachine();
            ConfigureHandlers();
        }

        public CommandsManager CommandsManager { get; }

        #region Private Methods

        /// <summary>
        /// назначение классов-обработчиков команд
        /// </summary>
        private void ConfigureHandlers()
        {
            SetHandler(new StartConversation(_botClient, _dataSource, this));
            SetHandler(new CatalogEditorConversation(_dataSource, this));
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
            .Permit(Trigger.CommandCatalogEditorStarted, State.CommandCatalogEditor)
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
        }

        /// <summary>
        /// сохранить состояние пользователя
        /// </summary>
        /// <returns></returns>
        private async Task SaveStateAsync()
        {
            string data = JsonConvert.SerializeObject(new
            {
                Start = GetHandler<StartConversation>(),
                CatalogEditor = GetHandler<CatalogEditorConversation>()
            });

            await _dataSource.SetAdminState(UserId, (int)_state, data, _lastMessageId);
        }

        /// <summary>
        /// восстановление состояния
        /// </summary>
        /// <param name="userState"></param>
        /// <returns></returns>
        private async Task StateRecoveryAsync()
        {
            AdminState adminState = await _dataSource.AdminStates
                    .FirstOrDefaultAsync(us => us.UserId == UserId);

            if (adminState.IsNull())
            {
                _state = State.New;
                Roles = new() { RolesList.User };
            }
            else
            {
                Roles = _dataSource
                    .GetUserRoles(UserId)
                    .ToHashSet();

                _state = (State)adminState.StateId;
                _lastMessageId = adminState.LastMessageId;

                IsAuth = !_machine.IsInState(State.CommandStart) && _state != State.New;

                RecoverHandlers(adminState.Data);
            }
        }

        private void RecoverHandlers(string data)
        {
            if (data.IsNotNullOrEmpty())
            {
                try
                {
                    AdminConversations conversations = JsonConvert.DeserializeObject<AdminConversations>(data);
                    _handlers = conversations.GetHandlers(_botClient, _dataSource, this);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Не удалось восстановить пользователя {UserId}. Data={data}", ex);
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

        public async Task<string> GetFileAsync(string fileId)
        {
            await using MemoryStream stream = new MemoryStream();
            await _botClient.GetInfoAndDownloadFileAsync(fileId, stream);
            stream.Position = 0;
            byte[] file = stream.ToArray();
            string result = Convert.ToBase64String(file);

            return result;
        }

        public static async Task<StateManager> CreateAsync(ITelegramBotClient botClient, 
            string connectionString, CommandsManager commandsManager,long userId, long chatId)
        {
            ApplicationContext dataSource = new ApplicationContext(connectionString);

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

#if !DEBUG
            CheckAuth();
#endif

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

#if !DEBUG
            CheckAuth();
#endif

            if(_lastMessageId.IsNotNull() && _query.Message.MessageId != _lastMessageId)
            {
                return false;
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

        public State GetState()
        {
            return _state;
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
    }
}
