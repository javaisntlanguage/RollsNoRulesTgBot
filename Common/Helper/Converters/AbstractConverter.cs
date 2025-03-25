using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helper.Converters
{
    public class AbstractConverter<TAbstract, TReal>
    : JsonConverter where TReal : TAbstract
    {
        public override Boolean CanConvert(Type objectType)
            => objectType == typeof(TAbstract);

        public override Object? ReadJson(JsonReader reader, Type type, object value, JsonSerializer jsonSerializer)
            => jsonSerializer.Deserialize<TReal>(reader);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer jsonSerializer)
            => jsonSerializer.Serialize(writer, value);
    }
}
