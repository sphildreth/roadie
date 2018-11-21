using System;

namespace Roadie.Library.Configuration
{
    [Serializable]
    public class ImageSize
    {
        public short Height { get; set; }
        public short Width { get; set; }

        public ImageSize()
        {
            this.Height = 80;
            this.Width = 80;
        }
    }
}