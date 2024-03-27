using Telegram.Bot.Types;
using Database.Enums;
using Telegram.Util.Core.Enums;
using Helper;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;

namespace Telegram.Util.Core
{
    public abstract class BotCommandHandler<TStateManager> where TStateManager : class
    {
        protected BotCommandHandler(string command, CommandDisplay displayMode)
        {
            Command = command;
            DisplayMode = displayMode;
        }

        public string Command { get; }
        public CommandDisplay DisplayMode { get; set; }

        public virtual async Task StartCommandAsync(object stateManager, Message message)
        {
            CheckStateManager(stateManager);
        }

        private void CheckStateManager(object stateManager)
        {
            if (stateManager is not TStateManager)
            {
                throw new Exception($"объект должен быть {typeof(TStateManager).FullName}");
            }
        }

        protected string SetCommand()
        {
            return "/" + Command.ToLower();
        }
    }
}
