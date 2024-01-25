using Telegram.Bot;
using Telegram.Bot.Types;

namespace Robox.Telegram.Util.Core
{
    public class TelegramBotClientBuilder
    {
        public IEnumerable<BotCommandHandler> BotCommandHandlers { get; set; }
        public IEnumerable<BotCommand> BotCommands { get; set; }
        private ITelegramBotClient _bot { get; set; }

        /// <summary>
        /// Инициализация клиента телеграм
        /// </summary>
        /// <param name="telegramBotClient"></param>
        /// <returns></returns>
        public TelegramBotClientBuilder InitTelegramBotClient(ITelegramBotClient telegramBotClient)
        {
            _bot = telegramBotClient;
            return this;
        }
        /// <summary>
        /// Инициализация команд меню
        /// </summary>
        /// <param name="commands"></param>
        /// <returns></returns>
        public TelegramBotClientBuilder InitMenuCommands(IEnumerable<BotCommandHandler> commands)
        {
            BotCommandHandlers = commands;

            BotCommands = BotCommandHandlers
                .Select(bc => new BotCommand 
                { 
                    Command = bc.Command, 
                    Description = bc.Description 
                });
            return this;
        }

        /// <summary>
        /// Активация команд меню
        /// </summary>
        /// <returns></returns>
        public TelegramBotClientBuilder SetMenuCommands()
        {
            _bot.SetMyCommandsAsync(BotCommands).ConfigureAwait(false);
            return this;
        }
    }
}
