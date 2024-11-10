using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Util.Core.Interfaces;

namespace MenuTgBot.Infrastructure.Interfaces
{
	internal interface IMenuBotStateManagerFactory : IStateManagerFactory<MenuBotStateManager>
	{
	}
}
