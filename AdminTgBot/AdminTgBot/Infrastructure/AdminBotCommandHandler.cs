using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Util.Core;
using Telegram.Util.Core.Enums;

namespace AdminTgBot.Infrastructure
{
    internal class AdminBotCommandHandler : BotCommandHandler<AdminBotStateManager>
    {
        public AdminBotCommandHandler(string command, CommandDisplay displayMode) : base(command, displayMode)
        {
        }
    }
}
