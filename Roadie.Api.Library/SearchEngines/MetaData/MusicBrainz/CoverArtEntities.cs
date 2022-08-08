namespace Roadie.Library.MetaData.MusicBrainz
{
    public class CoverArtArchivesResult
    {
        public Image[] images { get; set; }

        public string release { get; set; }
    }

    public class Image
    {
        public bool approved { get; set; }

        public bool back { get; set; }

        public string comment { get; set; }

        public int edit { get; set; }

        public bool front { get; set; }

        public string image { get; set; }

        public Thumbnails thumbnails { get; set; }

        public string[] types { get; set; }
    }

    public class Thumbnails
    {
        public string large { get; set; }

        public string small { get; set; }
    }
}
