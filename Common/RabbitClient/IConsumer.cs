using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitClient
{
    public interface IConsumer
    {
        public Task ConsumeAsync(string message);
    }
}
