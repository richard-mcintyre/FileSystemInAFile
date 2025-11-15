using System;
using System.Collections.Generic;
using System.Text;

namespace FsRepl;

internal interface ICommandOptions
{
    bool IsValid() => true;
}
