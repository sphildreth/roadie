using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Roadie.Library.Models.Releases
{
    [Serializable]
    public class Release : EntityModelBase
    {
        public const string DefaultIncludes = "tracks";

        [MaxLength(50)]
        public string AmgId { get; set; }

        public Artist Artist { get; set; }

        public List<ReleaseInCollection> Collections { get; set; }

        [MaxLength(50)]
        public string DiscogsId { get; set; }

        public List<ReleaseGenre> Genres { get; set; }
        public bool? IsVirtual { get; set; }

        [MaxLength(100)]
        public string ITunesId { get; set; }

        public List<ReleaseLabel> Labels { get; set; }

        [MaxLength(50)]
        public string LastFMId { get; set; }

        [MaxLength(65535)]
        public string LastFMSummary { get; set; }

        public string LibraryStatus { get; set; }

        public short? MediaCount { get; set; }

        public List<ReleaseMedia> Medias { get; set; }

        [MaxLength(100)]
        public string MusicBrainzId { get; set; }

        [MaxLength(65535)]
        public string Profile { get; set; }

        [Required]
        public DateTime ReleaseDate { get; set; }

        public string ReleaseType { get; set; }

        [MaxLength(100)]
        public string SpotifyId { get; set; }

        public Guid? SubmissionId { get; set; }

        public Image Thumbnail { get; set; }

        [MaxLength(250)]
        [Required]
        public string Title { get; set; }

        public short TrackCount { get; set; }
    }
}