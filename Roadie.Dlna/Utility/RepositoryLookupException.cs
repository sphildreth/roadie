using System;
using System.Runtime.Serialization;

namespace Roadie.Dlna.Utility
{
    [Serializable]
    public sealed class RepositoryLookupException : ArgumentException
    {
        public string Key { get; private set; }

        public RepositoryLookupException()
        {
        }

        public RepositoryLookupException(string key)
      : base($"Failed to lookup {key}")
        {
            Key = key;
        }

        public RepositoryLookupException(string message, Exception inner)
      : base(message, inner)
        {
        }

        public RepositoryLookupException(string message, string paramName) : base(message, paramName)
        {
        }

        public RepositoryLookupException(string message, string paramName, Exception innerException) : base(message, paramName, innerException)
        {
        }

        private RepositoryLookupException(SerializationInfo info,
                                                      StreamingContext context)
      : base(info, context)
        {
        }
    }
}