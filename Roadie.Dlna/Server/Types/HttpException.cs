using System;
using System.Runtime.Serialization;

namespace Roadie.Dlna.Server
{
    [Serializable]
    public class HttpException : Exception
    {
        public HttpException()
        {
        }

        public HttpException(string message)
      : base(message)
        {
        }

        public HttpException(string message, Exception innerException)
      : base(message, innerException)
        {
        }

        protected HttpException(SerializationInfo info, StreamingContext context)
                              : base(info, context)
        {
        }
    }
}