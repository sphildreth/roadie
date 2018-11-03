using System;
using System.Collections.Generic;

namespace Roadie.Library
{
    [Serializable]
    public class OperationResultModel
    {
        public const string NoImageDataFound = "NO_IMAGE_DATA_FOUND";
        public const string NotModified = "NotModified";
        public const string OkMessage = "OK";
        public virtual Dictionary<string, string> Data { get; set; }
        public IEnumerable<string> Errors { get; set; }
        public bool IsSuccess { get; set; }
        public long OperationTime { get; set; }
        public string RoadieId { get; set; }

        public OperationResultModel()
        {
            this.Data = new Dictionary<string, string>();
        }
    }
}