using Roadie.Library.Data;

namespace Roadie.Library.Imaging
{
    public interface IDefaultNotFoundImages
    {
        IImage Artist { get; }
        IImage Collection { get; }
        IImage Label { get; }
        IImage Genre { get; }
        IImage Playlist { get; }
        IImage Release { get; }
        IImage Track { get; }
        IImage User { get; }
    }
}