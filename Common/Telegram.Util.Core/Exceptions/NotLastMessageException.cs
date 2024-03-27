using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.Enums;

namespace Telegram.Util.Core.Exceptions
{
    public class NotLastMessageException : Exception
    {
        public NotLastMessageException(): base()
        {

        }
    }
}
