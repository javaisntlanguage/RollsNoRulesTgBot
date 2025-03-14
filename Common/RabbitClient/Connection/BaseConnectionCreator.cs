using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitClient.Connection
{
    public class BaseConnectionCreator : IBaseConnectionCreator
    {
        private readonly RabbitMqSettings _rabbitMqSettings;

        public BaseConnectionCreator(IOptions<RabbitMqSettings> options)
        {
            _rabbitMqSettings = options.Value;
        }

        public IConnection Create()
        {
            ConnectionFactory factory = new ConnectionFactory { HostName = _rabbitMqSettings.HostName };
            IConnection connection = factory.CreateConnection();
            return connection;
        }
    }
}
