using Roadie.Library.Extensions;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace Roadie.Library.Data
{
    public partial class Release
    {
        public string CacheKey => Artist.CacheUrn(RoadieId);

        public string CacheRegion => CacheRegionUrn(RoadieId);

        public string Etag
        {
            get
            {
                return HashHelper.CreateMD5($"{ RoadieId }{ LastUpdated }");
            }
        }

        public bool IsCastRecording
        {
            get
            {
                if (string.IsNullOrEmpty(Title)) return false;
                return IsReleaseTypeOf("Original Broadway Cast") || IsReleaseTypeOf("Original Cast");
            }
        }

        public bool IsSoundTrack
        {
            get
            {
                if (string.IsNullOrEmpty(Title)) return false;
                foreach (var soundTrackTrigger in new List<string> { "soundtrack", " ost", "(ost)" })
                    if (IsReleaseTypeOf(soundTrackTrigger))
                        return true;
                return false;
            }
        }

        public bool IsValid => !string.IsNullOrEmpty(Title) && ReleaseDate > DateTime.MinValue;

        public int? ReleaseYear
        {
            get
            {
                if (ReleaseDate.HasValue) return ReleaseDate.Value.Year;
                return null;
            }
        }

        public static string CacheRegionUrn(Guid Id)
        {
            return string.Format("urn:release:{0}", Id);
        }

        public static string CacheUrn(Guid Id)
        {
            return $"urn:release_by_id:{Id}";
        }

        public bool IsReleaseTypeOf(string type, bool doCheckTitles = false)
        {
            if (string.IsNullOrEmpty(type)) return false;
            try
            {
                if (doCheckTitles)
                {
                    if (Artist != null && !string.IsNullOrEmpty(Artist.Name))
                        if (Artist.Name.IndexOf(type, 0, StringComparison.OrdinalIgnoreCase) > -1)
                            return true;
                    if (!string.IsNullOrEmpty(Title))
                        if (Title.IndexOf(type, 0, StringComparison.OrdinalIgnoreCase) > -1)
                            return true;
                    if (AlternateNames != null)
                        if (AlternateNames.IsValueInDelimitedList(type))
                            return true;
                }

                if (Tags != null)
                    if (Tags.IsValueInDelimitedList(type))
                        return true;
                if (Genres != null)
                    if (Genres.Any(x => x.Genre.Name.ToLower().Equals(type)))
                        return true;
            }
            catch
            {
            }

            return false;
        }

        /// <summary>
        ///     Return this releases file folder for the given artist folder
        /// </summary>
        /// <param name="artistFolder"></param>
        /// <returns></returns>
        public string ReleaseFileFolder(string artistFolder)
        {
            return FolderPathHelper.ReleasePath(artistFolder, Title, ReleaseDate.Value);
        }

        public override string ToString()
        {
            return $"Id [{Id}], Title [{Title}], Release Date [{ReleaseYear}]";
        }
    }
}