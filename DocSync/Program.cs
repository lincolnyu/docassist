using DocAssistShared.Merging;
using QLogger.ConsoleHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static DocAssistShared.Merging.LogicFilter;
using static DocAssistShared.Merging.MergeSink;

namespace DocSync
{
    class Program
    {
        static CommonOperators? ParseOperator(string opStr)
        {
            switch (opStr.ToLower())
            {
                case "or": return CommonOperators.Or;
                case "and": return CommonOperators.And;
                case "xor": return CommonOperators.Xor;
                default: return null;
            }
        }

        static Tuple<string, string> PromptOnConflict(FileUnit left, FileUnit right, string target)
        {
            Console.Write($"{left.OriginalPath} vs {right.OriginalPath} (L/R/B/N): ");
            var key = Console.ReadKey();
            Console.WriteLine();
            switch (key.KeyChar)
            {
                case 'L':
                    return new Tuple<string, string>(GetTarget(left, target), null);
                case 'R':
                    return new Tuple<string, string>(null, GetTarget(right, target));
                case 'B':
                    return new Tuple<string, string>(GetLTarget(left, target), GetRTarget(right, target));
                case 'N':
                    break;
            }
            return new Tuple<string, string>(null, null);
        }

        static IEnumerable<FileUnit> GetAllFilesAndSubdirectories(string dirStr)
        {
            var dir = new DirectoryInfo(dirStr);
            var items = dir.EnumerateFiles("*", SearchOption.AllDirectories).Select(x => x.FullName).Concat(dir.EnumerateDirectories("*", SearchOption.AllDirectories).Select(x => x.FullName)).OrderBy(x => x).Select(x=> FileUnit.Create(x, dirStr));
            return items;
        }

        static void RunMergeProgram(CommonOperators op, string left, string right, string targetDir, bool prompt)
        {
            if (!Directory.Exists(left) || !Directory.Exists(right) || !Directory.Exists(targetDir))
            {
                Console.WriteLine("At least one of the directories doesn't exist");
                return;
            }
            const DirectorySelectionModes dsm = DirectorySelectionModes.HighDirectory;
            const FileDirSelectionModes fdsm = FileDirSelectionModes.File;
            var sink = prompt ? new MergeSink(PromptOnConflict, dsm, fdsm, targetDir)
                : new MergeSink(ConflictHandlingModes.AlwaysTakeNewer, dsm, fdsm, targetDir);
            var lf = new LogicFilter(op, PresenceLevels.ParentOrFile, PresenceLevels.ParentOrFile,
                sink.Output);
            var lfiles = GetAllFilesAndSubdirectories(left);
            var rfiles = GetAllFilesAndSubdirectories(right);
            var lrfiles = FileMerging.Merge(lfiles, rfiles);
            foreach (var lrfile in lrfiles)
            {
                lf.Process(lrfile.Item1, lrfile.Item2);
            }
        }

        static void PrintUsage(string topic = null)
        {
            switch (topic)
            {
                case "merge":
                    Console.WriteLine("m[erge] --left <left directory> -right <right directory> --op <operator> --out <merge list file> [--prompt]");
                    break;
                default:
                    Console.WriteLine("Choose topic: ");
                    Console.WriteLine("  merge");
                    break;
            }
        }

        static void Main(string[] args)
        {
            var hasHelp = args.Contains("--help");
            if (args.Contains("m") || args.Contains("merge"))
            {
                var left = args.GetSwitchValue("--left");
                var right = args.GetSwitchValue("--right");
                var target = args.GetSwitchValue("--out");
                var opStr = args.GetSwitchValue("--op");
                var op = opStr != null ? ParseOperator(opStr) : null;
                if (hasHelp || left == null || right == null || target == null || op == null)
                {
                    PrintUsage("merge");
                    return;
                }
                var prompt = args.Contains("--prompt");
                RunMergeProgram(op.Value, left, right, target, prompt);
            }
            else
            {
                PrintUsage();
            }
        }
    }
}
