using System.IO;

namespace Roadie.Library.Inspect.Plugins.Directory
{
    public interface IInspectorDirectoryPlugin
    {
        string Description { get; }
        int Order { get; }

        OperationResult<string> Process(DirectoryInfo directory);
    }
}