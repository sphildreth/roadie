namespace Roadie.Dlna.Server
{
    internal interface IPrefixHandler : IHandler
    {
        string Prefix { get; }
    }
}