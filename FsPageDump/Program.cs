using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using FileSystemInAFile;
using Fs.Cli.Common.Options;

namespace FsPageDump;

internal class Program
{
    record FileSystemInfo(uint TotalPages, ushort PageSize);

    record PageInfo(uint Id, string Kind, uint NextPageId, bool IsFree);


    static void Main(string[] args)
    {
        OptionDefinitions optionDefs = new OptionDefinitions();
        optionDefs.AddUnnamed("Path", "Path to the File System file.");
        optionDefs.AddUnnamed("Pages", "Comma separated list of page numbers or ranges (e.g. 1,2,5-10,18-*).");
        optionDefs.AddNamed("hex", "Dump the page contents in hex format.");
        optionDefs.AddNamedWithValue("hex", "Dump the page contents in hex format with the specified number of bytes per line (default is 16).");

        ProgramArgs? pargs = OptionsParser.TryParse<ProgramArgs>(optionDefs, args);
        if (pargs is null)
        {
            optionDefs.PrintUsage("FsPageDump");
            return;
        }

        Run(pargs);
    }

    private static void Run(ProgramArgs args)
    {
        if (!Path.Exists(args.Path))
        {
            Console.WriteLine($"File system file '{args.Path}' does not exist.");
            return;
        }

        Range[] ranges = ParseRanges(args.Pages).OrderBy(o => o.Start.Value).ToArray();

        if(args.BytesPerLine.HasValue && args.BytesPerLine.Value == 0)
            args = args with { BytesPerLine = 16 };

        using (FileSystem fs = FileSystem.OpenExisting(args.Path))
        {
            FileSystemInfo fsInfo;

            using (Stream stream = fs.Open("$fs.info$", FileSystemFileMode.Read))
            {                
                fsInfo = JsonSerializer.Deserialize<FileSystemInfo>(stream)!;
            }

            Console.WriteLine($"Total Pages: {fsInfo.TotalPages}");
            Console.WriteLine($"Page Size  : {fsInfo.PageSize}");
            Console.WriteLine();

            HashSet<uint> pageIdsDumped = new HashSet<uint>();
            foreach ((int offset, int length) in ranges.Select(o => o.GetOffsetAndLength((int)fsInfo.TotalPages)))
            {
                for (uint curPageId = (uint)offset; curPageId < offset + length; curPageId++)
                {
                    if (pageIdsDumped.Contains(curPageId))
                        continue;

                    pageIdsDumped.Add(curPageId);

                    PageInfo pageInfo;
                    using (Stream stream = fs.Open($"$fs.pageinfo$:{curPageId}", FileSystemFileMode.Read))
                    {
                        pageInfo = JsonSerializer.Deserialize<PageInfo>(stream)!;
                    }

                    string pageKindAsString = pageInfo.IsFree ? "Free" : pageInfo.Kind;

                    if (pageInfo.IsFree || pageInfo.NextPageId == 0)
                    {
                        Console.WriteLine($"Page: {curPageId,-10} {pageKindAsString}");
                    }
                    else
                    {
                        Console.WriteLine($"Page: {curPageId,-10} {pageKindAsString}     Next Page: {pageInfo.NextPageId}");
                    }

                    if (args.BytesPerLine.HasValue)
                    {
                        DumpPageContents(fs, curPageId, args.BytesPerLine.Value);
                        Console.WriteLine();
                    }
                }
            }
        }
    }

    private static void DumpPageContents(FileSystem fs, uint pageId, int bytesPerLine)
    {
        using (Stream stream = fs.Open($"$fs.pagecontents$:{pageId}", FileSystemFileMode.Read))
        {
            int lineLength = 6 + (bytesPerLine * 3) + (bytesPerLine / 8) + 1;

            byte[] buffer = new byte[bytesPerLine];
            while (true)
            {
                long position = stream.Position;

                int read = stream.Read(buffer, 0, buffer.Length);
                if (read == 0)
                    break;

                StringBuilder sb = new StringBuilder();
                StringBuilder sbAscii = new StringBuilder();

                sb.Append($"{position:x4}: ");

                for (int i = 0; i < buffer.Length; i++)
                {
                    if (i == read)
                        break;

                    if (i != 0 && (i % 8) == 0)
                        sb.Append("- ");

                    byte val = buffer[i];
                    sb.Append($"{val:x2} ");
                    sbAscii.Append(val >= 32 && val <= 126 ? (char)val : '.');
                }

                Console.Write(sb.ToString().PadRight(lineLength));
                Console.Write($" {sbAscii}");
                Console.WriteLine();
            }
        }
    }

    private static IEnumerable<Range> ParseRanges(string rangesAsString)
    {
        foreach(string part in rangesAsString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if(part.Contains('-'))
            {
                string[] bounds = part.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if(bounds.Length != 2)
                    throw new ArgumentException($"Invalid range specification: '{part}'");

                if (bounds[0] == "*")
                    throw new ArgumentException($"Invalid range specification: '{part}'");

                int start = Int32.Parse(bounds[0]);
                int end = bounds[1] == "*" ? Int32.MaxValue : Int32.Parse(bounds[1]) + 1;
                yield return new Range(start, end == Int32.MaxValue ? Index.End : new Index(end));
            }
            else
            {
                if (part == "*")
                {
                    yield return new Range(Index.Start, Index.End);
                }
                else
                {
                    int value = Int32.Parse(part);
                    yield return new Range(value, value + 1);
                }
            }
        }
    }
}