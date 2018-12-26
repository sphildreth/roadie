using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library
{
    [Serializable]
    public class EventMessage 
    {
        public Microsoft.Extensions.Logging.LogLevel Level { get; set; } = Microsoft.Extensions.Logging.LogLevel.Trace;
        public string Message { get; set; }
    }

}
