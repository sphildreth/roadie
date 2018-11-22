using Roadie.Library.Extensions;
using Roadie.Library.Utility;
using System;

namespace Roadie.Library.Models.ThirdPartyApi.Subsonic
{
    [Serializable]
    public class Request
    {
        public const string ArtistIdIdentifier = "A:";
        public const string CollectionIdentifier = "C:";
        public const int MaxPageSize = 100;
        public const string PlaylistdIdentifier = "P:";
        public const string ReleaseIdIdentifier = "R:";
        public const string TrackIdIdentifier = "T:";

        public Guid? ArtistId
        {
            get
            {
                if (string.IsNullOrEmpty(this.id))
                {
                    return null;
                }
                if (this.id.StartsWith(Request.ArtistIdIdentifier))
                {
                    return SafeParser.ToGuid(this.id);
                }
                return null;
            }
        }

        /// <summary>
        /// A unique string identifying the client application.
        /// </summary>
        public string c { get; set; }

        /// <summary>
        /// <seealso cref="f"/>
        /// </summary>
        public string callback { get; set; }

        public Guid? CollectionId
        {
            get
            {
                if (string.IsNullOrEmpty(this.id))
                {
                    return null;
                }
                if (this.id.StartsWith(Request.CollectionIdentifier))
                {
                    return SafeParser.ToGuid(this.id);
                }
                return null;
            }
        }

        /// <summary>
        /// Request data to be returned in this format. Supported values are "xml", "json" (since 1.4.0) and "jsonp" (since 1.6.0). If using jsonp, specify name of javascript callback function using a callback parameter.
        /// </summary>
        public string f { get; set; }

        /// <summary>
        /// A string which uniquely identifies the music folder. Obtained by calls to getIndexes or getMusicDirectory.
        /// </summary>
        public string id { get; set; }

        public bool IsCallbackSet
        {
            get
            {
                return !string.IsNullOrEmpty(this.callback);
            }
        }

        /// <summary>
        /// Request data to be returned in this format. Supported values are "xml", "json" (since 1.4.0) and "jsonp" (since 1.6.0). If using jsonp, specify name of javascript callback function using a callback parameter.
        /// </summary>
        public bool IsJSONRequest
        {
            // Default should be false (XML)
            get
            {
                if (string.IsNullOrEmpty(this.f))
                {
                    return false;
                }
                return this.f.ToLower().StartsWith("j");
            }
        }

        /// <summary>
        /// The password, either in clear text or hex-encoded with a "enc:" prefix. Since 1.13.0 this should only be used for testing purposes.
        /// </summary>
        public string p { get; set; }

        public string Password
        {
            get
            {
                if (string.IsNullOrEmpty(this.p))
                {
                    return null;
                }
                if (this.p.StartsWith("enc:"))
                {
                    return this.p.ToLower().Replace("enc:", "").FromHexString();
                }
                return this.p;
            }
        }

        public Guid? PlaylistId
        {
            get
            {
                if (string.IsNullOrEmpty(this.id))
                {
                    return null;
                }
                if (this.id.StartsWith(Request.PlaylistdIdentifier))
                {
                    return SafeParser.ToGuid(this.id);
                }
                return null;
            }
        }

        /// <summary>
        /// Search query.
        /// </summary>
        public string Query { get; set; }

        public Guid? ReleaseId
        {
            get
            {
                if (string.IsNullOrEmpty(this.id))
                {
                    return null;
                }
                if (this.id.StartsWith(Request.ReleaseIdIdentifier))
                {
                    return SafeParser.ToGuid(this.id);
                }
                return null;
            }
        }

        /// <summary>
        /// A random string ("salt") used as input for computing the password hash. See below for details.
        /// </summary>
        public string s { get; set; }

        /// <summary>
        /// The authentication token computed as md5(password + salt). See below for details
        /// </summary>
        public string t { get; set; }

        public Guid? TrackId
        {
            get
            {
                if (string.IsNullOrEmpty(this.id))
                {
                    return null;
                }
                if (this.id.StartsWith(Request.TrackIdIdentifier))
                {
                    return SafeParser.ToGuid(this.id);
                }
                return null;
            }
        }

        /// <summary>
        /// The username
        /// </summary>
        public string u { get; set; }

        /// <summary>
        /// The protocol version implemented by the client, i.e., the version of the subsonic-rest-api.xsd schema used (see below).
        /// </summary>
        public string v { get; set; }

        #region Paging and List Related

        private Library.Models.Pagination.PagedRequest _pagedRequest;

        /// <summary>
        /// Maximum number of albums to return.
        /// </summary>
        public int? AlbumCount { get; set; }

        /// <summary>
        /// Search result offset for albums. Used for paging.
        /// </summary>
        public int? AlbumOffset { get; set; }

        /// <summary>
        /// Maximum number of artists to return.
        /// </summary>
        public int? ArtistCount { get; set; }

        /// <summary>
        /// Search result offset for artists. Used for paging.
        /// </summary>
        public int? ArtistOffset { get; set; }

        /// <summary>
        /// The first year in the range. If fromYear > toYear a reverse chronological list is returned.
        /// </summary>
        public int? FromYear { get; set; }

        /// <summary>
        /// The name of the genre, e.g., "Rock".
        /// </summary>
        public string Genre { get; set; }

        /// <summary>
        /// Only return albums in the music folder with the given ID. See getMusicFolders.
        /// </summary>
        public int? MusicFolderId { get; set; }

        /// <summary>
        /// The list offset. Useful if you for example want to page through the list of newest albums.
        /// </summary>
        public int? Offset { get; set; }

        public Library.Models.Pagination.PagedRequest PagedRequest
        {
            get
            {
                var limit = this.Size ?? Request.MaxPageSize;
                var page = this.Offset > 0 ? (int)Math.Ceiling((decimal)this.Offset.Value / (decimal)limit) : 1;
                var pagedRequest = new Pagination.PagedRequest();
                switch (this.Type)
                {
                    case ListType.Newest:
                        pagedRequest.Sort = "CreatedDate";
                        pagedRequest.Order = "DESC";
                        break;

                    case ListType.Highest:
                        pagedRequest.Sort = "Rating";
                        pagedRequest.Order = "DESC";
                        pagedRequest.FilterRatedOnly = true;
                        break;

                    case ListType.Frequent:
                        pagedRequest.Sort = "TrackPlayedCount";
                        pagedRequest.Order = "DESC";
                        break;

                    case ListType.Recent:
                        pagedRequest.Sort = "LastPlayed";
                        pagedRequest.Order = "DESC";
                        break;

                    case ListType.AlphabeticalByName:
                        pagedRequest.Sort = "Release.Text";
                        pagedRequest.Order = "ASC";
                        break;

                    case ListType.AlphabeticalByArtist:
                        pagedRequest.Sort = "Artist.Text";
                        pagedRequest.Order = "ASC";
                        break;

                    case ListType.Starred:
                        pagedRequest.FilterRatedOnly = true;
                        pagedRequest.Sort = "Rating";
                        pagedRequest.Order = "DESC";
                        break;

                    case ListType.ByGenre:
                        pagedRequest.Sort = "Genre.Text";
                        pagedRequest.Order = "ASC";
                        break;

                    case ListType.ByYear:
                        pagedRequest.FilterFromYear = this.FromYear;
                        pagedRequest.FilterToYear = this.ToYear;
                        pagedRequest.Sort = "ReleaseDate";
                        pagedRequest.Order = this.FromYear > this.ToYear ? "DESC" : "ASC";
                        break;
                }
                pagedRequest.Limit = limit;
                pagedRequest.Page = page;
                return pagedRequest;
            }
        }

        /// <summary>
        /// The number of albums to return. Max 500.
        /// <see>Various *Count properties depending on objects being searched and client version.</see>
        /// </summary>
        public int? Size { get; set; }

        /// <summary>
        /// Maximum number of songs to return.
        /// </summary>
        public int? SongCount { get; set; }

        /// <summary>
        /// Search result offset for songs. Used for paging.
        /// </summary>
        public int? SongOffset { get; set; }

        /// <summary>
        /// The last year in the range.
        /// </summary>
        public int? ToYear { get; set; }

        public ListType Type { get; set; }

        #endregion Paging and List Related


    }
}