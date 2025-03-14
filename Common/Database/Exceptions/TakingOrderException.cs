using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Exceptions
{
    public class TakingOrderException : Exception
    {
        public TakingOrderException(string message) : base(message) { }
    }
}
