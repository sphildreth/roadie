using Roadie.Library.Data;

namespace Roadie.Library.Imaging
{
    public interface IDefaultNotFoundImages
    {
        Image Artist { get; }
        Image Collection { get; }
        Image Image { get; }
        Image Label { get; }
        Image Release { get; }
        Image Track { get; }
        Image User { get; }
    }
}