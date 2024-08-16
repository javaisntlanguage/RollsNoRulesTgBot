using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telegram.Util.Core.StateMachine.Exceptions
{
	public class GuardException : Exception
	{
		public GuardException(string? message) : base(message)
		{
		}
	}
}
