using System.Text;
using System.Text.Json.Serialization;
using FileSystemInAFile;
using Fs.Cli.Common;
using Fs.Cli.Common.Options;

namespace FsDir;

internal class Program
{
    static void Main(string[] args)
    {
        OptionDefinitions optionDefs = new OptionDefinitions();
        optionDefs.AddUnnamed("FsPath", "Path to the File System file.");
        optionDefs.AddUnnamed("Path", "Path within the File System to list.");
        optionDefs.AddNamed("s", "Include subdirectories.");

        ProgramArgs? pargs = OptionsParser.TryParse<ProgramArgs>(optionDefs, args);
        if (pargs is null || !pargs.IsValid())
        {
            optionDefs.PrintUsage("FsDir", "Lists the contents of a directory in a File System file.");
            return;
        }

        Run(pargs);
    }

    private static void Run(ProgramArgs args)
    {        
        if (!Path.Exists(args.FsPath))
        {
            Console.WriteLine($"File system file '{args.FsPath}' does not exist.");
            return;
        }

        AnsiTextWriter stdout = new AnsiTextWriter(Console.Out)
        {
            EnableColors = true
        };

        using (FileSystem fs = FileSystem.OpenExisting(args.FsPath))
        {            
            DirectoryListingWriter writer = new DirectoryListingWriter(fs);
            writer.Write(args.Path, args.IncludeSubdirectories, stdout);
        }
    }
}
