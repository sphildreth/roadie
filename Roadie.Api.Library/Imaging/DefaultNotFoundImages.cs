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
        private Image _playlist;
        private Image _release;
        private Image _track;
        private Image _user;

        private ILogger Logger { get; }

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
                return this._collection ?? (this._collection = MakeImageFromFile(MakeImagePath(@"images/collection.jpg")));
            }
        }


        public Image Label
        {
            get
            {
                return this._label ?? (this._label = MakeImageFromFile(MakeImagePath(@"images/label.jpg")));
            }
        }

        public Image Playlist
        {
            get
            {
                return this._playlist ?? (this._playlist = MakeImageFromFile(MakeImagePath(@"images/playlist.jpg")));
            }
        }

        public Image Release
        {
            get
            {
                return this._release ?? (this._release = MakeImageFromFile(MakeImagePath(@"images/release.jpg")));
            }
        }

        public Image Track
        {
            get
            {
                return this._track ?? (this._track = MakeImageFromFile(MakeImagePath(@"images/track.jpg")));
            }
        }

        public Image User
        {
            get
            {
                return this._user ?? (this._user = MakeImageFromFile(MakeImagePath(@"images/user.jpg")));
            }
        }

        private IRoadieSettings Configuration { get; }

        public DefaultNotFoundImages(ILogger<DefaultNotFoundImages> logger, IRoadieSettings configuration)
        {
            this.Configuration = configuration;
            this.Logger = logger;
        }

        private static Image MakeImageFromFile(string filename)
        {
            if(!File.Exists(filename))
            {
                return new Image();
            }
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
            if(string.IsNullOrEmpty(filename))
            {
                return null;
            }
            var path=  Path.Combine(this.Configuration.ContentPath, filename);
            if(!File.Exists(path))
            {
                this.Logger.LogWarning("Unable To Find Path [{0}], ContentPath [{1}]", path, this.Configuration.ContentPath);   
            }
            return path;
        }
    }
}