using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Roadie.Library
{
    [Serializable]
    public class OperationResult<T>
    {
        private List<Exception> _errors;
        private List<string> _messages;
        public Dictionary<string, object> AdditionalData { get; set; }
        public T Data { get; set; }
        public IEnumerable<Exception> Errors { get; set; }
        public bool IsSuccess { get; set; }
        [JsonIgnore]
        public bool IsNotFoundResult { get; set; }

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

        public OperationResult(IEnumerable<string> messages = null)
        {
            if (messages != null && messages.Any())
            {
                this.AdditionalData = new Dictionary<string, object>();
                messages.ToList().ForEach(x => this.AddMessage(x));
            }
        }

        public OperationResult(bool isNotFoundResult, IEnumerable<string> messages = null)
        {
            this.IsNotFoundResult = isNotFoundResult;
            if (messages != null && messages.Any())
            {
                this.AdditionalData = new Dictionary<string, object>();
                messages.ToList().ForEach(x => this.AddMessage(x));
            }
        }

        public OperationResult(bool isNotFoundResult, string message)
        {
            this.IsNotFoundResult = isNotFoundResult;
            this.AddMessage(message);
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

        public void AddError(Exception exception)
        {
            if (exception != null)
            {
                if(this._errors == null)
                {
                    this._errors = new List<Exception>();
                }
                this._errors.Add(exception);
            }
        }

        public void AddMessage(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                if(this._messages == null)
                {
                    this._messages = new List<string>();
                }
                this._messages.Add(message);
            }
        }
    }
}