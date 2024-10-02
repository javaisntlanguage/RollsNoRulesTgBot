using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;

namespace Telegram.Util.Core.Interfaces
{
	public interface ITelegramBotClientBuilder
	{
		ITelegramBotClient Build();
	}
}
