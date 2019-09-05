using Microsoft.Extensions.Logging;
using Roadie.Library.Configuration;
using Roadie.Library.Data;
using System;
using System.IO;

namespace Roadie.Library.Imaging
{
    public class DefaultNotFoundImages : IDefaultNotFoundImages
    {
        private Image _artist;
        private Image _collection;
        private Image _label;
        private Image _genre;
        private Image _playlist;
        private Image _release;
        private Image _track;
        private Image _user;

        public Image Artist => _artist ?? (_artist = MakeImageFromFile(MakeImagePath(@"images/artist.jpg")));

        public Image Collection =>
            _collection ?? (_collection = MakeImageFromFile(MakeImagePath(@"images/collection.jpg")));

        public Image Label => _label ?? (_label = MakeImageFromFile(MakeImagePath(@"images/label.jpg")));

        public Image Genre => _genre ?? (_genre = MakeImageFromFile(MakeImagePath(@"images/genre.jpg")));

        public Image Playlist => _playlist ?? (_playlist = MakeImageFromFile(MakeImagePath(@"images/playlist.jpg")));

        public Image Release => _release ?? (_release = MakeImageFromFile(MakeImagePath(@"images/release.jpg")));

        public Image Track => _track ?? (_track = MakeImageFromFile(MakeImagePath(@"images/track.jpg")));

        public Image User => _user ?? (_user = MakeImageFromFile(MakeImagePath(@"images/user.jpg")));

        private IRoadieSettings Configuration { get; }

        private ILogger Logger { get; }

        public DefaultNotFoundImages(ILogger<DefaultNotFoundImages> logger, IRoadieSettings configuration)
        {
            Configuration = configuration;
            Logger = logger;
        }

        public DefaultNotFoundImages(ILogger logger, IRoadieSettings configuration)
        {
            Configuration = configuration;
            Logger = logger;
        }

        private static Image MakeImageFromFile(string filename)
        {
            if (!File.Exists(filename)) return new Image();
            var bytes = File.ReadAllBytes(filename);
            return new Image
            {
                Bytes = bytes,
                CreatedDate = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow
            };
        }

        private string MakeImagePath(string filename)
        {
            if (string.IsNullOrEmpty(filename)) return null;
            var path = Path.Combine(Configuration.ContentPath, filename);
            if (!File.Exists(path))
                Logger.LogWarning("Unable To Find Path [{0}], ContentPath [{1}]", path, Configuration.ContentPath);
            return path;
        }
    }
}