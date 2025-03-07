using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitClient.Connection
{
    public interface IBaseConnectionCreator
    {
        IConnection Create();
    }
}
