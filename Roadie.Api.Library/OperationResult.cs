using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace Roadie.Library
{
    [Serializable]
    public class OperationResult<T>
    {
        private List<Exception> _errors;

        private List<string> _messages;

        [XmlIgnore]
        public Dictionary<string, object> AdditionalClientData { get; set; } = new Dictionary<string, object>();

        [JsonIgnore]
        [XmlIgnore]
        public Dictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();

        /// <summary>
        ///     Client friendly exceptions
        /// </summary>
        [JsonPropertyName("errors")]
        public IEnumerable<AppException> AppExceptions
        {
            get
            {
                if (Errors?.Any() != true)
                {
                    return null;
                }

                return Errors.Select(x => new AppException(x.Message));
            }
        }

        public T Data { get; set; }

        /// <summary>
        ///     Server side visible exceptions
        /// </summary>
        [JsonIgnore]
        public IEnumerable<Exception> Errors { get; set; }

        [JsonIgnore]
        public bool IsAccessDeniedResult { get; set; }

        [JsonIgnore] public bool IsNotFoundResult { get; set; }

        public bool IsSuccess { get; set; }

        public IEnumerable<string> Messages => _messages;

        public long OperationTime { get; set; }

        public OperationResult()
        {
        }

        public OperationResult(IEnumerable<string> messages = null)
        {
            if (messages?.Any() == true)
            {
                AdditionalData = new Dictionary<string, object>();
                messages.ToList().ForEach(AddMessage);
            }
        }

        public OperationResult(string message = null)
        {
            AdditionalData = new Dictionary<string, object>();
            AddMessage(message);
        }

        public OperationResult(Exception error = null)
        {
            AddError(error);
        }

        public OperationResult(bool isNotFoundResult, IEnumerable<string> messages = null)
        {
            IsNotFoundResult = isNotFoundResult;
            if (messages?.Any() == true)
            {
                AdditionalData = new Dictionary<string, object>();
                messages.ToList().ForEach(AddMessage);
            }
        }

        public OperationResult(bool isNotFoundResult, string message)
        {
            IsNotFoundResult = isNotFoundResult;
            AddMessage(message);
        }

        public OperationResult(string message = null, Exception error = null)
        {
            AddMessage(message);
            AddError(error);
        }

        public void AddError(Exception exception)
        {
            if (exception != null)
            {
                (_errors ?? (_errors = new List<Exception>())).Add(exception);
            }
        }

        public void AddMessage(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                (_messages ?? (_messages = new List<string>())).Add(message);
            }
        }
    }
}