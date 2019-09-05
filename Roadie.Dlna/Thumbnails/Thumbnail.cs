namespace Roadie.Dlna.Thumbnails
{
    internal sealed class Thumbnail : IThumbnail
    {
        private readonly byte[] data;

        public int Height { get; }

        public int Width { get; }

        internal Thumbnail(int width, int height, byte[] data)
        {
            Width = width;
            Height = height;
            this.data = data;
        }

        public byte[] GetData()
        {
            return data;
        }
    }
}