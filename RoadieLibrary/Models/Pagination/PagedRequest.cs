using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Roadie.Library.Models.Pagination
{
    [Serializable]
    public class PagedRequest
    {
        public const string OrderDescDirection = "DESC";
        public const string OrderAscDirection = "ASC";

        public string Action { get; set; }
        public string ActionValue
        {
            get
            {
                return this.Action ?? string.Empty;
            }
        }

        public int? Current { get; set; }
        public int CurrentValue
        {
            get
            {
                return this.Current ?? 1;
            }
        }
        public string Sort { get; set; }
        public string Order { get; set; }
        public int? Limit { get; set; }
        public int LimitValue
        {
            get
            {
                if (this.Limit.HasValue && this.Limit.Value == -1)
                {
                    // Something sane other than Int.MaxLimit
                    return 500;
                }
                return this.Limit ?? 50;
            }
        }
        public int? Skip { get; set; }
        public int SkipValue
        {
            get
            {
                if (this.Current.HasValue)
                {
                    return (this.Current.Value * this.LimitValue) - this.LimitValue;
                }
                return 0;
            }
        }
        public string Inc { get; set; }

        public string Filter { get; set; }
        public string Filtervalue
        {
            get
            {
                return this.Filter ?? string.Empty;
            }
        }

        public string OrderValue(Dictionary<string, string> orderBy = null, string defaultSortBy = null, string defaultOrderBy = null)
        {
            var result = new StringBuilder();
            if (orderBy != null && orderBy.Any())
            {
                foreach (var kp in orderBy)
                {
                    if (result.Length > 0)
                    {
                        result.Append(",");
                    }
                    result.AppendFormat("{0} {1}", kp.Key, kp.Value);
                }
            }
            else
            {
                result.AppendFormat("{0} {1}", this.Sort ?? defaultSortBy, this.Order ?? defaultOrderBy ?? PagedRequest.OrderAscDirection);
            }
            return result.ToString();
        }

        public bool FilterOnlyMissing { get; set; }
        public string UserId { get; set; }
        public string UserIdValue
        {
            get
            {
                return this.UserId ?? string.Empty;
            }
        }
    }
}
