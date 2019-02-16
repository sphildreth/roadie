using System;
using System.ComponentModel.DataAnnotations;
using McMaster.Extensions.CommandLineUtils;

namespace Inspector
{
    public class Program
    {
        public static int Main(string[] args)
            => CommandLineApplication.Execute<Program>(args);

        [Option(ShortName = "f", Description = "Folder To Inspect")]
        [Required]
        public string Folder { get; }

        [Option(ShortName = "d", Description = "Destination Folder")]
        public string Destination { get; }

        [Option("-c", "Copy Dont Move Originals", CommandOptionType.NoValue)]
        public bool DoCopy { get; }

        private void OnExecute()
        {
            var inspector = new Roadie.Library.Inspect.Inspector();
            inspector.Inspect(this.DoCopy, this.Folder, this.Destination ?? this.Folder);
            
        }
    }
}
