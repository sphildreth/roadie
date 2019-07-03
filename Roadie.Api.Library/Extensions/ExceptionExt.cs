using Newtonsoft.Json;
using System;

namespace Roadie.Library.Extensions
{
    public static class ExceptionExt
    {
        public static string Serialize(this Exception input)
        {
            if (input == null) return null;
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            return JsonConvert.SerializeObject(input, Formatting.Indented, settings);
        }
    }
}