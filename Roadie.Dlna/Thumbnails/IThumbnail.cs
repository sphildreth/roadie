namespace Roadie.Dlna.Thumbnails
{
    public interface IThumbnail
    {
        int Height { get; }

        int Width { get; }

        byte[] GetData();
    }
}