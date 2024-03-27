using Telegram.Bot;
using Telegram.Bot.Types;

namespace Telegram.Util.Core
{
    public class TelegramBotClientBuilder<TStateManager> where TStateManager : class
    {
        public IEnumerable<BotCommandHandler<TStateManager>> BotCommandHandlers { get; set; }
        public IEnumerable<BotCommand> BotCommands { get; set; }
        private ITelegramBotClient _bot { get; set; }

        /// <summary>
        /// Инициализация клиента телеграм
        /// </summary>
        /// <param name="telegramBotClient"></param>
        /// <returns></returns>
        public TelegramBotClientBuilder<TStateManager> InitTelegramBotClient(ITelegramBotClient telegramBotClient)
        {
            _bot = telegramBotClient;
            return this;
        }
        /// <summary>
        /// Инициализация команд меню
        /// </summary>
        /// <param name="commands"></param>
        /// <returns></returns>
        public TelegramBotClientBuilder<TStateManager> InitMenuCommands(IEnumerable<BotCommandHandler<TStateManager>> commands)
        {
            BotCommandHandlers = commands;

            BotCommands = BotCommandHandlers
                .Select(bc => new BotCommand 
                { 
                    Command = bc.Command, 
                    Description = string.Empty
                });
            return this;
        }

        /// <summary>
        /// Активация команд меню
        /// </summary>
        /// <returns></returns>
        public TelegramBotClientBuilder<TStateManager> SetMenuCommands()
        {
            _bot.SetMyCommandsAsync(BotCommands).ConfigureAwait(false);
            return this;
        }
    }
}
