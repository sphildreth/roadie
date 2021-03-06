﻿using System.Collections.Generic;

namespace Roadie.Library.Models.ThirdPartyApi.Subsonic
{
    public class SubsonicFileOperationResult<T> : FileOperationResult<T>
    {
        public ErrorCodes ErrorCode { get; set; }

        public SubsonicFileOperationResult()
        {
        }

        public SubsonicFileOperationResult(string message)
            : base(message)
        {
        }

        public SubsonicFileOperationResult(bool isNotFoundResult, string message)
            : base(isNotFoundResult, message)
        {
        }

        public SubsonicFileOperationResult(IEnumerable<string> messages = null)
            : base(messages)
        {
        }

        public SubsonicFileOperationResult(bool isNotFoundResult, IEnumerable<string> messages = null)
            : base(isNotFoundResult, messages)
        {
        }

        public SubsonicFileOperationResult(ErrorCodes error, string message = null)
            : base(message)
        {
            ErrorCode = error;
        }
    }
}