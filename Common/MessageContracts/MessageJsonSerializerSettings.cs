using Helper.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageContracts
{
    public class MessageJsonSerializerSettings : JsonSerializerSettings
    {
        public MessageJsonSerializerSettings()
        {
            Converters =
            [
                new AbstractConverter<IOrder, Order>()
            ];
        }
    }
}
