namespace Roadie.Library.Configuration
{
    public interface IInspector
    {
        bool DoCopyFiles { get; set; }
        bool IsInReadOnlyMode { get; set; }
    }
}