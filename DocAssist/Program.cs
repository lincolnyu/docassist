using QLogger.ConsoleHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DocAssist
{
    class Program
    {
        static void Concat(IEnumerable<FileInfo> files, FileInfo target)
        {
            const int bufferSize = 4096;
            byte[] buffer = new byte[bufferSize];
            using (var ofs = target.OpenWrite())
            {
                foreach (var file in files)
                {
                    using (var ifs = file.OpenRead())
                    {
                        while (true)
                        {
                            var read = ifs.Read(buffer, 0, bufferSize);
                            if (read <= 0) break;
                            ofs.Write(buffer, 0, read);
                        }
                    }
                }
            }
        }

        static void DoConcatProgram(string dirStr, string targetStr)
        {
            var dir = new DirectoryInfo(dirStr);
            var files = dir.GetFiles();
            var target = new FileInfo(targetStr);
            Console.WriteLine($"Concatenating files in '{dirStr}' to '{targetStr}'...");
            Concat(files, target);
            Console.WriteLine($"Concatenating completed.");
        }

        static void PrintUsage(string topic = null)
        {
            switch (topic)
            {
                case "concat":
                    Console.WriteLine("concat [--in <input directory>] --out <output file>");
                    break;
                default:
                    Console.WriteLine("Choose topic: ");
                    Console.WriteLine("  concat");
                    break;
            }
        }

        static string EnsureAbs(string path, string baseDir)
        {
            if (Path.IsPathRooted(path))
            {
                return path;
            }
            return Path.Combine(baseDir, path);
        }

        static void Main(string[] args)
        {
            var workingDir = Directory.GetCurrentDirectory();
            if (args.Contains("c") || args.Contains("concat"))
            {
                var target = args.GetSwitchValue("--out")?? args.GetSwitchValue("-o");
                if (target == null)
                {
                    PrintUsage("concat");
                    return;
                }
                var input = args.GetSwitchValue("--in") ?? args.GetSwitchValue("-i");
                if (input == null)
                {
                    input = workingDir;
                }
                input = EnsureAbs(input, workingDir);
                Console.WriteLine($"old target = {target}");
                target = EnsureAbs(target, workingDir);
                Console.WriteLine($"new target = {target}");
                DoConcatProgram(input, target);
            }
            else
            {
                PrintUsage();
            }
        }
    }
}
