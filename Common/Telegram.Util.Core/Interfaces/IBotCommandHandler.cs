using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Util.Core.Enums;

namespace Telegram.Util.Core.Interfaces
{
    public interface IBotCommandHandler
    {
        public string Command { get; }
        public CommandDisplay DisplayMode { get; set; }

        public Task StartCommandAsync(object stateManager, Message message);
    }
}
