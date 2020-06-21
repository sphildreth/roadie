using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library
{
    public sealed class RoadieProcessingException : Exception
    {
        public RoadieProcessingException() : base()
        {
        }

        public RoadieProcessingException(string message) 
            : base(message)
        {
        }

        public RoadieProcessingException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
    }
}
