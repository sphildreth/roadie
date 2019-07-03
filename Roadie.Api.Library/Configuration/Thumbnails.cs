using System;

namespace Roadie.Library.Configuration
{
    [Serializable]
    public class ImageSize : IImageSize
    {
        public short Height { get; set; }

        public short Width { get; set; }

        public ImageSize()
        {
            Height = 80;
            Width = 80;
        }
    }
}