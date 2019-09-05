namespace Roadie.Dlna.Server
{
    internal sealed class StaticHandler : IPrefixHandler
    {
        private readonly IResponse response;

        public string Prefix { get; }

        public StaticHandler(IResponse aResponse)
              : this("#", aResponse)
        {
        }

        public StaticHandler(string aPrefix, IResponse aResponse)
        {
            Prefix = aPrefix;
            response = aResponse;
        }

        public IResponse HandleRequest(IRequest req)
        {
            return response;
        }
    }
}