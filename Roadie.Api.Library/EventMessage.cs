using Microsoft.Extensions.Logging;
using System;

namespace Roadie.Library
{
    [Serializable]
    public class EventMessage
    {
        public LogLevel Level { get; set; } = LogLevel.Trace;
        public string Message { get; set; }
    }
}