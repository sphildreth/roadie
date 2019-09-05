using Roadie.Dlna.Utility;
namespace Roadie.Dlna.Server.Views
{
    public interface IView : IRepositoryItem
    {
        IMediaFolder Transform(IMediaFolder oldRoot);
    }
}