using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace FileSystemInAFile;

public static class FileSystemPath
{
    public const char DirectorySeparatorChar = '/';

    public static string GetFileName(string path)
    {
        if (String.IsNullOrEmpty(path))
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));

        int lastSeparatorIndex = path.LastIndexOf(DirectorySeparatorChar);
        if (lastSeparatorIndex < 0)
            return path;

        return path.Substring(lastSeparatorIndex + 1);
    }

    public static string GetDirectoryName(string path)
    {
        if (String.IsNullOrEmpty(path))
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));

        int lastSeparatorIndex = path.LastIndexOf(DirectorySeparatorChar);
        if(lastSeparatorIndex == 0)
            return DirectorySeparatorChar.ToString();

        if (lastSeparatorIndex < 0)
            return path;

        return path.Substring(0, lastSeparatorIndex);
    }

    public static string Combine(params string[] paths)
    {
        if (paths == null || paths.Length == 0)
            throw new ArgumentException("Paths cannot be null or empty.", nameof(paths));

        StringBuilder sb = new StringBuilder();
        sb.Append(DirectorySeparatorChar);

        foreach (string path in paths)
        {
            if (String.IsNullOrEmpty(path))
                continue;

            if (!sb.ToString().EndsWith(DirectorySeparatorChar))
            {
                sb.Append(DirectorySeparatorChar);
            }

            sb.Append(path.Trim(DirectorySeparatorChar));
        }

        return sb.ToString();
    }

    public static IEnumerable<string> SplitPath(string path)
    {
        if (String.IsNullOrEmpty(path))
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));

        return path.Split(DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
    }

    public static bool IsPathValid(string path)
    {
        if (String.IsNullOrWhiteSpace(path))
            return false;

        foreach (string segment in SplitPath(path))
        {
            if (!PageDirectory.IsEntryNameValid(segment))
                return false;
        }

        return true;
    }

    public static bool IsPathAbsolute(string path) =>
        !String.IsNullOrEmpty(path) && path.StartsWith(DirectorySeparatorChar);

    public static bool DoesFileNameMatchWildcard(string fileName, string pattern)
    {
        string regexPattern = "^" + Regex.Escape(GetFileName(pattern))
            .Replace(@"\*", ".*")
            .Replace(@"\?", ".") + "$";

        return Regex.IsMatch(fileName, regexPattern, RegexOptions.IgnoreCase);
    }
}
