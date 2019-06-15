using System.IO;

namespace Roadie.Library.Inspect.Plugins.Directory
{
    public interface IInspectorDirectoryPlugin
    {
        bool IsEnabled { get; }
        bool IsPostProcessingPlugin { get; }
        string Description { get; }
        int Order { get; }

        OperationResult<string> Process(DirectoryInfo directory);
    }
}