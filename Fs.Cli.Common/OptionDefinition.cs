using System;
using System.Collections.Generic;
using System.Text;

namespace Fs.Cli.Common;

public record class OptionDefinition(OptionKind Kind, string Name, string Description);
