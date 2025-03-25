using MessageContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitClient
{
    public interface IConsumer<TMessage> where TMessage : IMessage
    {
        public Task ConsumeAsync(TMessage message);
    }
}
