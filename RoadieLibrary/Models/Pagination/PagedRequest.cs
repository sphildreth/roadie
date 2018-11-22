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

        public int? Page { get; set; } = 1;
        public int PageValue
        {
            get
            {
                return this.Page ?? 1;
            }
        }
        public string Sort { get; set; }
        public string Order { get; set; }
        public int? Limit { get; set; } = 10;
        public int LimitValue
        {
            get
            {
                if (this.Limit.HasValue && this.Limit.Value == -1)
                {
                    // Suppose to mean return all, this limits tos something sane other than Int.MaxLimit
                    return 500;
                }
                return this.Limit ?? 50;
            }
        }
        private int? _skipValue;
        public int SkipValue
        {
            get
            {
                if (!this._skipValue.HasValue)
                {
                    if (this.Page.HasValue)
                    {
                        this._skipValue = (this.Page.Value * this.LimitValue) - this.LimitValue;
                    }
                    else
                    {
                        return 0;
                    }
                }
                return this._skipValue.Value;
            }
            set
            {
                this._skipValue = value;
            }
        }
        public string Filter { get; set; }
        public string FilterValue
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

        public bool? FilterOnlyMissing { get; set; }

        public Guid? FilterToArtistId { get; set; }
        public Guid? FilterToTrackId { get; set; }
        public Guid? FilterToCollectionId { get; set; }
        public Guid? FilterToPlaylistId { get; set; }

        public int? FilterMinimumRating { get; set; }
        public bool FilterRatedOnly { get; internal set; }
        public bool FilterFavoriteOnly { get; set; }
        public bool FilterTopPlayedOnly { get; set; }
        public int? FilterFromYear { get; internal set; }
        public int? FilterToYear { get; internal set; }
        public string FilterByGenre { get; internal set; }

        public PagedRequest()
        { }

    }
}
