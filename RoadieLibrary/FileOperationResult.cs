using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Net.Http.Headers;

namespace Roadie.Library
{
    /// <summary>
    /// A OperationResult specific to a File type request.
    /// </summary>
    public class FileOperationResult<T> : OperationResult<T>
    {
        public EntityTagHeaderValue ETag { get; set; }
        public DateTimeOffset? LastModified { get; set; }
        public string ContentType { get; set; }

        public FileOperationResult()
        {
        }

        public FileOperationResult(string message)
        {
            this.AddMessage(message);
        }

        public FileOperationResult(bool isNotFoundResult, string message)
        {
            this.IsNotFoundResult = isNotFoundResult;
            this.AddMessage(message);
        }

        public FileOperationResult(IEnumerable<string> messages = null)
        {
            if (messages != null && messages.Any())
            {
                this.AdditionalData = new Dictionary<string, object>();
                messages.ToList().ForEach(x => this.AddMessage(x));
            }
        }

        public FileOperationResult(bool isNotFoundResult, IEnumerable<string> messages = null)
        {
            this.IsNotFoundResult = isNotFoundResult;
            if (messages != null && messages.Any())
            {
                this.AdditionalData = new Dictionary<string, object>();
                messages.ToList().ForEach(x => this.AddMessage(x));
            }
        }
    }
}