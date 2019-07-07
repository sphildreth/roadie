using Roadie.Library.Enums;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Roadie.Library.Models.Pagination
{
    [Serializable]
    public class PagedRequest
    {
        public const string OrderAscDirection = "ASC";
        public const string OrderDescDirection = "DESC";
        private int? _skipValue;

        public string Action { get; set; }

        public string ActionValue => Action ?? string.Empty;

        public string Filter { get; set; }
        public string FilterByGenre { get; set; }
        public bool FilterFavoriteOnly { get; set; }
        public int? FilterFromYear { get; set; }
        public int? FilterMinimumRating { get; set; }
        public bool FilterRatedOnly { get; set; }
        public Guid? FilterToArtistId { get; set; }
        public Guid? FilterToCollectionId { get; set; }
        public Guid? FilterToLabelId { get; set; }
        public Guid? FilterToPlaylistId { get; set; }
        public bool FilterTopPlayedOnly { get; set; }
        public Guid? FilterToReleaseId { get; set; }
        public short? FilterToStatus { get; set; }
        public Statuses FilterToStatusValue => SafeParser.ToEnum<Statuses>(FilterToStatus);
        public Guid? FilterToTrackId { get; set; }
        public Guid?[] FilterToTrackIds { get; set; }
        public int? FilterToYear { get; set; }
        public string FilterValue => Filter ?? string.Empty;
        public bool IsHistoryRequest { get; set; }
        public short? Limit { get; set; } = 10;

        public short LimitValue
        {
            get
            {
                if (Limit.HasValue && Limit.Value == -1)
                    // Suppose to mean return all, this limits tos something sane other than Int.MaxLimit
                    return 500;
                return Limit ?? 50;
            }
        }

        public string Order { get; set; }
        public int? Page { get; set; } = 1;

        public int PageValue => Page ?? 1;

        public int SkipValue
        {
            get
            {
                if (!_skipValue.HasValue)
                {
                    if (Page.HasValue)
                    {
                        _skipValue = Page.Value * LimitValue - LimitValue;
                    }
                    else
                    {
                        return 0;
                    }
                }

                return _skipValue.Value;
            }
            set => _skipValue = value;
        }

        public string Sort { get; set; }

        /// <summary>
        ///     Sort first with the given (if any) parameter then apply default sorting. Example is "rating" supplied then sort by
        ///     sortName
        /// </summary>
        public string OrderValue(Dictionary<string, string> orderBy = null, string defaultSortBy = null,
            string defaultOrderBy = null)
        {
            var result = new StringBuilder();
            if (!string.IsNullOrEmpty(Sort))
                result.AppendFormat("{0} {1}", Sort ?? defaultSortBy, Order ?? defaultOrderBy ?? OrderAscDirection);
            if (orderBy != null && orderBy.Any())
                foreach (var kp in orderBy)
                {
                    if (result.Length > 0) result.Append(",");
                    result.AppendFormat("{0} {1}", kp.Key, kp.Value);
                }

            return result.ToString();
        }
    }
}