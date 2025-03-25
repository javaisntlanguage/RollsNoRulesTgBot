using MessageContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitClient
{
    public interface IIdempotentConsumer<TMessage, TConsumer> 
        where TMessage : IMessage
        where TConsumer : IConsumer<TMessage>
    {
        Task HandleAsync(TMessage message);
    }
}
