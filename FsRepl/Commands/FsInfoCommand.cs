using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FileSystemInAFile;

namespace FsRepl.Commands;

internal class FsInfoCommand : CommandBase<FsInfoCommand.CommandOptions>
{
    internal record CommandOptions(
        bool Raw, 
        [property: JsonPropertyName("praw")] bool PrettyRaw) : ICommandOptions;

    public FsInfoCommand()
        : base("FsInfo", "Displays information about the currently open File System.")
    {
        this.Options.AddNamed("raw", "Outputs the raw JSON information of the file system.");
        this.Options.AddNamed("praw", "Outputs the raw formatted JSON information of the file system.");
    }

    protected override CommandResults? Execute(ExecuteSettings settings, CommandOptions options)
    {
        if (settings.FileSystem is null)
            return null;

        FileSystem fs = settings.FileSystem;

        using (Stream stream = fs.Open("$fs.info$", FileSystemFileMode.Read))
        {
            byte[] buffer = new byte[stream.Length];
            stream.ReadExactly(buffer);

            string rawInfo = Encoding.UTF8.GetString(buffer);

            if (options.Raw)
            {
                Console.WriteLine(rawInfo);
                return null;
            }

            if (options.PrettyRaw)
            {
                Console.WriteLine(JsonUtils.FormatJson(rawInfo));
                return null;
            }

            JsonElement rootInfo = JsonDocument.Parse(rawInfo).RootElement;

            Console.WriteLine("File System Information:");
            Console.WriteLine($" - Path         : {fs.Path}");

            JsonElement jsonElement;
            if (rootInfo.TryGetProperty("FileSize", out jsonElement))
                Console.WriteLine($" - Size         : {jsonElement.GetInt64():N0}");

            if (rootInfo.TryGetProperty("TotalPages", out jsonElement))
                Console.WriteLine($" - Total Pages  : {jsonElement.GetInt64():N0}");

            if (rootInfo.TryGetProperty("PageSize", out jsonElement))
                Console.WriteLine($" - Page Size    : {jsonElement.GetInt64():N0}");
        }

        return null;
    }
}
