using Microsoft.Extensions.Logging;
using Roadie.Library.Configuration;
using System;
using System.IO;

namespace Roadie.Library.Imaging
{
    public class DefaultNotFoundImages : IDefaultNotFoundImages
    {
        private IImage _artist;
        private IImage _collection;
        private IImage _label;
        private IImage _genre;
        private IImage _playlist;
        private IImage _release;
        private IImage _track;
        private IImage _user;

        public IImage Artist => _artist ?? (_artist = MakeImageFromFile(MakeImagePath(@"images/artist.jpg")));

        public IImage Collection =>
            _collection ?? (_collection = MakeImageFromFile(MakeImagePath(@"images/collection.jpg")));

        public IImage Label => _label ?? (_label = MakeImageFromFile(MakeImagePath(@"images/label.jpg")));

        public IImage Genre => _genre ?? (_genre = MakeImageFromFile(MakeImagePath(@"images/genre.jpg")));

        public IImage Playlist => _playlist ?? (_playlist = MakeImageFromFile(MakeImagePath(@"images/playlist.jpg")));

        public IImage Release => _release ?? (_release = MakeImageFromFile(MakeImagePath(@"images/release.jpg")));

        public IImage Track => _track ?? (_track = MakeImageFromFile(MakeImagePath(@"images/track.jpg")));

        public IImage User => _user ?? (_user = MakeImageFromFile(MakeImagePath(@"images/user.jpg")));

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

        private static IImage MakeImageFromFile(string filename)
        {
            if (!File.Exists(filename))
            {
                return new Image();
            }
            var bytes = File.ReadAllBytes(filename);
            return new Image
            {
                Bytes = bytes,
                CreatedDate = DateTime.UtcNow            
            };
        }

        private string MakeImagePath(string filename)
        {
            if (string.IsNullOrEmpty(filename))
            {
                return null;
            }
            var path = Path.Combine(Configuration.ContentPath, filename);
            if (!File.Exists(path))
            {
                Logger.LogWarning("Unable To Find Path [{0}], ContentPath [{1}]", path, Configuration.ContentPath);
            }
            return path;
        }
    }
}