using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Robox.Telegram.Util.Core.Exceptions
{
    public class PayException : Exception
    {
        public PayException(int code) : base()
        {
            Code = code;
        }

        public int Code { get; set; }
    }
}
