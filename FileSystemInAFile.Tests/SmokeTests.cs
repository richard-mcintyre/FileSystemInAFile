using System;
using System.Text;

namespace FileSystemInAFile.Tests;

public class SmokeTests
{
    private const string TestFileName = "randomBytes.bin";

    [SetUp]
    public void OnSetup()
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, TestFileName);
        if (File.Exists(path))
            File.Delete(path);
    }

    [Test]
    public void ReadWriteTests([Values(0, 4000, 8191, 8192, 10000)] int length)
    {
        byte[] fileData = new byte[length];
        Random random = new Random();
        random.NextBytes(fileData);

        using (FileSystem fs = FileSystem.CreateNew(Path.Combine(TestContext.CurrentContext.WorkDirectory, TestFileName)))
        {
            using (Stream stream = fs.Open("/testfile.txt", FileSystemFileMode.CreateNew | FileSystemFileMode.ReadWrite))
            {
                stream.Write(fileData, 0, fileData.Length);
            }
        }

        byte[] readFileData = new byte[length];
        using (FileSystem fs = FileSystem.OpenExisting(Path.Combine(TestContext.CurrentContext.WorkDirectory, TestFileName)))
        {
            using (Stream stream = fs.Open("/testfile.txt", FileSystemFileMode.Read))
            {
                stream.ReadExactly(readFileData, 0, readFileData.Length);
            }
        }

        Assert.That(readFileData, Is.EquivalentTo(fileData));
    }

    [Test]
    public void DeleteFile()
    {
        using (FileSystem fs = FileSystem.CreateNew(Path.Combine(TestContext.CurrentContext.WorkDirectory, TestFileName)))
        {
            for (int i = 1; i <= 3; i++)
            {
                using (Stream stream = fs.Open($"/testfile_{i}.txt", FileSystemFileMode.CreateNew | FileSystemFileMode.ReadWrite))
                {
                    stream.Write(Encoding.UTF8.GetBytes("Hello, World!"));
                }
            }
        }

        using (FileSystem fs = FileSystem.OpenExisting(Path.Combine(TestContext.CurrentContext.WorkDirectory, TestFileName)))
        {
            fs.Delete("/testfile_2.txt");
        }

        using (FileSystem fs = FileSystem.OpenExisting(Path.Combine(TestContext.CurrentContext.WorkDirectory, TestFileName)))
        {
            IEnumerable<string> entries = fs.GetDirectoryContents("/").Select(e => e.Name).ToArray();
            Assert.That(entries, Is.EquivalentTo(new[] { "testfile_1.txt", "testfile_3.txt" }));
        }
    }

    [Test]
    public void DeleteDirectory()
    {
        using (FileSystem fs = FileSystem.CreateNew(Path.Combine(TestContext.CurrentContext.WorkDirectory, TestFileName)))
        {
            for (int i = 1; i <= 3; i++)
            {
                fs.CreateDirectory($"/subdir_{i}");
            }
        }

        using (FileSystem fs = FileSystem.OpenExisting(Path.Combine(TestContext.CurrentContext.WorkDirectory, TestFileName)))
        {
            fs.DeleteDirectory("/subdir_2");
        }

        using (FileSystem fs = FileSystem.OpenExisting(Path.Combine(TestContext.CurrentContext.WorkDirectory, TestFileName)))
        {
            IEnumerable<string> entries = fs.GetDirectoryContents("/").Select(e => e.Name).ToArray();
            Assert.That(entries, Is.EquivalentTo(new[] { "subdir_1", "subdir_3" }));
        }
    }

}
