using System;
using System.Collections.Generic;
using System.Text;

namespace Fs.Cli.Common;

public enum AnsiFgColors
{
    Black = 30,
    Red = 31,
    Green = 32,
    Yellow = 33,
    Blue = 34,
    Magenta = 35,
    Cyan = 36,
    White = 37,
    BrightBlack = 90,
    BrightRed = 91,
    BrightGreen = 92,
    BrightYellow = 93,
    BrightBlue = 94,
    BrightMagenta = 95,
    BrightCyan = 96,
    BrightWhite = 97
}

public enum AnsiBgColors
{
    Black = 40,
    Red = 41,
    Green = 42,
    Yellow = 43,
    Blue = 44,
    Magenta = 45,
    Cyan = 46,
    White = 47,
    BrightBlack = 100,
    BrightRed = 101,
    BrightGreen = 102,
    BrightYellow = 103,
    BrightBlue = 104,
    BrightMagenta = 105,
    BrightCyan = 106,
    BrightWhite = 107
}

public static class AnsiHelper
{
    public static string FgColor(AnsiFgColors color) =>
        $"\u001b[{(int)color}m";

    public static string BgColor(AnsiBgColors color) =>
        $"\u001b[{(int)color}m";

    public static string BgFgColor(AnsiBgColors bgColor, AnsiFgColors fgColor) =>
        $"\u001b[{(int)fgColor};{(int)bgColor}m";

    public static string Reset =>
        "\u001b[0m";
}
