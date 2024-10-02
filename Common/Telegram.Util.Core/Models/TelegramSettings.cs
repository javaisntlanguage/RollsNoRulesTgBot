using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telegram.Util.Core.Models
{
	public class TelegramSettings
	{
		public required string TelegramBotToken { get; set; }
		public int MessageTimeoutSec { get; set; }
	}
}
