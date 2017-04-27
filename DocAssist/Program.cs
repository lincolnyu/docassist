using QLogger.ConsoleHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace DocAssist
{
    class Program
    {
        private delegate void VoidDelegate();

        static int GetBufferSize() => 4096;

        static void Concat(IEnumerable<FileInfo> files, FileInfo target)
        {
            var bufferSize = GetBufferSize();
            var buffer = new byte[bufferSize];
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

        static long Slice(FileInfo file, long start, long len, FileInfo target)
        {
            var bufferSize = GetBufferSize();
            var buffer = new byte[bufferSize];
            using (var ofs = target.OpenWrite())
            using (var ifs = file.OpenRead())
            {
                var actual = ifs.Seek(start, SeekOrigin.Begin);
                if (actual != start)
                {
                    return 0;
                }
                var totalRead = 0L;
                for (var left = len; left > 0; )
                {
                    var toRead = bufferSize < left ? bufferSize : (int)left;
                    var read = ifs.Read(buffer, 0, toRead);
                    if (read <= 0) break;
                    ofs.Write(buffer, 0, read);
                    totalRead += read;
                }
                return totalRead;
            }
        }

        static void RunConcatProgram(string dirStr, string targetStr)
        {
            var dir = new DirectoryInfo(dirStr);
            var files = dir.GetFiles();
            var target = new FileInfo(targetStr);
            Console.WriteLine($"Concatenating files in '{dirStr}' to '{targetStr}'...");
            Concat(files, target);
            Console.WriteLine($"Concatenating completed.");
        }

        static void RunSliceProgram(string ifStr, long? start, long? len, string ofStr)
        {
            var input = new FileInfo(ifStr);
            var output = new FileInfo(ofStr);
            if (start == null) start = 0;
            if (len == null) len = input.Length - start.Value;
            Slice(input, start.Value, len.Value, output);
        }

        private static void RunAwaitedCopyProgram(string source, string target, bool move, bool force, bool verbose)
        {
            var handle = new VoidDelegate(() =>
            {
                if (verbose)
                {
                    Console.WriteLine($"Source '{source}' appears");
                }
                if (Directory.Exists(target))
                {
                    target = Path.Combine(target, Path.GetFileName(source));
                }
                if (File.Exists(target))
                {
                    if (force)
                    {
                        File.Delete(target);
                    }
                    else
                    {
                        Console.WriteLine($"Target file '{target}' already exists");
                        return;
                    }
                }
                if (verbose)
                {
                    Console.Write((move ? "Moving" : "Copying") + $" file '{source}' to '{target}' ...");
                }
                if (move)
                {
                    File.Move(source, target);
                }
                else
                {
                    File.Copy(source, target);
                }
                Console.WriteLine(" Done");
            });
            if (File.Exists(source))
            {
                handle();
            }
            else
            {
                var watcher = new FileSystemWatcher()
                {
                    Path = Path.GetDirectoryName(source),
                    NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime
                };
                var fileEvent = new ManualResetEvent(false);

                FileSystemEventHandler fseh = null;
                RenamedEventHandler reh = null;
                var unsubscribe = new VoidDelegate(() =>
                {
                    watcher.Created -= fseh;
                    watcher.Changed -= fseh;
                    watcher.Renamed -= reh;
                });
                fseh = (s, e) =>
                {
                    if (e.FullPath.ToLower() == source.ToLower()) // TODO windows only
                    {
                        unsubscribe();
                        handle();
                        fileEvent.Set();
                    }
                };
                reh = (s, e) =>
                {
                    if (e.FullPath.ToLower() == source.ToLower()) // TODO windows only
                    {
                        unsubscribe();
                        handle();
                        fileEvent.Set();
                    }
                };
              
                watcher.Created += fseh;
                watcher.Changed += fseh;
                watcher.Renamed += reh; 
                if (verbose)
                {
                    Console.WriteLine($"Waiting for '{source}' ...");
                }
                watcher.EnableRaisingEvents = true;
                fileEvent.WaitOne();
            }
        }

        static void PrintUsage(string topic = null)
        {
            switch (topic)
            {
                case "awaitedcopy":
                    Console.WriteLine("[a]w[aited]c[opy] [--in <source file>] --out <target file> [--move] [--force] [--verbose]");
                    break;
                case "concat":
                    Console.WriteLine("c[oncat] [--in <input directory>] --out <output file>");
                    break;
                case "slice":
                    Console.WriteLine("s[lice] --in <input file> --out <output file> [--start <start>] [--len <length>]");
                    break;
                default:
                    Console.WriteLine("Choose topic: ");
                    Console.WriteLine("  [a]w[aited]c[opy]");
                    Console.WriteLine("  c[oncat]");
                    Console.WriteLine("  s[lice]");
                    break;
            }
        }

        static string EnsureAbs(string path, string baseDir)
            => Path.IsPathRooted(path)? path : Path.Combine(baseDir, path);

        static void Main(string[] args)
        {
            var workingDir = Directory.GetCurrentDirectory();
            var hasHelp = args.Contains("--help");
            if (args.Contains("wc") || args.Contains("awaitedcopy"))
            {
                var source = args.GetSwitchValue("--in") ?? args.GetSwitchValue("-i");
                var target = args.GetSwitchValue("--out") ?? args.GetSwitchValue("-o");
                var move = args.Contains("--move");
                var force = args.Contains("--force");
                var verbose = args.Contains("--verbose");
                if (source == null || target == null)
                {
                    PrintUsage("awaitedcopy");
                    return;
                }
                source = EnsureAbs(source, workingDir);
                target = EnsureAbs(target, workingDir);
                RunAwaitedCopyProgram(source, target, move, force, verbose);
            }
            else if (args.Contains("c") || args.Contains("concat"))
            {
                var target = args.GetSwitchValue("--out")?? args.GetSwitchValue("-o");
                if (hasHelp || target == null)
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
                target = EnsureAbs(target, workingDir);
                RunConcatProgram(input, target);
            }
            else if (args.Contains("s") || args.Contains("slice"))
            {
                var input = args.GetSwitchValue("--in") ?? args.GetSwitchValue("-i");
                var target = args.GetSwitchValue("--out") ?? args.GetSwitchValue("-o");
                var start = args.GetSwitchValueAsLongOpt("--start");
                var len = args.GetSwitchValueAsLongOpt("--len");
                if (hasHelp || input == null || target == null)
                {
                    PrintUsage("slice");
                    return;
                }
                RunSliceProgram(input, start, len, target);
            }            
            else
            {
                PrintUsage();
            }
        }
    }
}
