using Database.Migrations;
using Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Util.Core.Enums;
using Telegram.Util.Core.Interfaces;

namespace Telegram.Util.Core.Models
{
	public class MenuHandler : IMenuHandler
	{

		public MenuHandler(IBotCommandHandler[] commands) 
		{
			Commands = commands;
		}

		public IBotCommandHandler[] Commands { get; set; }

		public List<IEnumerable<KeyboardButton>> GetDefaultMenuButtons()
		{
			int rows = Commands.Length / 3;
			rows = rows == 0 ? 1 : rows;

			List<IEnumerable<KeyboardButton>> result = Commands
					.Where(command => command.DisplayMode == CommandDisplay.ButtonMenu)
					.Select(command =>
						new KeyboardButton(command.Command))
					.ToArray()
					.Split(rows)
					.ToList();

			return result;
		}
	}
}
