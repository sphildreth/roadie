using System;
using System.Collections.Generic;
using System.Linq;

namespace Roadie.Library
{
    public class OperationResult<T>
    {
        public const string Key = "Bearer ";
        public const string NewKey = "__new__";
        public const string NoImageDataFound = "NO_IMAGE_DATA_FOUND";
        public const string NotModified = "NotModified";
        public const string OkMessage = "OK";

        private List<string> _messages = new List<string>();
        private List<Exception> _errors = new List<Exception>();

        public Dictionary<string, object> AdditionalData { get; set; }
        public T Data { get; set; }
        public IEnumerable<Exception> Errors { get; set; }
        public bool IsSuccess { get; set; }
        public IEnumerable<string> Messages
        {
            get
            {
                return this._messages;
            }
        }
        public long OperationTime { get; set; }

        public OperationResult()
        {
        }

        public OperationResult(string message = null)
        {
            this.AdditionalData = new Dictionary<string, object>();
            this.AddMessage(message);
        }

        public OperationResult(Exception error = null)
        {
            this.AddError(error);
        }

        public OperationResult(string message = null, Exception error = null)
        {
            this.AddMessage(message);
            this.AddError(error);
        }

        public void AddMessage(string message)
        {
            if(!string.IsNullOrEmpty(message))
            {
                this._messages.Add(message);
            }
        }

        public void AddError(Exception exception)
        {
            if(exception != null)
            {
                this._errors.Add(exception);
            }
        }

    }
}