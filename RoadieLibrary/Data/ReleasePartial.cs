using Roadie.Library.Extensions;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Roadie.Library.Data
{
    public partial class Release
    {
        public static string CacheRegionUrn(Guid Id)
        {
            return string.Format("urn:release:{0}", Id);
        }

        public static string CacheUrn(Guid Id)
        {
            return $"urn:artist_by_id:{ Id }";
        }

        public string CacheKey
        {
            get
            {
                return Artist.CacheUrn(this.RoadieId);
            }
        }

        public string CacheRegion
        {
            get
            {
                return Release.CacheRegionUrn(this.RoadieId);
            }
        }

        public string Etag
        {
            get
            {
                using (var md5 = System.Security.Cryptography.MD5.Create())
                {
                    return String.Concat(md5.ComputeHash(System.Text.Encoding.Default.GetBytes(string.Format("{0}{1}", this.RoadieId, this.LastUpdated))).Select(x => x.ToString("D2")));
                }
            }
        }

        public bool IsCastRecording
        {
            get
            {
                if (string.IsNullOrEmpty(this.Title))
                {
                    return false;
                }
                return this.IsReleaseTypeOf("Original Broadway Cast") || this.IsReleaseTypeOf("Original Cast");
            }
        }

        public bool IsSoundTrack
        {
            get
            {
                if (string.IsNullOrEmpty(this.Title))
                {
                    return false;
                }
                foreach (var soundTrackTrigger in new List<string> { "soundtrack", " ost", "(ost)" })
                {
                    if (this.IsReleaseTypeOf(soundTrackTrigger))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public bool IsValid
        {
            get
            {
                return !string.IsNullOrEmpty(this.Title) && this.ReleaseDate > DateTime.MinValue;
            }
        }

        public bool IsReleaseTypeOf(string type, bool doCheckTitles = false)
        {
            if (string.IsNullOrEmpty(type))
            {
                return false;
            }
            try
            {
                if (doCheckTitles)
                {
                    if (this.Artist != null && !string.IsNullOrEmpty(this.Artist.Name))
                    {
                        if (this.Artist.Name.IndexOf(type, 0, StringComparison.OrdinalIgnoreCase) > -1)
                        {
                            return true;
                        }
                    }
                    if (!string.IsNullOrEmpty(this.Title))
                    {
                        if (this.Title.IndexOf(type, 0, StringComparison.OrdinalIgnoreCase) > -1)
                        {
                            return true;
                        }
                    }
                    if (this.AlternateNames != null)
                    {
                        if (this.AlternateNames.IsValueInDelimitedList(type))
                        {
                            return true;
                        }
                    }
                }
                if (this.Tags != null)
                {
                    if (this.Tags.IsValueInDelimitedList(type))
                    {
                        return true;
                    }
                }
                if (this.Genres != null)
                {
                    if (this.Genres.Any(x => x.Genre.Name.ToLower().Equals(type)))
                    {
                        return true;
                    }
                }
            }
            catch
            {
            }
            return false;
        }

        /// <summary>
        /// Return this releases file folder for the given artist folder
        /// </summary>
        /// <param name="artistFolder"></param>
        /// <returns></returns>
        public string ReleaseFileFolder(string artistFolder)
        {
            return FolderPathHelper.ReleasePath(artistFolder, this.Title, this.ReleaseDate.Value);
        }

        public override string ToString()
        {
            return string.Format("Id [{0}], Title [{1}], Release Date [{2}]", this.Id, this.Title, this.ReleaseDate);
        }
    }
}