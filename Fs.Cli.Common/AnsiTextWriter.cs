using System;
using System.Collections.Generic;
using System.Text;

namespace Fs.Cli.Common;

public class AnsiTextWriter : TextWriter
{
    #region Construction

    public AnsiTextWriter(TextWriter output)
    {
        _output = output;
    }

    #endregion

    #region Fields

    private readonly TextWriter _output;
    private bool _inEscapeSequence;

    #endregion

    #region Properties

    public override Encoding Encoding => Encoding.UTF8;

    public bool EnableColors { get; set; } = true;

    #endregion

    #region Methods

    public override void Write(char value)
    {
        if (!this.EnableColors)
        {
            if (_inEscapeSequence)
            {
                if (value == 'm')
                    _inEscapeSequence = false;

                return;
            }
            else if (!_inEscapeSequence)
            {
                if (value == '\u001b')
                {
                    _inEscapeSequence = true;
                    return;
                }
            }
        }

        _output.Write(value);
    }

    #endregion
}
