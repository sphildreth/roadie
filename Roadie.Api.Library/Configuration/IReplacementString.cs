namespace Roadie.Library.Configuration
{
    public interface IReplacementString
    {
        string Key { get; set; }
        int Order { get; set; }
        string ReplaceWith { get; set; }
    }
}