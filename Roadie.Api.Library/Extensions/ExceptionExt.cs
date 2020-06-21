using System;
using System.Text.Json;

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
            return JsonSerializer.Serialize(input, new JsonSerializerOptions
            {
                IgnoreNullValues = true,
                WriteIndented = true
            });
        }
    }
}