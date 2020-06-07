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

        [Option("-ss", "Don't Run Prescript", CommandOptionType.SingleOrNoValue)]
        public bool DontRunPreScript { get; }

        public static int Main(string[] args) => CommandLineApplication.Execute<Program>(args);

#pragma warning disable RCS1213 // Remove unused member declaration.
#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable CRR0026
        private void OnExecute()
#pragma warning restore IDE0051 // Remove unused private members
#pragma warning restore RCS1213 // Remove unused member declaration.
#pragma warning restore CRR0026
        {
            var inspector = new Roadie.Library.Inspect.Inspector();
            inspector.Inspect(DoCopy, IsReadOnly, Folder, Destination ?? Folder, DontAppendSubFolder, IsReadOnly ? true : DontDeleteEmptyFolders, DontRunPreScript);
        }
    }
}