using System;
using System.Collections.Generic;
using System.Text;

namespace FileSystemInAFile.Tests;

public class FileSystemPathTests
{
    [Test]
    public void GetFileName()
    {
        const string path = "/folder/subfolder/file.txt";

        string actual = FileSystemPath.GetFileName(path);
        Assert.That(actual, Is.EqualTo("file.txt"));
    }

    [Test]
    public void GetDirectoryName()
    {
        const string path = "/folder/subfolder/file.txt";

        string actual = FileSystemPath.GetDirectoryName(path);
        Assert.That(actual, Is.EqualTo("/folder/subfolder"));
    }

    [Test]
    public void Combine()
    {
        string actual = FileSystemPath.Combine("folder", "/subfolder/", "file.txt");
        Assert.That(actual, Is.EqualTo("/folder/subfolder/file.txt"));
    }

    [Test]
    public void SplitPath()
    {
        IEnumerable<string> actual = FileSystemPath.SplitPath("/folder/subfolder/file.txt");
        Assert.That(actual, Is.EqualTo(new string[] { "folder", "subfolder", "file.txt" }));
    }

    [Test]
    public void IsPathValid([Values("/folder/subfolder/file.txt")] string value) =>
        Assert.That(FileSystemPath.IsPathValid(value), Is.True);

    [Test]
    public void IsPathAbsolute_True([Values("/folder/subfolder/file.txt", "/file.txt")] string value) =>
        Assert.That(FileSystemPath.IsPathAbsolute(value), Is.True);

    [Test]
    public void IsPathAbsolute_False([Values("folder/subfolder/file.txt", "file.txt")] string value) =>
        Assert.That(FileSystemPath.IsPathAbsolute(value), Is.False);

}
