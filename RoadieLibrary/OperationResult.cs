using System;
using System.Collections.Generic;
using System.Linq;

namespace Roadie.Library
{
    public class OperationResult<T>
    {
        public const string NoImageDataFound = "NO_IMAGE_DATA_FOUND";
        public const string NotModified = "NotModified";
        public const string OkMessage = "OK";

        public Dictionary<string, object> AdditionalData { get; set; }
        public T Data { get; set; }
        public IEnumerable<Exception> Errors { get; set; }
        public bool IsSuccess { get; set; }
        public IEnumerable<string> Messages { get; set; }
        public long OperationTime { get; set; }

        public OperationResult()
        {
            this.AdditionalData = new Dictionary<string, object>();
        }
    }
}