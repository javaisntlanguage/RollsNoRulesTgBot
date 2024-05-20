using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Util.Core;
using Telegram.Util.Core.Enums;

namespace MenuTgBot.Infrastructure
{
    internal class MenuBotCommandHandler : BotCommandHandler<MenuBotStateManager>
    {
        public MenuBotCommandHandler(string command, CommandDisplay displayMode) : base(command, displayMode)
        {
        }
    }
}
