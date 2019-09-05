using Roadie.Dlna.Utility;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace Roadie.Dlna.Server
{
    [Serializable]
    public sealed class Subtitle : IMediaResource
    {
        [NonSerialized]
        private static readonly string[] exts =
        {
      ".srt", ".SRT",
      ".ass", ".ASS",
      ".ssa", ".SSA",
      ".sub", ".SUB",
      ".vtt", ".VTT"
    };

        [NonSerialized] private byte[] encodedText;

        private string text;

        public IMediaCoverResource Cover
        {
            get { throw new NotImplementedException(); }
        }

        public bool HasSubtitle => !string.IsNullOrWhiteSpace(text);

        public string Id
        {
            get { return Path; }
            set { throw new NotImplementedException(); }
        }

        public DateTime InfoDate => DateTime.UtcNow;

        public long? InfoSize
        {
            get
            {
                try
                {
                    using (var s = CreateContentStream())
                    {
                        return s.Length;
                    }
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        public DlnaMediaTypes MediaType
        {
            get { throw new NotImplementedException(); }
        }

        public string Path => "ad-hoc-subtitle:";

        public string PN => DlnaMaps.MainPN[Type];

        public IHeaders Properties
        {
            get
            {
                var rv = new RawHeaders { { "Type", Type.ToString() } };
                if (InfoSize.HasValue)
                {
                    rv.Add("SizeRaw", InfoSize.ToString());
                    rv.Add("Size", InfoSize.Value.FormatFileSize());
                }
                rv.Add("Date", InfoDate.ToString(CultureInfo.InvariantCulture));
                rv.Add("DateO", InfoDate.ToString("o"));
                return rv;
            }
        }

        public string Title
        {
            get { throw new NotImplementedException(); }
        }

        public DlnaMime Type => DlnaMime.SubtitleSRT;

        public Subtitle()
        {
        }

        public Subtitle(FileInfo file)
        {
            Load(file);
        }

        public Subtitle(string text)
        {
            this.text = text;
        }

        public int CompareTo(IMediaItem other)
        {
            throw new NotImplementedException();
        }

        public Stream CreateContentStream()
        {
            if (!HasSubtitle)
            {
                throw new NotSupportedException();
            }
            if (encodedText == null)
            {
                encodedText = Encoding.UTF8.GetBytes(text);
            }
            return new MemoryStream(encodedText, false);
        }

        public bool Equals(IMediaItem other)
        {
            throw new NotImplementedException();
        }

        public string ToComparableTitle()
        {
            throw new NotImplementedException();
        }

        private void Load(FileInfo file)
        {
            try
            {
                // Try external
                foreach (var i in exts)
                {
                    var sti = new FileInfo(
                      System.IO.Path.ChangeExtension(file.FullName, i));
                    try
                    {
                        if (!sti.Exists)
                        {
                            sti = new FileInfo(file.FullName + i);
                        }
                        if (!sti.Exists)
                        {
                            continue;
                        }
                        text = FFmpeg.GetSubtitleSubrip(sti);
                        Trace.WriteLine($"Loaded subtitle from {sti.FullName}");
                    }
                    catch (NotSupportedException)
                    {
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine($"Failed to get subtitle from {sti.FullName} Ex [{ ex }]");
                    }
                }
                try
                {
                    text = FFmpeg.GetSubtitleSubrip(file);
                    Trace.WriteLine($"Loaded subtitle from {file.FullName}");
                }
                catch (NotSupportedException ex)
                {
                    Trace.WriteLine($"Subtitle not supported {file.FullName} Ex [{ ex }]");
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Failed to get subtitle from {file.FullName} Ex [{ ex }]");
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Failed to load subtitle for {file.FullName} Ex [{ ex }]");
            }
        }
    }
}