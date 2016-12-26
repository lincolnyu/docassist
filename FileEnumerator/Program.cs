using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using DocAssistRuntime;
using DocAssistShared;
using DocAssistShared.Helpers;

namespace FileEnumerator
{
    /// <summary>
    ///  The program for choosing files from a specified path and its subdirectories
    /// </summary>
    internal class Program
    {
        #region Methods

        private static void GetFiltersFromCode(string code, string[] referencedAssemblies,
                                               out FileSysItemPredicate<DirectoryInfo> dirFilter,
                                               out FileSysItemPredicate<DirectoryInfo> dirSelector,
                                               out FileSysItemPredicate<FileInfo> fileSelector)
        {
            dirFilter = null;
            dirSelector = null;
            fileSelector = null;

            var assembly = code.Compile("CSharp", referencedAssemblies);
            const string fullClassName = "SelectScript.Program";
            var programType = assembly.GetType(fullClassName);
            if (programType == null)
            {
                return;
            }
            var dirFilterMethod = programType.GetMethod("DirectoryFilter");
            if (dirFilterMethod != null)
            {
                dirFilter = (FileSysItemPredicate<DirectoryInfo>)dirFilterMethod.CreateDelegate(typeof(FileSysItemPredicate<DirectoryInfo>));
            }
            
            var dirSelectMethod = programType.GetMethod("DirectorySelector");
            if (dirSelectMethod != null)
            {
                dirSelector = (FileSysItemPredicate<DirectoryInfo>)dirSelectMethod.CreateDelegate(typeof(FileSysItemPredicate<DirectoryInfo>));
            }
            
            var fileSelectMethod = programType.GetMethod("FileSelector");
            if (fileSelectMethod != null)
            {
                fileSelector = (FileSysItemPredicate<FileInfo>)fileSelectMethod.CreateDelegate(typeof(FileSysItemPredicate<FileInfo>));
            }
        }

        /// <summary>
        ///  Display help information about the program
        /// </summary>
        private static void ShowHelp()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            Console.WriteLine("FileEnumerator version {0}, by quanbenSoft 2013", version);
            Console.WriteLine("Usage: FileEnumerator --help");
            Console.WriteLine("           for this help");
            Console.WriteLine("       FileEnumerator -t <template-script>");
            Console.WriteLine("       FileEnumerator -i <start-directories> [-a <assemblies>] [-f <filter-script>] [-o [<output-list>]]");
            Console.WriteLine("           renames the specified files as per the customisable renamer");
            Console.WriteLine("           <start-directories> Text file that contains a list of start directories one line each");
            Console.WriteLine("           <filter-script> C# file that defines filters and seletors; see samples");
            Console.WriteLine("           <assemblies> text file containing searchable path to referenced assemblies");
            Console.WriteLine("           <output-file> A report of selected files and directories");
        }

        private static void GenerateTemplateScript(string fileName)
        {
            var code = new[]
            {
                "using System.IO;",
                "",
                "namespace SelectScript",
                "{",
                "	class Program",
                "	{",
                "		// uncomment the following code if the control of stepping into subdirectory is needed",
                "		/*",
                "		public static bool DirectoryFilter(DirectoryInfo dir)",
                "		{",
                "			// change the logic if a different policy that specifies whether or not entering a ",
                "			// specified subdirectory",
                "			return true;",
                "		}",
                "		*/",
                "",
                "		// uncomment the following code if directory selection policy is to be defined",
                "		/*",
                "		public static bool DirectorySelector(DirectoryInfo dir)",
                "		{",
                "			// change the logic if a different directory selection policy is to be used",
                "			return true;",
                "		}",
                "		*/",
                "	",
                "		public static bool FileSelector(FileInfo file)",
                "		{",
                "			// change the logic if a different file selection policy is to be used",
                "			return true;",
                "		}",
                "	}",
                "}",
                ""
            };

            using (var sw = new StreamWriter(fileName))
            {
                foreach (var line in code)
                {
                    sw.WriteLine(line);
                }
            }
        }

        /// <summary>
        ///  Main entry of the renaming program
        /// </summary>
        /// <param name="args">Arguments to the program; see ShowHelp() for more detail</param>
        private static void Main(string[] args)
        {
            try
            {
                var currDir = Directory.GetCurrentDirectory();
                System.Diagnostics.Trace.Assert(currDir != null);
                string inputFile = null;
                string codeFile = null;
                string assembliesFile = null;
                var outputfile = "filelist.txt";
                var nextArgType = 0;
                foreach (var arg in args)
                {
                    if (arg == "-i")
                    {
                        nextArgType = 1;
                    }
                    else if (arg == "-f")
                    {
                        nextArgType = 2;
                    }
                    else if (arg == "-a")
                    {
                        nextArgType = 3;
                    }
                    else if (arg == "-o")
                    {
                        nextArgType = 4;
                    }
                    else if (arg == "-t")
                    {
                        nextArgType = 5;
                    }
                    else if (arg == "--help")
                    {
                        ShowHelp();
                        return;
                    }
                    else if (nextArgType == 1)
                    {
                        inputFile = Path.IsPathRooted(arg) ? arg : Path.Combine(currDir, arg);
                    }
                    else if (nextArgType == 2)
                    {
                        codeFile = Path.IsPathRooted(arg) ? arg : Path.Combine(currDir, arg);
                    }
                    else if (nextArgType == 3)
                    {
                        assembliesFile = Path.IsPathRooted(arg) ? arg : Path.Combine(currDir, arg);
                    }
                    else if (nextArgType == 4)
                    {
                        outputfile = Path.IsPathRooted(arg) ? arg : Path.Combine(currDir, arg);
                    }
                    else if (nextArgType == 5)
                    {
                        var templateFile = Path.IsPathRooted(arg) ? arg : Path.Combine(currDir, arg);
                        GenerateTemplateScript(templateFile);
                        return;
                    }
                }

                if (inputFile == null)
                {
                    Console.WriteLine("Essential input parameters missing. Please check usage as follows,");
                    ShowHelp();
                    return;
                }

                var fileOrDirs = new List<FileSystemInfo>();
                using (var srInput = new StreamReader(inputFile))
                {
                    var dirInputFile = Path.GetDirectoryName(inputFile);
                    System.Diagnostics.Trace.Assert(dirInputFile != null);
                    string line;
                    while (!srInput.EndOfStream && (line = srInput.ReadLine()) != null)
                    {
                        var fileName = Path.IsPathRooted(line) ? line : Path.Combine(dirInputFile, line);
                        FileSystemInfo fsi;
                        switch (fileName.GetPathFileSystemType())
                        {
                            case FileSystemHelper.FileSystemObjectTypes.Directory:
                                fsi = new DirectoryInfo(fileName);
                                break;
                            case FileSystemHelper.FileSystemObjectTypes.File:
                                fsi = new FileInfo(fileName);
                                break;
                            default:
                                continue;   // TODO warning message
                        }

                        fileOrDirs.Add(fsi);
                    }
                }

                // obtain filters and selectors from code
                var referencedAssemblies = assembliesFile.GetReferencedAssemblyList();
                FileSysItemPredicate<DirectoryInfo> dirFilter = null, dirSelector = null;
                FileSysItemPredicate<FileInfo> fileSelector = null;
                if (codeFile != null)
                {
                    using (var srCode = new StreamReader(codeFile))
                    {
                        var code = srCode.ReadToEnd();
                        GetFiltersFromCode(code, referencedAssemblies, out dirFilter, out dirSelector, out fileSelector);
                    }
                }

                var selected = new List<FileSystemInfo>();

                foreach (var fileOrDir in fileOrDirs)
                {
                    var directoryInfo = fileOrDir as DirectoryInfo;
                    if (directoryInfo != null)
                    {

                        // recursively selects files and directories from the current folder
                        directoryInfo.SelectFilesPostOrder(dirFilter,
// ReSharper disable ImplicitlyCapturedClosure
                                                           (f, dummy) =>
// ReSharper restore ImplicitlyCapturedClosure
                                                               {
                                                                   if (fileSelector == null || fileSelector(f))
                                                                       selected.Add(f);
                                                               },
// ReSharper disable ImplicitlyCapturedClosure
                                                           (d, dummy) =>
// ReSharper restore ImplicitlyCapturedClosure
                                                               {
                                                                   if (dirSelector != null && dirSelector(d))
                                                                       selected.Add(d);
                                                               });
                    }
                    else
                    {
                        // just determines if the file needs to be selected
                        var f = fileOrDir as FileInfo;
                        if (f != null && (fileSelector == null || fileSelector(f)))
                        {
                            selected.Add(f);
                        }
                    }
                }

                using (var swOut = new StreamWriter(outputfile))
                {
                    foreach (var f in selected)
                    {
                        var name = f.FullName;
                        swOut.WriteLine(name);
                    }
                }
            }
            catch (Exception e)
            {
                if (e.Message == CompilationHelper.CompilationFailure)
                {
                    var errors = (CompilerErrorCollection)e.Data[CompilationHelper.DataKeyErrors];
                    var output = (StringCollection)e.Data[CompilationHelper.DataKeyOutput];
                    Console.WriteLine("Compilation failure. Please check your code for the following errors");
                    Console.WriteLine("Output: ");
                    foreach (var outputLine in output)
                    {
                        Console.WriteLine(outputLine);
                    }
                    Console.WriteLine("Errors: ");
                    foreach (var error in errors)
                    {
                        var errorStr = error.ToString();
                        Console.WriteLine(errorStr);
                    }
                }
                else
                {
                    Console.WriteLine("Some unexpected error occurred, details of the error being,");
                    Console.WriteLine(e.Message);
                    Console.WriteLine("Please check the usage as follows,");
                    ShowHelp();
                }
            }
        }

        #endregion
    }
}
