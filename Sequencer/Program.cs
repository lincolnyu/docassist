using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Sequencer
{
    class Program
    {
        static int ParsePattern(string pattern, int minDigits, out string newPattern)
        {
            var result = 0;
            var newPatternSb = new StringBuilder();
            // looking for '{...}'
            for (var i = 0; i < pattern.Length - 1; i++)
            {
                var c = pattern[i];
                if (c == '{')
                {
                    if (pattern[i + 1] == '{')
                    {
                        i++;
                        newPatternSb.Append("{{");
                        continue;
                    }
                    result++;
                    int j;
                    for (j = i; j < pattern.Length; j++)
                    {
                        c = pattern[j];
                        if (c == '}')
                        {
                            break;
                        }
                    }
                    var directive = pattern.Substring(i+1, j-i-1);    // get substring [i+1,j)
                    var posComma = directive.IndexOf(':');
                    if (posComma >= 0)
                    {
                        var numFmt = directive.Substring(posComma + 1);
                        if (numFmt.ToUpper() == "A") // auto
                        {
                            newPatternSb.Append("{0:");
                            newPatternSb.Append('0', minDigits);
                            newPatternSb.Append('}');
                        }
                    }
                    else
                    {
                        if (j >= pattern.Length) j = pattern.Length - 1;
                        newPatternSb.Append(pattern, i, j + 1 - i);   // substring [i, j]
                    }
                    i = j + 1;
                }
                else
                {
                    newPatternSb.Append(c);
                }
            }
            newPattern = newPatternSb.ToString();
            return result;
        }

        static void ArrangeByDate(DirectoryInfo dir, string pattern, bool extSpecd = false)
        {
            var files = dir.GetFiles().OrderBy(f => f.CreationTimeUtc).ToList();
            var count = files.Count;
            var minDigits = (int)Math.Floor(Math.Log10(count)) + 1;

            var appfile = Assembly.GetExecutingAssembly().Location.ToLower();

            string workingPattern;
            var numArgs = ParsePattern(pattern, minDigits, out workingPattern);

            var counter = 1;    // TODO allow other base value for counter
            var newNames = new HashSet<string>();
            var newNameList = new List<string>();
            for (var i = 0; i < files.Count; i++)
            {
                var file = files[i];
                
                if (file.FullName.ToLower() == appfile || file.IsReadOnly ||
                    (file.Attributes & FileAttributes.System) != 0 ||
                    (file.Attributes & FileAttributes.Hidden) != 0 ||
                    (file.Attributes & FileAttributes.Temporary) != 0)  // TODO any type else?
                {
                    files.RemoveAt(i);
                    i--;
                    continue;
                }

                var pars = new object[numArgs];
                for (var j = 0; j < numArgs; j++) pars[j] = counter;
                var newName = string.Format(workingPattern, pars).ToLower();

                if (!extSpecd)
                {
                    newName += file.Extension;
                }

                newNames.Add(newName);
                newNameList.Add(newName);
                counter++;
            }

            for (var i = 0; i < files.Count; i++)
            {
                var file = files[i];

                if (!newNames.Contains(file.Name.ToLower())) continue;

                var tempName = file.Name + ".tmp";
                var tempPath = Path.Combine(dir.FullName, tempName);
                file.MoveTo(tempPath);
                var tempFile = new FileInfo(tempPath);
                files[i] = tempFile;
            }

            var iName = 0;
            foreach (var file in files)
            {
                if (file.FullName.ToLower() == appfile) continue;
                var newName = newNameList[iName++];
                var newPath = Path.Combine(dir.FullName, newName);
                file.MoveTo(newPath);
            }
        }

        static void ShowHelp()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            Console.WriteLine("Sequencer version {0}, by quanbenSoft 2013", version);
            Console.WriteLine("Usage: sequencer --help");
            Console.WriteLine("           for this help");
            Console.WriteLine("       sequencer -d <directory> -p <pattern> [-e]");
            Console.WriteLine("           renames the files in the directory so they in order of date");
            Console.WriteLine("           <pattern> C-sharp string pattern");
            Console.WriteLine("                     {<index>:A} for the app to figure out minimum");
            Console.WriteLine("                     digit numbers needed for decimal");
            Console.WriteLine("           -e        if this exists it means ext already specified by the pattern");
        }

        static void Main(string[] args)
        {
            try
            {
                var nextArgType = 0;
                var asmLoc = Assembly.GetExecutingAssembly().Location;
                var asmDir = Path.GetDirectoryName(asmLoc);
                System.Diagnostics.Trace.Assert(asmDir != null);
                var dirPath = asmDir;   // default directory is the working directory of the app
                var pattern = "{0:A}";  // default pattern
                var extSpecd = false;   // by default extension is not included in the pattern
                foreach (var arg in args)
                {
                    if (arg == "-d")
                    {
                        nextArgType = 1;
                    }
                    else if (arg == "-p")
                    {
                        nextArgType = 2;
                    }
                    else if (arg == "--help")
                    {
                        ShowHelp();
                        return;
                    }
                    else if (arg == "-e")
                    {
                        extSpecd = true;
                    }
                    else if (nextArgType == 1)
                    {
                        dirPath = arg;
                        if (!Path.IsPathRooted(dirPath))
                        {
                            dirPath = Path.Combine(asmDir, dirPath);
                        }
                    }
                    else if (nextArgType == 2)
                    {
                        pattern = arg;
                    }
                }
                var dir = new DirectoryInfo(dirPath);
                Console.Write("Arranging files in '{0}' ... ", dirPath);
                ArrangeByDate(dir, pattern, extSpecd);
                Console.WriteLine("done.");
            }
            catch (Exception e)
            {
                Console.WriteLine("Some unexpected error occurred, details of the error being,");
                Console.WriteLine(e.Message);
                Console.WriteLine("Please check the usage as follows,");
                ShowHelp();
            }
        }
    }
}
