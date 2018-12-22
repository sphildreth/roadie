using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Roadie.Library.Utility
{
    public class LookupSerializer : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            var result = objectType.GetInterfaces().Any(a => a.IsGenericType
                && a.GetGenericTypeDefinition() == typeof(ILookup<,>));
            return result;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var obj = new JObject();
            var enumerable = (IEnumerable)value;

            foreach (object kvp in enumerable)
            {
                // TODO: caching
                var keyProp = kvp.GetType().GetProperty("Key");
                var keyValue = keyProp.GetValue(kvp, null);

                obj.Add(keyValue.ToString(), JArray.FromObject((IEnumerable)kvp));
            }

            obj.WriteTo(writer);
        }
    }
}
