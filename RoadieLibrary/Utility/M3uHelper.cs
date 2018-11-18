using Roadie.Library.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.Utility
{
    public static class M3uHelper
    {
        public static string M3uContentForTracks(IEnumerable<TrackList> tracks)
        {
            var result = new List<string>
            {
                "#EXTM3U"
            };
            foreach (var track in tracks)
            {
                result.Add($"#EXTINF:{ track.Duration },{ track.Artist.Text} - { track.Track.Text }");
                result.Add($"{ track.TrackPlayUrl }");
                result.Add(string.Empty);
            }
            return string.Join("\r\n", result);
        }
    }
}
