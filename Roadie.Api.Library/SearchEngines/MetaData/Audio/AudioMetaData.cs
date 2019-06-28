using Newtonsoft.Json;
using Roadie.Library.Extensions;
using Roadie.Library.Inspect.Plugins;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Roadie.Library.MetaData.Audio
{
    [Serializable]
    [DebuggerDisplay("Artist: {Artist}, TrackArtist: {TrackArtist}, Release: {Release}, TrackNumber: {TrackNumber}, Title: {Title}, Year: {Year}")]
    public sealed class AudioMetaData : IAudioMetaData
    {
        public const char ArtistSplitCharacter = '/';

        public const int MinimumYearValue = 1900;
        public const string SoundTrackArtist = "Sound Tracks";
        private string _artist = null;
        private bool _doModifyArtistNameOnGet = true;

        /// <summary>
        /// TIT2
        /// </summary>
        private string _title = null;

        private string _trackArtist = null;

        /// <summary>
        /// TYER
        /// </summary>
        private int? _year = null;

        public string AmgId { get; set; }

        /// <summary>
        /// TPE1 First Lead Artist
        /// </summary>
        public string Artist
        {
            get
            {
                if (this._doModifyArtistNameOnGet)
                {
                    if (!string.IsNullOrEmpty(this._artist) && this._artist.Contains(AudioMetaData.ArtistSplitCharacter.ToString()))
                    {
                        return this._artist.Split(AudioMetaData.ArtistSplitCharacter).First();
                    }
                }
                return this._artist;
            }
            set
            {
                this._artist = value;
                if (!string.IsNullOrEmpty(this._artist))
                {
                    this._artist = this._artist.Replace(';', AudioMetaData.ArtistSplitCharacter);
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
                if (!string.IsNullOrEmpty(this.Artist))
                {
                    result |= AudioMetaDataWeights.Artist;
                }
                if (!string.IsNullOrEmpty(this.Title))
                {
                    result |= AudioMetaDataWeights.Time;
                }
                if ((this.Year ?? 0) > 1)
                {
                    result |= AudioMetaDataWeights.Year;
                }
                if ((this.TrackNumber ?? 0) > 1)
                {
                    result |= AudioMetaDataWeights.TrackNumber;
                }
                if ((this.TotalTrackNumbers ?? 0) > 1)
                {
                    result |= AudioMetaDataWeights.TrackTotalNumber;
                }
                if (this.TotalSeconds > 1)
                {
                    result |= AudioMetaDataWeights.Time;
                }
                return result;
            }
        }

        public int? AudioSampleRate { get; set; }

        /// <summary>
        /// Directory holding file used to get this AudioMetaData
        /// </summary>
        public string Directory
        {
            get
            {
                if (string.IsNullOrEmpty(this.Filename))
                {
                    return null;
                }
                return Path.GetDirectoryName(this.Filename);
            }
        }

        /// <summary>
        /// TPOS
        /// </summary>
        public int? Disc { get; set; }

        /// <summary>
        /// TSST
        /// </summary>
        public string DiscSubTitle { get; set; }

        /// <summary>
        /// Full filename to the file used to get this AudioMetaData
        /// </summary>
        public string Filename { get; set; }

        private FileInfo _fileInfo = null;
        [JsonIgnore]
        public FileInfo FileInfo
        {
            get
            {
                return this._fileInfo ?? (this._fileInfo = new FileInfo(this.Filename));
            }
        }

        public ICollection<string> Genres { get; set; }
        [JsonIgnore]
        public IEnumerable<AudioMetaDataImage> Images { get; set; }

        public string ISRC { get; internal set; }

        public bool IsSoundTrack
        {
            get
            {
                if (this.Genres != null && this.Genres.Any())
                {
                    var soundtrackGenres = new List<string> { "24", "soundtrack" };
                    if (this.Genres.Intersect(soundtrackGenres, StringComparer.OrdinalIgnoreCase).Any())
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
                    this.Artist = this.Artist == null ? null : this.Artist.Equals("Unknown Artist") ? null : this.Artist;
                    this.Release = this.Release == null ? null : this.Release.Equals("Unknown Release") ? null : this.Release;
                    if (!string.IsNullOrEmpty(this.Title))
                    {
                        var trackNumberTitle = string.Format("Track {0}", this.TrackNumber);
                        this.Title = this.Title == trackNumberTitle ? null : this.Title;
                    }
                    return !string.IsNullOrEmpty(this.Artist)
                            && !string.IsNullOrEmpty(this.Release)
                            && !string.IsNullOrEmpty(this.Title)
                            && (this.Year ?? 0) > 0
                            && (this.TrackNumber ?? 0) > 0;
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
        /// TALB
        /// </summary>
        public string Release { get; set; }

        /// <summary>
        /// COMM
        /// </summary>
        public string Comments { get; set; }

        public string ReleaseLastFmId { get; set; }

        public string ReleaseMusicBrainzId { get; set; }

        public ulong? SampleLength { get; set; }

        public string SpecialTitle { get; set; }

        public string SpotifyId { get; set; }

        /// <summary>
        /// TIME
        /// </summary>
        public TimeSpan? Time { get; set; }

        /// <summary>
        /// TIT2
        /// </summary>
        public string Title
        {
            get
            {
                return this.SpecialTitle ?? this._title;
            }
            set
            {
                this._title = value;
                if (IsSoundTrack)
                {
                    this.Artist = AudioMetaData.SoundTrackArtist;
                }
            }
        }

        /// <summary>
        /// Total number of Discs for Media
        /// </summary>
        public int? TotalDiscCount { get; set; }

        public double TotalSeconds
        {
            get
            {
                if (this.Time == null)
                {
                    return 0;
                }
                return this.Time.Value.TotalSeconds;
            }
        }

        /// <summary>
        /// TRCK 0[/OptionalElements]
        /// </summary>
        public int? TotalTrackNumbers { get; set; }

        /// <summary>
        /// TOPE First Contributing Artist, null if same as Artist
        /// </summary>
        public string TrackArtist
        {
            get
            {
                string result = null;
                if (!string.IsNullOrEmpty(this._trackArtist))
                {
                    result = this._trackArtist.Split(AudioMetaData.ArtistSplitCharacter).First().ToTitleCase();
                }
                result = !this._artist?.Equals(result, StringComparison.OrdinalIgnoreCase) ?? false ? result : null;
                return result;
            }
            set
            {
                this._trackArtist = value;
            }
        }

        public string TrackArtistRaw { get; set; }

        /// <summary>
        /// TOPE All Contributing Artists
        /// </summary>
        /// <remarks>Per ID3.Org Spec: They are seperated with the "/" character.</remarks>
        public IEnumerable<string> TrackArtists
        {
            get
            {
                if (string.IsNullOrEmpty(this._trackArtist))
                {
                    return new string[0];
                }
                if (!this._trackArtist.Contains(AudioMetaData.ArtistSplitCharacter.ToString()))
                {
                    if (string.IsNullOrEmpty(this.TrackArtist))
                    {
                        return new string[0];
                    }
                    return new string[1] { this.TrackArtist };
                }
                if (!string.IsNullOrEmpty(this._artist) || !string.IsNullOrEmpty(this._trackArtist))
                {
                    if (!this._artist.Equals(this._trackArtist, StringComparison.OrdinalIgnoreCase))
                    {
                        return this._trackArtist.Split(AudioMetaData.ArtistSplitCharacter).Where(x => !string.IsNullOrEmpty(x)).Select(x => x.ToTitleCase()).OrderBy(x => x).ToArray();
                    }
                }
                return new string[0];
            }
        }

        /// <summary>
        /// TRCK
        /// </summary>
        public short? TrackNumber { get; set; }

        public int ValidWeight
        {
            get
            {
                return (int)this.AudioMetaDataWeights;
            }
        }

        /// <summary>
        /// TYER | TDRC | TORY | TDOR
        /// </summary>
        public int? Year
        {
            get
            {
                return this._year;
            }
            set
            {
                this._year = value < AudioMetaData.MinimumYearValue ? null : value;
            }
        }

        public AudioMetaData()
        {
            this.Images = new AudioMetaDataImage[0];
        }

        public override bool Equals(object obj)
        {
            var item = obj as AudioMetaData;
            if (item == null)
            {
                return false;
            }
            return item.GetHashCode() == this.GetHashCode();
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + this.Artist.GetHashCode();
                hash = hash * 23 + this.Release.GetHashCode();
                hash = hash * 23 + this.Title.GetHashCode();
                hash = hash * 23 + this.TrackNumber.GetHashCode();
                hash = hash * 23 + this.AudioBitrate.GetHashCode();
                hash = hash * 23 + this.AudioSampleRate.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Use this value for the artist name, dont process in any way
        /// </summary>
        /// <param name="name"></param>
        public void SetArtistName(string name)
        {
            this._artist = name;
            this._doModifyArtistNameOnGet = false;
        }

        public override string ToString()
        {
            var result = $"IsValid: {this.IsValid}{ (this.IsSoundTrack ? " [SoundTrack ]" : string.Empty)}, ValidWeight {this.ValidWeight}, Artist: {this.Artist}";
            if(!string.IsNullOrEmpty(this.TrackArtist))
            {
                result += $", TrackArtist: { this.TrackArtist}";
            }
            result += $", Release: {this.Release}, TrackNumber: {this.TrackNumber}, TrackTotal: {this.TotalTrackNumbers}";
            if(this.TotalDiscCount > 1)
            {
                result += $", Disc: { this.Disc }/{ this.TotalDiscCount}";
            }
            result += $", Title: {this.Title}, Year: {this.Year}, Duration: {(this.Time == null ? "-" : this.Time.Value.ToString())}";
            return result;
        }
    }
}