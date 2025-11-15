using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FileSystemInAFile;
using Fs.Cli.Common.Options;

namespace FsDel;

internal class Program
{
    static void Main(string[] args)
    {
        OptionDefinitions optionDefs = new OptionDefinitions();
        optionDefs.AddUnnamed("FsPath", "Path to the File System file.");
        optionDefs.AddUnnamed("Path", "Path to the file to delete.");

        ProgramArgs? pargs = OptionsParser.TryParse<ProgramArgs>(optionDefs, args);
        if (pargs is null || !pargs.IsValid())
        {
            optionDefs.PrintUsage("FsDel");
            return;
        }

        Run(pargs);
    }

    private static void Run(ProgramArgs args)
    {
        using (FileSystem fs = FileSystem.OpenExisting(args.FsPath))
        {
            fs.Delete(args.Path);
        }
    }
}
