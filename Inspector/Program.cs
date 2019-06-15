using McMaster.Extensions.CommandLineUtils;
using System.ComponentModel.DataAnnotations;

namespace Inspector
{
    public class Program
    {
        [Option(ShortName = "d", Description = "Destination Folder")]
        public string Destination { get; }

        [Option("-c", "Copy Dont Move Originals", CommandOptionType.NoValue)]
        public bool DoCopy { get; }

        [Option("-s", "Don't append a subfolder to the Destination folder", CommandOptionType.NoValue)]
        public bool DontAppendSubFolder { get; }

        [Option("-x", "Don't delete empty folders after inspection, if moving", CommandOptionType.NoValue)]
        public bool DontDeleteEmptyFolders { get; }

        [Option(ShortName = "f", Description = "Folder To Inspect")]
        [Required]
        public string Folder { get; }

        [Option("-r", "Only show what would be done, don't modify any files", CommandOptionType.NoValue)]
        public bool IsReadOnly { get; }

        public static int Main(string[] args) => CommandLineApplication.Execute<Program>(args);

        private void OnExecute()
        {
            var inspector = new Roadie.Library.Inspect.Inspector();
            inspector.Inspect(DoCopy, IsReadOnly, Folder, Destination ?? Folder, DontAppendSubFolder, IsReadOnly ? true : DontDeleteEmptyFolders);
        }
    }
}