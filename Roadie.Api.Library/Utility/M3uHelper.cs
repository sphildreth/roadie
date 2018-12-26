using Roadie.Library.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.Utility
{
    /// <summary>
    /// Helper to work with M3U files
    /// <seealso cref="https://en.wikipedia.org/wiki/M3U"/>
    /// </summary>
    public static class M3uHelper
    {
        /// <summary>
        /// For the given collection of tracks generate a M3U file in a single string
        /// </summary>
        public static string M3uContentForTracks(IEnumerable<TrackList> tracks)
        {
            var result = new List<string>
            {
                "#EXTM3U"
            };
            foreach (var track in tracks)
            {
                result.Add($"#EXTINF:{ track.Duration },{ track.Artist.Artist.Text} - { track.Track.Text }");
                result.Add($"{ track.TrackPlayUrl }");
                result.Add(string.Empty);
            }
            return string.Join("\r\n", result);
        }
    }
}
