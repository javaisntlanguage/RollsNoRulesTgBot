using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Util.Core.Interfaces;

namespace Telegram.Util.Core
{
    public class TelegramClientBuilder
    {
        public IEnumerable<IBotCommandHandler> BotCommandHandlers { get; set; }
        public IEnumerable<BotCommand> BotCommands { get; set; }
        private ITelegramBotClient _bot { get; set; }

        /// <summary>
        /// Инициализация клиента телеграм
        /// </summary>
        /// <param name="telegramBotClient"></param>
        /// <returns></returns>
        public TelegramClientBuilder InitTelegramBotClient(ITelegramBotClient telegramBotClient)
        {
            _bot = telegramBotClient;
            return this;
        }
        /// <summary>
        /// Инициализация команд меню
        /// </summary>
        /// <param name="commands"></param>
        /// <returns></returns>
        public TelegramClientBuilder InitMenuCommands(IEnumerable<IBotCommandHandler> commands)
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
        public TelegramClientBuilder SetMenuCommands()
        {
            _bot.SetMyCommandsAsync(BotCommands).ConfigureAwait(false);
            return this;
        }
    }
}
