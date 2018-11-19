using Roadie.Library.Extensions;
using System;

namespace Roadie.Library.Models.ThirdPartyApi.Subsonic
{
    [Serializable]
    public class Request
    {
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