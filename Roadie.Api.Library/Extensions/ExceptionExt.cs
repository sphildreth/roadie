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
            try
            {
                return JsonSerializer.Serialize(input, new JsonSerializerOptions
                {
                    IgnoreNullValues = true,
                    WriteIndented = true
                });
            }
            catch (Exception)
            {
                return input?.ToString();
            }
        }
    }
}