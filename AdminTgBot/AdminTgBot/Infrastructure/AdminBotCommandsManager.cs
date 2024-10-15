using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot;
using Helper;
using Telegram.Util.Core;
using Database;
using Database.Tables;
using Microsoft.EntityFrameworkCore;
using Database.Enums;
using AdminTgBot.Infrastructure.Conversations.Start;
using AdminTgBot.Infrastructure.Conversations.CatalogEditor;
using AdminTgBot.Infrastructure.Commands;
using Telegram.Util.Core.Enums;
using Telegram.Util.Core.Interfaces;
using Microsoft.EntityFrameworkCore.Internal;
using AdminTgBot.Infrastructure.Models;
using AdminTgBot.Infrastructure.Interfaces;
using Microsoft.Extensions.Options;
using Telegram.Util.Core.Models;

namespace AdminTgBot.Infrastructure
{
    internal class AdminBotCommandsManager : CommandsManager
    {
		public AdminBotCommandsManager(ITelegramBotClient botClient,
			IMenuHandler menuHandler,
			IAdminStateManagerFactory stateManagerFactory,
            IOptions<AdminSettings> options) : base(botClient, menuHandler, stateManagerFactory)
        {
        }
	}
}
