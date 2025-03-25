using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitClient.Exceptions
{
    public class MessageHandlingException : Exception
    {
        public MessageHandlingException(string message) : base(message) { }
        public MessageHandlingException(string message, Exception ex) : base(message, ex) { }
    }
}
