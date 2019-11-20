namespace Roadie.Library.Configuration
{
    public interface IFileDatabaseOptions
    {
        string DatabaseFolder { get; set; }
        FileDatabaseFormat DatabaseFormat { get; set; }
    }
}