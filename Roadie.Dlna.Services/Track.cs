using Roadie.Dlna.Server;
using Roadie.Dlna.Utility;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace Roadie.Dlna.Services
{
    [Serializable]
    public sealed class Track : IMediaAudioResource
    {
        private byte[] FileData = null;

        public IMediaCoverResource Cover { get; }
        public string Id { get; set; }
        public DateTime InfoDate { get; }
        public long? InfoSize { get; }
        public DlnaMediaTypes MediaType { get; }

        public string MetaAlbum { get; }
        public string MetaArtist { get; }
        public string MetaDescription { get; }
        public TimeSpan? MetaDuration { get; }
        public string MetaGenre { get; }
        public string MetaPerformer { get; }
        public int? MetaReleaseYear { get; }
        public int? MetaTrack { get; }
        public string Path { get; }
        public string PN { get; }

        public IHeaders Properties
        {
            get
            {
                var rv = new RawHeaders { { "Title", Title }, { "MediaType", MediaType.ToString() }, { "Type", Type.ToString() } };
                if (InfoSize.HasValue)
                {
                    rv.Add("SizeRaw", InfoSize.ToString());
                    rv.Add("Size", InfoSize.Value.FormatFileSize());
                }
                rv.Add("Date", InfoDate.ToString(CultureInfo.InvariantCulture));
                rv.Add("DateO", InfoDate.ToString("o"));
                try
                {
                    if (Cover != null)
                    {
                        rv.Add("HasCover", "true");
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Failed to access CachedCover Ex [{ ex }]", "Warning");
                }
                if (MetaAlbum != null)
                {
                    rv.Add("Album", MetaAlbum);
                }
                if (MetaArtist != null)
                {
                    rv.Add("Artist", MetaArtist);
                }
                if (MetaDescription != null)
                {
                    rv.Add("Description", MetaDescription);
                }
                if (MetaDuration != null)
                {
                    rv.Add("Duration", MetaDuration.Value.ToString("g"));
                }
                if (MetaGenre != null)
                {
                    rv.Add("Genre", MetaGenre);
                }
                if (MetaPerformer != null)
                {
                    rv.Add("Performer", MetaPerformer);
                }
                if (MetaTrack != null)
                {
                    rv.Add("Track", MetaTrack.Value.ToString());
                }
                return rv;
            }
        }

        public string Title { get; }
        public DlnaMime Type { get; }

        public Track(string id, string artistName, string releaseTitle, short mediaNumber,
                     string title, string genre, string trackArtistName,
                     int trackNumber, int? releaseYear, TimeSpan duration,
                     string description, DateTime infoDate, byte[] coverData, byte[] fileData = null)
        {
            Id = id;
            Title = $"[{ trackNumber.ToString().PadLeft(3, '0') }] { title }";
            MetaArtist = artistName;
            MetaAlbum = releaseTitle;
            if (mediaNumber > 1)
            {
                MetaAlbum = $"{ mediaNumber.ToString().PadLeft(2, '0') } { releaseTitle}";
            }
            MetaDescription = description;
            MetaDuration = duration;
            MetaGenre = genre;
            MetaPerformer = trackArtistName;
            MetaReleaseYear = releaseYear;
            MetaTrack = trackNumber;
            InfoDate = infoDate;
            if (fileData != null)
            {
                FileData = fileData;
                InfoSize = fileData.Length;
            }
            MediaType = DlnaMediaTypes.Audio;
            Type = DlnaMime.AudioMP3;
            if (coverData != null)
            {
                Cover = new CoverArt(coverData, 320, 320);
            }
        }

        public int CompareTo(IMediaItem other) => throw new NotImplementedException();

        public Stream CreateContentStream() => new MemoryStream(FileData);

        public bool Equals(IMediaItem other) => throw new NotImplementedException();

        public string ToComparableTitle() => throw new NotImplementedException();
    }
}