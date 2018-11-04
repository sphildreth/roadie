using System;

namespace Roadie.Library.Configuration
{
    [Serializable]
    public class Thumbnails
    {
        public short Height { get; set; }
        public short Width { get; set; }

        public Thumbnails()
        {
            this.Height = 80;
            this.Width = 80;
        }
    }
}