using System;
using System.Collections.Generic;

namespace Roadie.Library.Models.Pagination
{
    [Serializable]
    public class PagedResult<T>
    {
        public int CurrentPage { get; set; }

        public bool IsSuccess => Message == OperationMessages.OkMessage;

        public string Message { get; set; }

        public long OperationTime { get; set; }

        public IEnumerable<T> Rows { get; set; }

        public int TotalCount { get; set; }

        public int TotalPages { get; set; }

        public PagedResult()
        {
            Message = OperationMessages.OkMessage;
        }
    }
}