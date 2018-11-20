using Roadie.Library.Extensions;
using Roadie.Library.Utility;
using System;

namespace Roadie.Library.Models.ThirdPartyApi.Subsonic
{
    [Serializable]
    public class Request
    {
        public const int MaxPageSize = 500;

        public const string ArtistIdIdentifier = "A:";
        public const string CollectionIdentifier = "C:";
        public const string ReleaseIdIdentifier = "R:";
        public const string TrackIdIdentifier = "T:";
        public const string PlaylistdIdentifier = "P:";

        /// <summary>
        /// A unique string identifying the client application.
        /// </summary>
        public string c { get; set; }

        /// <summary>
        /// <seealso cref="f"/>
        /// </summary>
        public string callback { get; set; }

        /// <summary>
        /// Request data to be returned in this format. Supported values are "xml", "json" (since 1.4.0) and "jsonp" (since 1.6.0). If using jsonp, specify name of javascript callback function using a callback parameter.
        /// </summary>
        public string f { get; set; }

        public bool IsCallbackSet
        {
            get
            {
                return !string.IsNullOrEmpty(this.callback);
            }
        }

        
        public bool IsJSONRequest
        {
            get
            {
                if (string.IsNullOrEmpty(this.f))
                {
                    return true;
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

        /// <summary>
        /// A random string ("salt") used as input for computing the password hash. See below for details.
        /// </summary>
        public string s { get; set; }

        /// <summary>
        /// The authentication token computed as md5(password + salt). See below for details
        /// </summary>
        public string t { get; set; }

        /// <summary>
        /// The username
        /// </summary>
        public string u { get; set; }

        /// <summary>
        /// The protocol version implemented by the client, i.e., the version of the subsonic-rest-api.xsd schema used (see below).
        /// </summary>
        public string v { get; set; }

        /// <summary>
        /// A string which uniquely identifies the music folder. Obtained by calls to getIndexes or getMusicDirectory.
        /// </summary>
        public string id { get; set; }

        public Guid? ArtistId
        {
            get
            {
               if(string.IsNullOrEmpty(this.id))
               {
                    return null;
               }
               if(this.id.StartsWith(Request.ArtistIdIdentifier))
               {
                    return SafeParser.ToGuid(this.id.Replace(Request.ArtistIdIdentifier, ""));
               }
                return null;
            }
        }

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
                    return SafeParser.ToGuid(this.id.Replace(Request.CollectionIdentifier, ""));
                }
                return null;
            }
        }

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
                    return SafeParser.ToGuid(this.id.Replace(Request.ReleaseIdIdentifier, ""));
                }
                return null;
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
                    return SafeParser.ToGuid(this.id.Replace(Request.PlaylistdIdentifier, ""));
                }
                return null;
            }
        }

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
                    return SafeParser.ToGuid(this.id.Replace(Request.TrackIdIdentifier, ""));
                }
                return null;
            }
        }

        #region Paging and List Related

        /// <summary>
        /// The number of albums to return. Max 500.
        /// </summary>
        public int? Size { get; set; }

        /// <summary>
        /// The list offset. Useful if you for example want to page through the list of newest albums.
        /// </summary>
        public int? Offset { get; set; }

        /// <summary>
        /// The first year in the range. If fromYear > toYear a reverse chronological list is returned.
        /// </summary>
        public int? FromYear { get; set; }

        /// <summary>
        /// The last year in the range.
        /// </summary>
        public int? ToYear { get; set; }

        /// <summary>
        /// The name of the genre, e.g., "Rock".
        /// </summary>
        public string Genre { get; set; }

        /// <summary>
        /// Only return albums in the music folder with the given ID. See getMusicFolders.
        /// </summary>
        public int? MusicFolderId { get; set; }

        public ListType Type { get; set; }

        //var pagedRequest = new Library.Models.Pagination.PagedRequest
        //{

        //};

        private Library.Models.Pagination.PagedRequest _pagedRequest;

        public Library.Models.Pagination.PagedRequest PagedRequest
        {
            get
            {
                if(this._pagedRequest == null)
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
                            break;
                        case ListType.AlphabeticalByArtist:
                            break;
                        case ListType.Starred:
                            pagedRequest.FilterRatedOnly = true;
                            break;
                        case ListType.ByGenre:
                            break;
                        default:
                            break;
                    }
                    pagedRequest.Limit = limit;
                    pagedRequest.Page = page;
                    this._pagedRequest = pagedRequest;
                }
                return this._pagedRequest;
            }
        }

        #endregion


        //public user CheckPasswordGetUser(ICacheManager<object> cacheManager, RoadieDbContext context)
        //{
        //    user user = null;
        //    if (string.IsNullOrEmpty(this.UsernameValue))
        //    {
        //        return null;
        //    }
        //    try
        //    {
        //        var cacheKey = string.Format("urn:user:byusername:{0}", this.UsernameValue.ToLower());
        //        var resultInCache = cacheManager.Get<user>(cacheKey);
        //        if (resultInCache == null)
        //        {
        //            user = context.users.FirstOrDefault(x => x.username.Equals(this.UsernameValue, StringComparison.OrdinalIgnoreCase));
        //            var claims = new List<string>
        //            {
        //                new Claim(Library.Authentication.ClaimTypes.UserId, user.id.ToString()).ToString()
        //            };
        //            var sql = @"select ur.name FROM `userrole` ur LEFT JOIN usersInRoles uir on ur.id = uir.userRoleId where uir.userId = " + user.id + ";";
        //            var userRoles = context.Database.SqlQuery<string>(sql).ToList();
        //            if (userRoles != null && userRoles.Any())
        //            {
        //                foreach (var userRole in userRoles)
        //                {
        //                    claims.Add(new Claim(Library.Authentication.ClaimTypes.UserRole, userRole).ToString());
        //                }
        //            }
        //            user.ClaimsValue = claims;
        //            cacheManager.Add(cacheKey, user);
        //        }
        //        else
        //        {
        //            user = resultInCache;
        //        }
        //        if (user == null)
        //        {
        //            return null;
        //        }
        //        var password = this.Password;
        //        var wasAuthenticatedAgainstPassword = false;
        //        if (!string.IsNullOrEmpty(this.s))
        //        {
        //            var token = ModuleBase.MD5Hash((user.apiToken ?? user.email) + this.s);
        //            if (!token.Equals(this.t, StringComparison.OrdinalIgnoreCase))
        //            {
        //                user = null;
        //            }
        //            else
        //            {
        //                wasAuthenticatedAgainstPassword = true;
        //            }
        //        }
        //        else
        //        {
        //            if (user != null && !BCrypt.Net.BCrypt.Verify(password, user.password))
        //            {
        //                user = null;
        //            }
        //            else
        //            {
        //                wasAuthenticatedAgainstPassword = true;
        //            }
        //        }
        //        if (wasAuthenticatedAgainstPassword)
        //        {
        //            // Since API dont update LastLogin which likely invalidates any browser logins
        //            user.lastApiAccess = DateTime.UtcNow;
        //            context.SaveChanges();
        //        }
        //        return user;
        //    }
        //    catch (Exception ex)
        //    {
        //        Trace.WriteLine("Error CheckPassword [" + ex.Serialize() + "]");
        //    }
        //    return null;
        //}
    }
}