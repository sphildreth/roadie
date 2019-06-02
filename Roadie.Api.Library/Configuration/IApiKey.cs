namespace Roadie.Library.Configuration
{
    public interface IApiKey
    {
        string ApiName { get; set; }
        string Key { get; set; }
        string KeySecret { get; set; }
    }
}