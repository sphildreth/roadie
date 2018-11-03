using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Roadie.Library.Extensions;
using System.IO;

namespace Roadie.Library.MetaData.Audio
{
    [Serializable]
    [DebuggerDisplay("Artist: {Artist}, TrackArtist: {TrackArtist}, Release: {Release}, TrackNumber: {TrackNumber}, Title: {Title}, Year: {Year}")]
    public sealed class AudioMetaData
    {
        public const char ArtistSplitCharacter = '/';

        private bool _doModifyArtistNameOnGet = true;

        private string _trackArtist = null;
        private string _artist = null;

        public const string SoundTrackArtist = "Sound Tracks";
        public const int MinimumYearValue = 1900;

        /// <summary>
        /// Full filename to the file used to get this AudioMetaData
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// Directory holding file used to get this AudioMetaData
        /// </summary>
        public string Directory
        {
            get
            {
                if(string.IsNullOrEmpty(this.Filename))
                {
                    return null;
                }
                return Path.GetDirectoryName(this.Filename);
            }
        }

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

        public ICollection<string> Genres { get; set; }

        public string ArtistRaw { get; set; }       
          

        /// <summary>
        /// TPE1 All Lead Artists       
        /// <seealso cref="http://id3.org/id3v2.3.0"/>
        /// </summary>
        /// <remarks>Per ID3.Org Spec: The 'Lead artist(s)/Lead performer(s)/Soloist(s)/Performing group' is used for the main artist(s). They are seperated with the "/" character.</remarks>
        public IEnumerable<string> Artists
        {
            get
            {
                if(string.IsNullOrEmpty(this._artist))
                {
                    return new string[0];
                }
                if(!this._artist.Contains(AudioMetaData.ArtistSplitCharacter.ToString()))
                {
                    return new string[0];
                }
                return this._artist.Split(AudioMetaData.ArtistSplitCharacter).Select(x => x.ToTitleCase()).ToArray();
            }
        }

        /// <summary>
        /// TOPE First Contributing Artist
        /// </summary>
        public string TrackArtist
        {
            get
            {
                if (!string.IsNullOrEmpty(this._trackArtist) && this._trackArtist.Contains(AudioMetaData.ArtistSplitCharacter.ToString()))
                {
                    return this._trackArtist.Split(AudioMetaData.ArtistSplitCharacter).First().ToTitleCase();
                }
                if(!string.IsNullOrEmpty(this._artist) || !string.IsNullOrEmpty(this._trackArtist))
                {
                    return !this._artist.Equals(this._trackArtist, StringComparison.OrdinalIgnoreCase) ? this._trackArtist : null;
                }
                return null;
            }
            set
            {
                this._trackArtist = value;
                if (!string.IsNullOrEmpty(this._trackArtist))
                {
                    this._trackArtist = this._trackArtist.Replace(';', AudioMetaData.ArtistSplitCharacter).ToTitleCase();
                }
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
                    return new string[1] { this.TrackArtist };
                }
                if (!string.IsNullOrEmpty(this._artist) || !string.IsNullOrEmpty(this._trackArtist))
                {
                    if(!this._artist.Equals(this._trackArtist, StringComparison.OrdinalIgnoreCase))
                    {
                        return this._trackArtist.Split(AudioMetaData.ArtistSplitCharacter).Select(x => x.ToTitleCase()).ToArray();
                    }
                }
                return new string[0];
            }
        }

        

        /// <summary>
        /// TALB
        /// </summary>
        public string Release { get; set; }

        /// <summary>
        /// TIT2
        /// </summary>
        private string _title = null;

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

        public string SpecialTitle { get; set; }

        /// <summary>
        /// TYER
        /// </summary>
        private int? _year = null;

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

        /// <summary>
        /// TPOS
        /// </summary>
        public int? Disk { get; set; }

        /// <summary>
        /// TRCK
        /// </summary>
        public short? TrackNumber { get; set; }

        /// <summary>
        /// TRCK 0[/OptionalElements]
        /// </summary>
        public int? TotalTrackNumbers { get; set; }

        public int? AudioBitrate { get; set; }

        public int? AudioChannels { get; set; }

        public int? AudioSampleRate { get; set; }

        public string ReleaseMusicBrainzId { get; set; }

        public string ReleaseLastFmId { get; set; }

        public string LastFmId { get; set; }

        public string MusicBrainzId { get; set; }
        public string AmgId { get; set; }

        public string SpotifyId { get; set; }

        /// <summary>
        /// TIME
        /// </summary>
        public TimeSpan? Time { get; set; }

        public ulong? SampleLength { get; set; }

        public IEnumerable<AudioMetaDataImage> Images { get; set; }

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
                if (this.TotalSeconds > 1)
                {
                    result |= AudioMetaDataWeights.Time;
                }
                return result;
            }
        }

        public int ValidWeight
        {
            get
            {
                return (int)this.AudioMetaDataWeights;
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
            return string.Format("IsValid: {0}{7}, ValidWeight {1}, Artist: {2}, Release: {3}, TrackNumber: {4}, Title: {5}, Year: {6}, Duration: {8}",
                                  this.IsValid,
                                  this.ValidWeight,
                                  this.Artist,
                                  this.Release,
                                  this.TrackNumber,
                                  this.Title,
                                  this.Year,
                                  this.IsSoundTrack ? " [SoundTrack ]" : string.Empty,
                                  this.Time == null ? "-" : this.Time.Value.ToString());
        }

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

        public string ISRC { get; internal set; }
    }
}