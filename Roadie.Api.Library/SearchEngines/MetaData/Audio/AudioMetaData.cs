using Roadie.Library.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Roadie.Library.MetaData.Audio
{
    [Serializable]
    [DebuggerDisplay(
        "Artist: {Artist}, TrackArtist: {TrackArtist}, Release: {Release}, TrackNumber: {TrackNumber}, Title: {Title}, Year: {Year}")]
    public sealed class AudioMetaData : IAudioMetaData
    {
        public const char ArtistSplitCharacter = '/';
        public const int MinimumYearValue = 1900;
        public const string SoundTrackArtist = "Sound Tracks";

        private string _artist;

        private bool _doModifyArtistNameOnGet = true;

        private FileInfo _fileInfo;

        /// <summary>
        ///     TIT2
        /// </summary>
        private string _title;

        private string _trackArtist;

        /// <summary>
        ///     TYER
        /// </summary>
        private int? _year;

        public string AmgId { get; set; }

        /// <summary>
        ///     TPE1 First Lead Artist
        /// </summary>
        public string Artist
        {
            get
            {
                if (_doModifyArtistNameOnGet && !string.IsNullOrEmpty(_artist) && _artist.Contains(ArtistSplitCharacter.ToString()))
                {
                    return _artist.Split(ArtistSplitCharacter)[0];
                }

                return _artist;
            }
            set
            {
                _artist = value;
                if (!string.IsNullOrEmpty(_artist))
                {
                    _artist = _artist.Replace(';', ArtistSplitCharacter);
                }
            }
        }

        public string ArtistRaw { get; set; }

        public int? AudioBitrate { get; set; }

        public int? AudioChannels { get; set; }

        public AudioMetaDataWeights AudioMetaDataWeights
        {
            get
            {
                var result = AudioMetaDataWeights.None;
                if (!string.IsNullOrEmpty(Artist))
                {
                    result |= AudioMetaDataWeights.Artist;
                }

                if (!string.IsNullOrEmpty(Title))
                {
                    result |= AudioMetaDataWeights.Time;
                }

                if ((Year ?? 0) > 1)
                {
                    result |= AudioMetaDataWeights.Year;
                }

                if ((TrackNumber ?? 0) > 1)
                {
                    result |= AudioMetaDataWeights.TrackNumber;
                }

                if ((TotalTrackNumbers ?? 0) > 1)
                {
                    result |= AudioMetaDataWeights.TrackTotalNumber;
                }

                if (TotalSeconds > 1)
                {
                    result |= AudioMetaDataWeights.Time;
                }

                return result;
            }
        }

        public int? AudioSampleRate { get; set; }

        /// <summary>
        ///     COMM
        /// </summary>
        public string Comments { get; set; }

        /// <summary>
        ///     Directory holding file used to get this AudioMetaData
        /// </summary>
        public string Directory
        {
            get
            {
                if (string.IsNullOrEmpty(Filename))
                {
                    return null;
                }

                return Path.GetDirectoryName(Filename);
            }
        }

        /// <summary>
        ///     TPOS
        /// </summary>
        public int? Disc { get; set; }

        /// <summary>
        ///     TSST
        /// </summary>
        public string DiscSubTitle { get; set; }

        [IgnoreDataMember]
        [JsonIgnore]
        public FileInfo FileInfo => _fileInfo ?? (_fileInfo = new FileInfo(Filename));

        /// <summary>
        ///     Full filename to the file used to get this AudioMetaData
        /// </summary>
        public string Filename { get; set; }

        public ICollection<string> Genres { get; set; }

        [IgnoreDataMember]
        [JsonIgnore]
        public IEnumerable<AudioMetaDataImage> Images { get; set; }

        public string ISRC { get; internal set; }

        public bool IsSoundTrack
        {
            get
            {
                if (Genres?.Any() == true)
                {
                    var soundtrackGenres = new List<string> { "24", "soundtrack" };
                    if (Genres.Intersect(soundtrackGenres, StringComparer.OrdinalIgnoreCase).Any())
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
                try
                {
                    Artist = Artist == null ? null : Artist.Equals("Unknown Artist") ? null : Artist;
                    Release = Release == null ? null : Release.Equals("Unknown Release") ? null : Release;
                    if (!string.IsNullOrEmpty(Title))
                    {
                        var trackNumberTitle = $"Track {TrackNumber}";
                        Title = Title == trackNumberTitle ? null : Title;
                    }

                    return !string.IsNullOrEmpty(Artist)
                           && !string.IsNullOrEmpty(Release)
                           && !string.IsNullOrEmpty(Title)
                           && (Year ?? 0) > 0
                           && (TrackNumber ?? 0) > 0;
                }
                catch
                {
                }

                return false;
            }
        }

        public string LastFmId { get; set; }

        public string MusicBrainzId { get; set; }

        /// <summary>
        ///     TALB
        /// </summary>
        public string Release { get; set; }

        public string ReleaseLastFmId { get; set; }

        public string ReleaseMusicBrainzId { get; set; }

        public ulong? SampleLength { get; set; }

        public string SpecialTitle { get; set; }

        public string SpotifyId { get; set; }

        /// <summary>
        ///     TIME
        /// </summary>
        public TimeSpan? Time { get; set; }

        /// <summary>
        ///     TIT2
        /// </summary>
        public string Title
        {
            get => SpecialTitle ?? _title;
            set
            {
                _title = value;
                if (IsSoundTrack)
                {
                    Artist = SoundTrackArtist;
                }
            }
        }

        /// <summary>
        ///     Total number of Discs for Media
        /// </summary>
        public int? TotalDiscCount { get; set; }

        public double TotalSeconds
        {
            get
            {
                if (Time == null)
                {
                    return 0;
                }

                return Time.Value.TotalSeconds;
            }
        }

        /// <summary>
        ///     TRCK 0[/OptionalElements]
        /// </summary>
        public int? TotalTrackNumbers { get; set; }

        /// <summary>
        ///     TOPE First Contributing Artist, null if same as Artist
        /// </summary>
        public string TrackArtist
        {
            get
            {
                string result = null;
                if (!string.IsNullOrEmpty(_trackArtist))
                {
                    result = _trackArtist.Split(ArtistSplitCharacter)[0].ToTitleCase();
                }

                return !String.Equals(_artist, result, StringComparison.OrdinalIgnoreCase) ? result : null;
            }
            set => _trackArtist = value;
        }

        public string TrackArtistRaw { get; set; }

        /// <summary>
        ///     TOPE All Contributing Artists
        /// </summary>
        /// <remarks>Per ID3.Org Spec: They are seperated with the "/" character.</remarks>
        public IEnumerable<string> TrackArtists
        {
            get
            {
                if (string.IsNullOrEmpty(_trackArtist))
                {
                    return new string[0];
                }

                if (!_trackArtist.Contains(ArtistSplitCharacter.ToString()))
                {
                    if (string.IsNullOrEmpty(TrackArtist))
                    {
                        return new string[0];
                    }

                    return new string[1] { TrackArtist };
                }

                if (!string.IsNullOrEmpty(_artist) || !string.IsNullOrEmpty(_trackArtist))
                {
                    if (!String.Equals(_artist, _trackArtist, StringComparison.OrdinalIgnoreCase))
                    {
                        return _trackArtist.Split(ArtistSplitCharacter)
                                           .Where(x => !string.IsNullOrEmpty(x))
                                           .Select(x => x.ToTitleCase())
                                           .OrderBy(x => x)
                                           .ToArray();
                    }
                }

                return new string[0];
            }
        }

        /// <summary>
        ///     TRCK
        /// </summary>
        public short? TrackNumber { get; set; }

        public int ValidWeight => (int)AudioMetaDataWeights;

        /// <summary>
        ///     TYER | TDRC | TORY | TDOR
        /// </summary>
        public int? Year
        {
            get => _year;
            set => _year = value < MinimumYearValue ? null : value;
        }

        public AudioMetaData()
        {
            Images = new AudioMetaDataImage[0];
        }

        public override bool Equals(object obj)
        {
            if (!(obj is AudioMetaData item))
            {
                return false;
            }

            return item.GetHashCode() == GetHashCode();
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = (hash * 23) + Artist.GetHashCode();
                hash = (hash * 23) + Release.GetHashCode();
                hash = (hash * 23) + Title.GetHashCode();
                hash = (hash * 23) + TrackNumber.GetHashCode();
                hash = (hash * 23) + AudioBitrate.GetHashCode();
                hash = (hash * 23) + AudioSampleRate.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        ///     Use this value for the artist name, dont process in any way
        /// </summary>
        /// <param name="name"></param>
        public void SetArtistName(string name)
        {
            _artist = name;
            _doModifyArtistNameOnGet = false;
        }

        public override string ToString()
        {
            var result =
                $"IsValid: {IsValid}{(IsSoundTrack ? " [SoundTrack ]" : string.Empty)}, ValidWeight {ValidWeight}, Artist: {Artist}";
            if (!string.IsNullOrEmpty(TrackArtist))
            {
                result += $", TrackArtist: {TrackArtist}";
            }

            result += $", Release: {Release}, TrackNumber: {TrackNumber}, TrackTotal: {TotalTrackNumbers}";
            if (TotalDiscCount > 1)
            {
                result += $", Disc: {Disc}/{TotalDiscCount}";
            }

            result += $", Title: {Title}, Year: {Year}, Duration: {(Time == null ? "-" : Time.Value.ToString())}";
            return result;
        }
    }
}