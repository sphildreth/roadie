namespace Roadie.Dlna.Server.Views
{
    public interface IFilteredView : IView
    {
        bool Allowed(IMediaResource item);
    }
}