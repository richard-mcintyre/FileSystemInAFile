using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSystemInAFile.App;
internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine($"Is 64bit = {Environment.Is64BitProcess}");

        const ushort pageSize = 8192;
        string path = $"test_{pageSize}.bin";

        if (File.Exists(path))
            File.Delete(path);

        using (FileSystem fs = FileSystem.CreateNew(path, pageSize))
        {
            //CopyFiles(fs, @"F:\bin");
            CopyFiles(fs, @"O:\Cameras\Driveway");

            //fs.CreateDirectory(@"\test\aaa");
            //fs.CreateDirectory(@"\test\bbb\ccc");

            //fs.WriteFile(@"\testfile.txt", new MemoryStream(Encoding.UTF8.GetBytes("hello world")));

            /*fs.WriteFile(@"\testfile.txt", new MemoryStream(new byte[]
            {
                0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
                0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
                0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
                0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
                0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
                0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
                0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
                0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
                0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
                0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
                0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
                0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
                0xff,0xff,0xff, 0xee
            }));*/

            /*
            for (int i = 0; i < 1; i++)
            {
                Stopwatch sw = Stopwatch.StartNew();
                fs.WriteFile($@"\ta{i}.bin", File.OpenRead(@"F:\bin\c3dlZXRsb3ZlXzI0Mw==\20230727j.mpb"));

                Console.WriteLine($"Writed {i + 1} - {sw.ElapsedMilliseconds}ms");
            }*/

            /*
            for (int i = 0; i < 1200; i++)
            {
                Stopwatch sw = Stopwatch.StartNew();
                fs.WriteFile($@"\tb{i}.bin", File.OpenRead(@"F:\bin\_ah\20230503e.dat"));
                Console.WriteLine($"Writed {i + 1} - {sw.ElapsedMilliseconds}ms");
            }*/

            /*foreach (FileSystemEntry entry in fs.GetDirectoryContents(@"\").ToArray())
            {
                Console.WriteLine(entry.Name);
            }*/
        }
    }

    private static void CopyFiles(FileSystem fs, string sourcePath)
    {
        int count = 0;
        Stack<string> stack = new Stack<string>();
        stack.Push(sourcePath);

        while (stack.Count > 0)
        {
            string path = stack.Pop();

            foreach (string file in Directory.GetFiles(path))
            {
                string fsPath = path.Substring(sourcePath.Length);
                if (fsPath.Length == 0)
                    fsPath = @"\";

                fs.CreateDirectory(fsPath);

                count++;
                Console.WriteLine($"[{count}] {file}");
                /*if ((count % 100) == 0)
                    Console.WriteLine(count);*/

                using (FileStream stream = File.OpenRead(file))
                {
                    fs.WriteFile(file.Substring(sourcePath.Length), stream);
                }
            }

            foreach (string dir in Directory.GetDirectories(path))
            {
                stack.Push(Path.Combine(path, dir));
            }
        }
    }
}
