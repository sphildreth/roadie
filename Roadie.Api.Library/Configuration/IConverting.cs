namespace Roadie.Library.Configuration
{
    public interface IConverting
    {
        string APEConvertCommand { get; set; }
        bool ConvertingEnabled { get; set; }
        bool DoDeleteAfter { get; set; }
        string M4AConvertCommand { get; set; }
        string OGGConvertCommand { get; set; }
    }
}