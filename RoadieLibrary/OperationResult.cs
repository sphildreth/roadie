using System;
using System.Collections.Generic;

namespace Roadie.Library
{
    public sealed class OperationResult<T>
    {
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