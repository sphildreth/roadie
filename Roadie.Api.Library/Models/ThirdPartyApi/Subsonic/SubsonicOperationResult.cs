using System;
using System.Collections.Generic;

namespace Roadie.Library.Models.ThirdPartyApi.Subsonic
{
    [Serializable]
    public class SubsonicOperationResult<T> : OperationResult<T>
    {
        public ErrorCodes? ErrorCode { get; set; }

        public bool IsEmptyResponse { get; set; }

        public SubsonicOperationResult(bool isNotFoundResult, IEnumerable<string> messages = null)
                            : base(isNotFoundResult, messages)
        {
        }

        public SubsonicOperationResult()
        {
        }

        public SubsonicOperationResult(IEnumerable<string> messages = null)
            : base(messages)
        {
        }

        public SubsonicOperationResult(string message = null)
            : base(message)
        {
        }

        public SubsonicOperationResult(ErrorCodes error, string message = null)
            : base(message)
        {
            ErrorCode = error;
        }

        public SubsonicOperationResult(Exception error = null)
            : base(error)
        {
        }

        public SubsonicOperationResult(string message = null, Exception error = null)
            : base(message, error)
        {
        }
    }
}