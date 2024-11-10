using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telegram.Util.Core.Exceptions
{
	public class CustomMessageException : Exception
	{
		public CustomMessageException(string message, string? logErrorMessage = null)
			: base("Ошибка пользователю: \"" + message + 
				  "\". Ошибка в лог: \"" + logErrorMessage + "\"")
		{
			UserMessage = message;
			LogErrorMessage = logErrorMessage;
		}

		public string UserMessage { get; }
		public string? LogErrorMessage { get; }
	}
}
