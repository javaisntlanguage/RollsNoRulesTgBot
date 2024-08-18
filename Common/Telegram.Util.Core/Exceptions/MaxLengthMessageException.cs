using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telegram.Util.Core.Exceptions
{
	public class MaxLengthMessageException : Exception
	{
		public MaxLengthMessageException(int? maxLength) : base()
		{
			MaxLength = maxLength;
		}

		public int? MaxLength { get; }
	}
}
