using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Util.Core.Models;

namespace MenuTgBot.Infrastructure.Models
{
	internal class MenuBotSettings : TelegramSettings
	{
		public string SmsLength { get; set; }
	}
}
