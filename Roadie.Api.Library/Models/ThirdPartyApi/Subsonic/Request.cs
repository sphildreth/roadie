using Newtonsoft.Json;
using Roadie.Library.Extensions;
using Roadie.Library.Models.Pagination;
using Roadie.Library.Utility;
using System;

namespace Roadie.Library.Models.ThirdPartyApi.Subsonic
{
    [Serializable]
    public class Request
    {
        public const string ArtistIdIdentifier = "A:";
        public const string CollectionIdentifier = "C:";
        public const short MaxPageSize = 100;
        public const string PlaylistdIdentifier = "P:";
        public const string ReleaseIdIdentifier = "R:";
        public const string TrackIdIdentifier = "T:";

        public Guid? ArtistId
        {
            get
            {
                if (string.IsNullOrEmpty(id)) return null;
                if (id.StartsWith(ArtistIdIdentifier)) return SafeParser.ToGuid(id);
                return null;
            }
        }

        /// <summary>
        ///     A unique string identifying the client application.
        /// </summary>
        public string c { get; set; }

        /// <summary>
        ///     <seealso cref="f" />
        /// </summary>
        public string callback { get; set; }

        public Guid? CollectionId
        {
            get
            {
                if (string.IsNullOrEmpty(id)) return null;
                if (id.StartsWith(CollectionIdentifier)) return SafeParser.ToGuid(id);
                return null;
            }
        }

        /// <summary>
        ///     Request data to be returned in this format. Supported values are "xml", "json" (since 1.4.0) and "jsonp" (since
        ///     1.6.0). If using jsonp, specify name of javascript callback function using a callback parameter.
        /// </summary>
        public string f { get; set; }

        /// <summary>
        ///     A string which uniquely identifies the music folder. Obtained by calls to getIndexes or getMusicDirectory.
        /// </summary>
        public string id { get; set; }

        /// <summary>
        ///     Some operations have an array of ids, see savePlayQue
        /// </summary>
        public string[] ids { get; set; }

        public bool IsCallbackSet => !string.IsNullOrEmpty(callback);

        /// <summary>
        ///     Request data to be returned in this format. Supported values are "xml", "json" (since 1.4.0) and "jsonp" (since
        ///     1.6.0). If using jsonp, specify name of javascript callback function using a callback parameter.
        /// </summary>
        public bool IsJSONRequest
        {
            // Default should be false (XML)
            get
            {
                if (string.IsNullOrEmpty(f)) return false;
                return f.ToLower().StartsWith("j");
            }
        }

        /// <summary>
        ///     The password, either in clear text or hex-encoded with a "enc:" prefix. Since 1.13.0 this should only be used for
        ///     testing purposes.
        /// </summary>
        [JsonIgnore]
        public string p { get; set; }

        [JsonIgnore]
        public string Password
        {
            get
            {
                if (string.IsNullOrEmpty(p)) return null;
                if (p.StartsWith("enc:")) return p.ToLower().Replace("enc:", "").FromHexString();
                return p;
            }
        }

        public Guid? PlaylistId
        {
            get
            {
                if (string.IsNullOrEmpty(id)) return null;
                if (id.StartsWith(PlaylistdIdentifier)) return SafeParser.ToGuid(id);
                return null;
            }
        }

        /// <summary>
        ///     Search query.
        /// </summary>
        public string Query { get; set; }

        public Guid? ReleaseId
        {
            get
            {
                if (string.IsNullOrEmpty(id)) return null;
                if (id.StartsWith(ReleaseIdIdentifier)) return SafeParser.ToGuid(id);
                return null;
            }
        }

        /// <summary>
        ///     A random string ("salt") used as input for computing the password hash. See below for details.
        /// </summary>
        public string s { get; set; }

        /// <summary>
        ///     Whether this is a "submission" or a "now playing" notification.
        /// </summary>
        public string submission { get; set; }

        /// <summary>
        ///     The authentication token computed as md5(password + salt). See below for details
        /// </summary>
        public string t { get; set; }

        /// <summary>
        ///     The time (in milliseconds since 1 Jan 1970) at which the song was listened to.
        /// </summary>
        public string time { get; set; }

        public Guid? TrackId
        {
            get
            {
                if (string.IsNullOrEmpty(id)) return null;
                if (id.StartsWith(TrackIdIdentifier)) return SafeParser.ToGuid(id);
                return null;
            }
        }

        /// <summary>
        ///     The username
        /// </summary>
        public string u { get; set; }

        /// <summary>
        ///     The protocol version implemented by the client, i.e., the version of the subsonic-rest-api.xsd schema used (see
        ///     below).
        /// </summary>
        public string v { get; set; }

        #region Paging and List Related

        /// <summary>
        ///     Maximum number of albums to return.
        /// </summary>
        public short? AlbumCount { get; set; }

        /// <summary>
        ///     Search result offset for albums. Used for paging.
        /// </summary>
        public int? AlbumOffset { get; set; }

        /// <summary>
        ///     Maximum number of artists to return.
        /// </summary>
        public short? ArtistCount { get; set; }

        /// <summary>
        ///     The artist name.
        ///     <see cref="getTopSongs" />
        /// </summary>
        public string ArtistName { get; set; }

        /// <summary>
        ///     Search result offset for artists. Used for paging.
        /// </summary>
        public int? ArtistOffset { get; set; }

        /// <summary>
        ///     The first year in the range. If fromYear > toYear a reverse chronological list is returned.
        /// </summary>
        public int? FromYear { get; set; }

        /// <summary>
        ///     The name of the genre, e.g., "Rock".
        /// </summary>
        public string Genre { get; set; }

        // When adding a chat message this is the message to add
        public string Message { get; set; }

        /// <summary>
        ///     Only return albums in the music folder with the given ID. See getMusicFolders.
        /// </summary>
        public int? MusicFolderId { get; set; }

        /// <summary>
        ///     The list offset. Useful if you for example want to page through the list of newest albums.
        /// </summary>
        public int? Offset { get; set; }

        public PagedRequest PagedRequest
        {
            get
            {
                var limit = Size ?? MaxPageSize;
                var page = Offset > 0 ? (int)Math.Ceiling(Offset.Value / (decimal)limit) : 1;
                var pagedRequest = new PagedRequest();
                switch (Type)
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
                        pagedRequest.FilterByGenre = Genre;
                        break;

                    case ListType.ByYear:
                        pagedRequest.FilterFromYear = FromYear;
                        pagedRequest.FilterToYear = ToYear;
                        pagedRequest.Sort = "ReleaseDate";
                        pagedRequest.Order = FromYear > ToYear ? "DESC" : "ASC";
                        break;
                }

                pagedRequest.Limit = limit;
                pagedRequest.Page = page;
                return pagedRequest;
            }
        }

        /// <summary>
        ///     The number of albums to return. Max 500.
        ///     <see>Various *Count properties depending on objects being searched and client version.</see>
        ///     <remark>Something this value is posted as 'count' versus 'size'</remark>
        /// </summary>
        public short? Size { get; set; }

        /// <summary>
        ///     Maximum number of songs to return.
        /// </summary>
        public short? SongCount { get; set; }

        /// <summary>
        ///     Search result offset for songs. Used for paging.
        /// </summary>
        public int? SongOffset { get; set; }

        /// <summary>
        ///     The last year in the range.
        /// </summary>
        public int? ToYear { get; set; }

        public ListType Type { get; set; }

        #endregion Paging and List Related
    }
}