namespace Roadie.Library.Configuration
{
    public interface IConverting
    {
        bool ConvertingEnabled { get; set; }
        string APEConvertCommand { get; set; }
        bool DoDeleteAfter { get; set; }
        string M4AConvertCommand { get; set; }
        string OGGConvertCommand { get; set; }
    }
}