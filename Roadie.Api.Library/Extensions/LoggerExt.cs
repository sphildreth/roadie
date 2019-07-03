using System;

namespace Microsoft.Extensions.Logging
{
    public static class LoggerExtensions
    {
        public static void LogError(this ILogger logger, Exception ex)
        {
            logger.LogError(default(EventId), ex, null, null);
        }
    }
}