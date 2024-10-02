using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Util.Core.Interfaces;

namespace Telegram.Util.Core.Interfaces
{
	public interface IMenuHandler
	{
		public IBotCommandHandler[] Commands { get; set; }
		List<IEnumerable<KeyboardButton>> GetDefaultMenuButtons();
	}
}
