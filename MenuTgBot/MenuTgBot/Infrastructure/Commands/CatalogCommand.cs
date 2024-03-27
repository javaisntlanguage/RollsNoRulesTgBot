using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Util.Core.Enums;

namespace MenuTgBot.Infrastructure.Commands
{
    internal class CatalogCommand : MenuBotCommandHandler
    {
        public CatalogCommand(string command, CommandDisplay displayMode) : base(command, displayMode)
        {
        }

        public override async Task StartCommandAsync(object stateManager, Message message)
        {
            await base.StartCommandAsync(stateManager, message);

            await (stateManager as StateManager).ShowCatalogAsync(message);
        }
    }
}
