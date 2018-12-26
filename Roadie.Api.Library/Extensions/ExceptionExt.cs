using System;

namespace Roadie.Library.Extensions
{
    public static class ExceptionExt
    {
        public static string Serialize(this Exception input)
        {
            if (input == null)
            {
                return null;
            }
            var settings = new Newtonsoft.Json.JsonSerializerSettings
            {
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
            };
            return Newtonsoft.Json.JsonConvert.SerializeObject(input, Newtonsoft.Json.Formatting.Indented, settings);
        }
    }
}