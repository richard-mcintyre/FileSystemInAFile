using System;
using System.Collections.Generic;
using System.Text;
using Fs.Cli.Common;

namespace FsRepl;

internal interface ICommand
{
    string Name { get; }

    string Description { get; }

    CommandResults? Execute(ExecuteSettings settings, string[] args);
}
