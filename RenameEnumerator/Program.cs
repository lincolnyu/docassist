using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using DocAssistRuntime;
using DocAssistShared;
using DocAssistShared.Helpers;

namespace RenameEnumerator
{
    /// <summary>
    ///  The program for renaming multiple files
    /// </summary>
    internal class Program
    {
        #region Methods

        /// <summary>
        ///  Writes a report of how files are to be renamed to the specified report file
        /// </summary>
        /// <param name="files">The files to rename</param>
        /// <param name="newNames">The new names for the files</param>
        /// <param name="reportFile">The report file to write the report to</param>
        /// <param name="append">Whether to append to an existing report</param>
        private static void GenerateReport(IEnumerable<FileSystemInfo> files, IEnumerable<string> newNames,
                                           string reportFile, bool append = false)
        {
            using (var rw = new StreamWriter(reportFile, append))
            {
                var enumFiles = files.GetEnumerator();
                var enumNames = newNames.GetEnumerator();

                enumFiles.Reset();
                enumNames.Reset();

                while (enumFiles.MoveNext() && enumNames.MoveNext())
                {
                    var oldFile = enumFiles.Current;
                    var oldFileName = oldFile.FullName;
                    var newFileName = enumNames.Current;

                    var oldDistinctName = oldFileName.GetDistinctPath();
                    var newDistinctName = newFileName.GetDistinctPath();

                    var line = string.Format("\"{0}\" -> \"{1}\"", oldDistinctName, newDistinctName);
                    rw.WriteLine(line);
                }
            }
        }

        /// <summary>
        ///  Default renaming method for the specified list of files, which keeps the files unchanged
        /// </summary>
        /// <param name="files">The list of files</param>
        /// <returns>A l</returns>
        private static IList<string> DefaultRenamer(IEnumerable<FileSystemInfo> files)
        {
            return files.Select(f => f.Name).ToList();
        }

        /// <summary>
        ///  Generates rename delegates from the specified code defined in
        ///   namespace RenameScript { class Program { } } with a name 'Rename' and
        ///   signature defined as 'RenameAction'
        /// </summary>
        /// <param name="code">The code that contains the delegate</param>
        /// <param name="referencedAssemblies">The assembly dependencies</param>
        /// <param name="renameFiles">delegate that renames files</param>
        /// <param name="renameDirs">delegates that rename directories</param>
        private static void GetRenamersFromCode(string code, string[] referencedAssemblies,
            out RenameAction<FileInfo> renameFiles, out RenameAction<DirectoryInfo> renameDirs)
        {
            var assembly = code.Compile("CSharp", referencedAssemblies);
            const string fullClassName = "RenameScript.Program";
            var programType = assembly.GetType(fullClassName);

            renameFiles = null;
            renameDirs = null;

            var renameFileMethod = programType.GetMethod("RenameFiles");
            if (renameFileMethod != null)
            {
                renameFiles = (RenameAction<FileInfo>)renameFileMethod.CreateDelegate(typeof(RenameAction<FileInfo>));
            }

            var renameDirectoryMethod = programType.GetMethod("RenameDirectories");
            if (renameDirectoryMethod != null)
            {
                renameDirs = (RenameAction<DirectoryInfo>)renameDirectoryMethod.CreateDelegate(typeof(RenameAction<DirectoryInfo>));
            }

            if (renameFiles != null && renameDirs != null) return;

            var renameMethod = programType.GetMethod("Rename");
            if (renameMethod != null)
            {
                var rename =
                (RenameAction<FileSystemInfo>)renameMethod.CreateDelegate(typeof(RenameAction<FileSystemInfo>));
                if (renameFiles == null) renameFiles = rename;
                if (renameDirs == null) renameDirs = rename;
            }

            if (renameFiles == null) renameFiles = DefaultRenamer;
            if (renameDirs == null) renameDirs = DefaultRenamer;
        }

        /// <summary>
        ///  Display help information about the program
        /// </summary>
        private static void ShowHelp()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            Console.WriteLine("RenameEnumerator version {0}, by quanbenSoft 2013", version);
            Console.WriteLine("Usage: RenameEnumerator --help");
            Console.WriteLine("           for this help");
            Console.WriteLine("       RenameEnumerator -s <template-file>");
            Console.WriteLine("           generates a template script file");
            Console.WriteLine("       RenameEnumerator -i <input-list> -r <renamer> [-a <assemblies>] [-c [<confirm-file>]]");
            Console.WriteLine("           [-o <output-list>] [-k]");
            Console.WriteLine("           renames the specified files as per the customizable renamer");
            Console.WriteLine("           <input-list> text file containing list of files to rename, one line each");
            Console.WriteLine("           <renamer> C# file that defines how to rename the files; see samples");
            Console.WriteLine("           <assemblies> text file containing searchable path to referenced assemblies");
            Console.WriteLine("           <confirm-file> A report of files to rename and how for the user to confirm");
            Console.WriteLine("           <output-file> A list of renamed files to be fed to the follow-up tools");
            Console.WriteLine("           -k Copy instead of renaming");
        }

        private static void GenerateTemplateScript(string fileName)
        {
            var code = new[]
            {
                "using System;",
                "using System.Collections.Generic;",
                "using System.IO;",
                "using System.Linq;",
                "using System.Text;",
                "",
                "namespace RenameScript",
                "{",
                "	class Program",
                "	{",
                "		public static IList<string> Rename(IEnumerable<FileSystemInfo> files)",
                "		{",
                "			var newNames = new List<string>();",
                "			// NOTE every file system info item needs to be attended to",
                "			foreach (var f in files)",
                "            {",
                "				var file = f as FileInfo;",
                "				if (file == null)",
                "                {    // non-files (directories)",
                "                    // change the folowing if you would like to have a different directory renaming plan",
                "                    newNames.Add(f.FullName);    // name remains the same",
                "                    continue;",
                "                }",
                "                ",
                "                var newName = file.Name;",
                "                // change the folowing if you would like to have a different file renaming plan",
                "                newNames.Add(newName);    // name remains the same",
                "            }",
                "            return newNames;",
                "        }",
                "    }",
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
                Trace.Assert(currDir != null);
                var nextArgType = 0;
                string inputFile = null;
                string codeFile = null;
                string assembliesFile = null;
                var toConfirm = false;
                var confirmationFile = "renamelist.txt";
                string outputFile = null;
                var copy = false;

                foreach (var arg in args)
                {
                    if (arg == "-i")
                    {
                        nextArgType = 1;
                    }
                    else if (arg == "-r")
                    {
                        nextArgType = 2;
                    }
                    else if (arg == "-a")
                    {
                        nextArgType = 3;
                    }
                    else if (arg == "-c")
                    {
                        toConfirm = true;
                        nextArgType = 4;
                    }
                    else if (arg == "-o")
                    {
                        nextArgType = 5;
                    }
                    else if (arg == "-t")
                    {
                        nextArgType = 6;
                    }
                    else if (arg == "-k")
                    {
                        copy = true;
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
                        confirmationFile = Path.IsPathRooted(arg) ? arg : Path.Combine(currDir, arg);
                    }
                    else if (nextArgType == 5)
                    {
                        outputFile = Path.IsPathRooted(arg) ? arg : Path.Combine(currDir, arg);
                    }
                    else if (nextArgType == 6)
                    {
                        var templateFile = Path.IsPathRooted(arg) ? arg : Path.Combine(currDir, arg);
                        GenerateTemplateScript(templateFile);
                        return;
                    }
                }
                if (inputFile == null || codeFile == null)
                {
                    Console.WriteLine("Essential input parameters missing. Please check usage as follows,");
                    ShowHelp();
                    return;
                }

                var filesToRename = new List<FileInfo>();
                var dirsToRename = new List<DirectoryInfo>();
                using (var sr = new StreamReader(inputFile))
                {
                    var dirInputFile = Path.GetDirectoryName(inputFile);
                    Trace.Assert(dirInputFile != null);
                    string line;
                    while (!sr.EndOfStream && (line = sr.ReadLine()) != null)
                    {
                        var fileName = Path.IsPathRooted(line) ? line : Path.Combine(dirInputFile, line);
                        var ft = fileName.GetPathFileSystemType();
                        switch (ft)
                        {
                            case FileSystemHelper.FileSystemObjectTypes.File:
                                var file = new FileInfo(fileName);
                                filesToRename.Add(file);
                                break;
                            case FileSystemHelper.FileSystemObjectTypes.Directory:
                                var dir = new DirectoryInfo(fileName);
                                dirsToRename.Add(dir);
                                break;
                        }
                    }
                }

                var referencedAssemblies = assembliesFile.GetReferencedAssemblyList();

                using (var srCode = new StreamReader(codeFile))
                {
                    var code = srCode.ReadToEnd();
                    RenameAction<FileInfo> renameFiles;
                    RenameAction<DirectoryInfo> renameDirs;
                    GetRenamersFromCode(code, referencedAssemblies, out renameFiles, out renameDirs);
                    var newFileNames = renameFiles(filesToRename);
                    var newDirNames = renameDirs(dirsToRename);

                    // ReSharper disable PossibleMultipleEnumeration
                    GenerateReport(filesToRename, newFileNames, confirmationFile);
                    GenerateReport(dirsToRename, newDirNames, confirmationFile, true);
                    // ReSharper restore PossibleMultipleEnumeration
                    if (toConfirm)
                    {
                        Process.Start(confirmationFile);
                        Console.Write("Please double check the report and press 'Y' or 'y' to confirm...");
                        var key = Console.ReadKey();
                        Console.WriteLine();
                        if (key.KeyChar != 'Y' && key.KeyChar != 'y')
                        {
                            Console.WriteLine("User cancelled.");
                            return;
                        }
                    }

                    if (copy)
                    {
                        // NOTE assumed files copy are followed by directories and target directories
                        // have been properly created

                        for (var i = 0; i < filesToRename.Count && i < newFileNames.Count; i++)
                        {
                            var newFileName = newFileNames[i];
                            if (newFileName.Trim() == string.Empty) continue;
                            var file = filesToRename[i];
                            var nameIsAbsolute = Path.IsPathRooted(newFileName);
                            if (!nameIsAbsolute)
                            {
                                newFileName = Path.Combine(file.DirectoryName, newFileName);
                                newFileNames[i] = newFileName;  // update the name list for exporting
                            }
                            file.CopyTo(newFileName, true);
                        }

                        for (var i = 0; i < dirsToRename.Count && i < newDirNames.Count; i++)
                        {
                            var newDirName = newDirNames[i];
                            if (newDirName.Trim() == string.Empty) continue;
                            var dir = dirsToRename[i];
                            var dirParent = dir.Parent;
                            if (dirParent == null) continue; // a root dir can't be renamed
                            var nameIsAbsolute = Path.IsPathRooted(newDirName);
                            if (!nameIsAbsolute)
                            {
                                newDirName = Path.Combine(dirParent.FullName, newDirName);
                                newDirNames[i] = newDirName;    // update the name list for exporting
                            }
                            // NOTE directory copy merges source and target
                            if (!Directory.Exists(newDirName))
                            {
                                Directory.CreateDirectory(newDirName);
                            }
                            dir.SelectFilesPostOrder(d => true,
                                                     (f, rel) => f.CopyTo(Path.Combine(newDirName, rel, f.Name), true),
                                                     (d, rel) =>
                                                         {
                                                             var target = Path.Combine(newDirName, rel, d.Name);
                                                             if (!Directory.Exists(target))
                                                                 Directory.CreateDirectory(target);
                                                         });
                        }
                    }
                    else
                    {
                        // ReSharper disable PossibleMultipleEnumeration
                        filesToRename.RenameFiles(newFileNames);
                        dirsToRename.RenameDirectories(newDirNames);
                        // ReSharper restore PossibleMultipleEnumeration
                    }

                    if (outputFile != null)
                    {
                        using (var swOut = new StreamWriter(outputFile))
                        {
                            foreach (var newFileName in newFileNames)
                            {
                                if (newFileName.Trim() == string.Empty) continue;   // non-exist target file not exported
                                swOut.WriteLine(newFileName);
                            }
                            foreach (var newDirName in newDirNames)
                            {
                                if (newDirName.Trim() == string.Empty) continue;    // non-exist target dir not exported
                                swOut.WriteLine(newDirName);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (e.Message == CompilationHelper.CompilationFailure)
                {
                    var errors = (CompilerErrorCollection) e.Data[CompilationHelper.DataKeyErrors];
                    var output = (StringCollection) e.Data[CompilationHelper.DataKeyOutput];
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
                else if (e.Message == MultiFileRenamer.FileError)
                {
                    var detail = e.Data[MultiFileRenamer.FileErrorDetail];
                    Console.WriteLine("Error processing file, details as below,");
                    Console.WriteLine(detail);
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