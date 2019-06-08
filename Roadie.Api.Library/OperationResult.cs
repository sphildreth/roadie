using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Serialization;

namespace Roadie.Library
{
    [Serializable]
    public class AppException : Exception
    {
        public AppException() : base()
        {
        }

        public AppException(string message) : base(message)
        {
        }

        public AppException(string message, params object[] args)
            : base(String.Format(CultureInfo.CurrentCulture, message, args))
        {
        }
    }

    [Serializable]
    public class OperationResult<T>
    {
        private List<Exception> _errors;
        private List<string> _messages;

        [JsonIgnore]
        [XmlIgnore]
        public Dictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Client friendly exceptions
        /// </summary>
        [JsonProperty("errors")]
        public IEnumerable<AppException> AppExceptions
        {
            get
            {
                if (this.Errors == null || !this.Errors.Any())
                {
                    return null;
                }
                return this.Errors.Select(x => new AppException(x.Message));
            }
        }

        public T Data { get; set; }

        /// <summary>
        /// Server side visible exceptions
        /// </summary>
        [JsonIgnore]
        public IEnumerable<Exception> Errors { get; set; }

        [JsonIgnore]
        public bool IsNotFoundResult { get; set; }

        [JsonIgnore]
        public bool IsAccessDeniedResult { get; set; }

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
                if (this._errors == null)
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
                if (this._messages == null)
                {
                    this._messages = new List<string>();
                }
                this._messages.Add(message);
            }
        }
    }
}