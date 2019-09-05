namespace Roadie.Dlna.Server
{
    internal interface IHandler
    {
        IResponse HandleRequest(IRequest request);
    }
}