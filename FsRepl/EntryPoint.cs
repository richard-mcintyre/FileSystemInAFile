using System;
using System.Collections.Generic;
using System.Text;
using FileSystemInAFile;
using Fs.Cli.Common;

namespace FsRepl;

internal class EntryPoint
{
    static void Main(string[] args)
    {
        OptionDefinitions defs = new OptionDefinitions();
        defs.AddUnnamed("Path", "Path to the File System file to open.");
        defs.AddNamed("CreateNew", "Create the File System file");
        defs.AddNamedWithValue("PageSize", "If CreateNew is specified, then this is the size of the pages (Default=8192)");

        ProgramArgs? pargs = OptionsParser.TryParse<ProgramArgs>(defs, args);
        if(pargs is null || String.IsNullOrEmpty(pargs?.Path))
        {
            defs.PrintUsage("FsRepl");
            return;
        }

        try
        {
            FileSystem? initialFS = null;
            if (pargs.CreateNew)
            {
                initialFS = FileSystem.CreateNew(pargs.Path, pargs.CreateNewPageSize);
            }
            else
            {
                initialFS = FileSystem.OpenExisting(pargs.Path);
            }

            Program program = new Program();
            program.Run(initialFS);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}
