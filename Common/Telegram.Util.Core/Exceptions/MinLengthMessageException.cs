using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telegram.Util.Core.Exceptions
{
	public class MinLengthMessageException : Exception
	{
		public MinLengthMessageException(int? minLength) : base()
		{
			MinLength = minLength;
		}

		public int? MinLength { get; }
	}
}
