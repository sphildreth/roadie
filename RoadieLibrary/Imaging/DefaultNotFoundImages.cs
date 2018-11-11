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
        private Image _image;
        private Image _label;
        private Image _release;
        private Image _track;
        private Image _user;

        public Image Artist
        {
            get
            {
                return this._artist ?? (this._artist = MakeImageFromFile(MakeImagePath(@"images/artist.jpg")));
            }
        }

        public Image Collection
        {
            get
            {
                return this._artist ?? (this._artist = MakeImageFromFile(MakeImagePath(@"images/collection.jpg")));
            }
        }

        public Image Image
        {
            get
            {
                return this._artist ?? (this._artist = MakeImageFromFile(MakeImagePath(@"images/image.jpg")));
            }
        }

        public Image Label
        {
            get
            {
                return this._artist ?? (this._artist = MakeImageFromFile(MakeImagePath(@"images/label.jpg")));
            }
        }

        public Image Release
        {
            get
            {
                return this._artist ?? (this._artist = MakeImageFromFile(MakeImagePath(@"images/release.jpg")));
            }
        }

        public Image Track
        {
            get
            {
                return this._artist ?? (this._artist = MakeImageFromFile(MakeImagePath(@"images/track.jpg")));
            }
        }

        public Image User
        {
            get
            {
                return this._artist ?? (this._artist = MakeImageFromFile(MakeImagePath(@"images/user.jpg")));
            }
        }

        private IRoadieSettings Configuration { get; }
        private ILogger Logger { get; }

        public DefaultNotFoundImages(IRoadieSettings configuration, ILoggerFactory logger)
        {
            this.Configuration = configuration;
            this.Logger = logger.CreateLogger("DefaultNotFoundImages");
        }

        private static Image MakeImageFromFile(string filename)
        {
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
            return Path.Combine(this.Configuration.ContentPath, filename);
        }
    }
}