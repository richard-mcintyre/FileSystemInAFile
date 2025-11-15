using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using FileSystemInAFile;
using Fs.Cli.Common;

namespace FsRepl;

internal class Program
{
    #region Construction

    static Program() =>
        RegisterCommands();

    public Program()
    {
    }

    #endregion

    #region Fields

    private static readonly Dictionary<string, ICommand> _commands = new Dictionary<string, ICommand>(StringComparer.InvariantCultureIgnoreCase);

    private FileSystem? _fs;

    #endregion

    #region Properties

    public static IEnumerable<ICommand> Commands => _commands.Values;

    #endregion

    #region Methods

    public void Run(FileSystem? initialFileSystem)
    {
        _fs = initialFileSystem;

        Console.WriteLine("FsRepl - File System In A File REPL");
        Console.WriteLine("Type 'help' to see available commands.");
        Console.WriteLine();

        CommandResults? commandResults = null;
        while (true)
        {
            if (commandResults?.LaunchDebugger == true)
                System.Diagnostics.Debugger.Launch();

            Console.Write(GetPrompt());
            string? input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
                continue;

            string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            string commandName = parts[0];
            string[] commandArgs = parts.Length > 1 ? parts[1..] : Array.Empty<string>();

            if (_commands.TryGetValue(commandName, out ICommand? command))
            {
                try
                {
                    commandResults = command.Execute(new ExecuteSettings()
                    {
                        FileSystem = _fs,
                    }, commandArgs);

                    if (commandResults is not null)
                    {
                        ProcessCommandResults(commandResults);
                        if (commandResults.Exit)
                        {
                            _fs?.Dispose();
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ex.GetType()}: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"Unknown command: {commandName}");
            }

            Console.WriteLine();
        }
    }

    private void ProcessCommandResults(CommandResults results)
    {
        if (results.CloseFileSystem || results.UseFileSystem is not null)
        {
            _fs?.Dispose();
            _fs = null;
        }

        if (results.UseFileSystem is not null)
        {
            _fs = results.UseFileSystem;
        }


    }

    private string GetPrompt()
    {
        StringBuilder prompt = new StringBuilder();
        if (_fs is not null)
        {
            prompt.Append($"[{Path.GetFileName(_fs.Path)}]");
        }
        prompt.Append("> ");
        return prompt.ToString();
    }

    private static void RegisterCommands()
    {
        foreach(Type type in typeof(Program).Assembly.GetTypes())
        {
            if (typeof(ICommand).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
            {
                ICommand cmdInstance = (ICommand)Activator.CreateInstance(type)!;
                _commands.Add(cmdInstance.Name, cmdInstance);
            }
        }
    }

    #endregion
}
