using System;

namespace Roadie.Library
{
    [Serializable]
    public class AppException : Exception
    {
        public AppException(string message) : base(message)
        {
        }
    }
}