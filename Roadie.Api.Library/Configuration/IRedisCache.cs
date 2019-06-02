namespace Roadie.Library.Configuration
{
    public interface IRedisCache
    {
        string ConnectionString { get; set; }
    }
}