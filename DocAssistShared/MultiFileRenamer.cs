using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DocAssistShared
{
    /// <summary>
    ///  A class that renames multi-file as per guidelines 
    /// </summary>
    public static class MultiFileRenamer
    {
        #region Delegates

        /// <summary>
        ///  A delegate that removes a file/directory at specified path
        /// </summary>
        /// <param name="path">The path to the file to remove</param>
        public delegate void RemoveAction(string path);


        /// <summary>
        ///  A delegate that moves a file/directory at the specified old path to the specified new path
        /// </summary>
        /// <param name="oldPath">The path to the file or directory</param>
        /// <param name="newPath">The path the file or directory is to be moved to</param>
        public delegate void RenameAction(string oldPath, string newPath);

        #endregion

        #region Nested types

        /// <summary>
        ///  A node that contains an absolute file name that a file to be renamed has 
        ///  or a file should be renamed
        /// </summary>
        class NameNode
        {
            /// <summary>
            ///  Instantiates a name node
            /// </summary>
            /// <param name="name">The file name</param>
            public NameNode(string name)
            {
                Name = name;
            }

            #region Properties

            /// <summary>
            ///  The name string this node contains
            /// </summary>
            public string Name { get; private set; }

            /// <summary>
            ///  The file that should be renamed to this name if existent
            /// </summary>
            public NameNode ToRename { get; set; }

            #endregion
        }

        #endregion

        #region Fields

        /// <summary>
        ///  A string that names the exception group for file errors
        /// </summary>
        public const string FileError = "File Error";

        /// <summary>
        ///  A key to the data field for details of a file error exception
        /// </summary>
        public const string FileErrorDetail = "File Error Detail";

        #endregion

        #region Methods

        /// <summary>
        ///  Tries to create in the specified folder a new file without name specified but 
        ///  a list of names to avoid
        /// </summary>
        /// <param name="folderDir">The folder where the new file is to be created</param>
        /// <param name="namesToAvoid">An exclusion list of files</param>
        /// <returns>The full path of the new file or null if failed</returns>
        static string TryCreateFileInFolder(string folderDir, IReadOnlyDictionary<string, NameNode> namesToAvoid)
        {
            string tempFp = null;
            const string validChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            const int maxFileNameLen = 262;
            var fileLenExceeded = false;
            for (var nameLen = 1; !fileLenExceeded && tempFp == null; nameLen++)
            {
                var currSel = new byte[nameLen];
                for (var i = 0; i < nameLen; i++)
                {
                    currSel[i] = 0; // initialisation
                }

                int pos;
                do
                {
                    var sbTempFn = new StringBuilder();
                    for (var i = 0; i < nameLen; i++)
                    {
                        sbTempFn.Append(validChars[currSel[i]]);
                    }

                    var tempFn = sbTempFn.ToString();
                    tempFp = Path.Combine(folderDir, tempFn);
                    if (tempFp.Length > maxFileNameLen)
                    {
                        fileLenExceeded = true;
                        tempFp = null;
                        break;
                    }

                    if (!namesToAvoid.ContainsKey(tempFp))
                    {
                        break;  // found
                    }

                    tempFp = null;

                    for (pos = 0; pos < nameLen; pos++)
                    {
                        currSel[pos]++;
                        if (currSel[pos] < validChars.Length) break;
                    }
                } while (pos < nameLen);
            }
            return tempFp;
        }

        /// <summary>
        ///  returns a temporary file with an exclusion list in mind
        /// </summary>
        /// <param name="namesToAvoid">A list of the absolute file paths that the temporary file should avoid using</param>
        /// <returns>The absolute path to the temporary file</returns>
        static string GetTempFile(Dictionary<string, NameNode> namesToAvoid)
        {
            // generate a name that has been used as the name for temporary file
            // only use alphanumeric characters for temporary file name

            foreach (var val in namesToAvoid.Keys)
            {
                var dir = Path.GetDirectoryName(val);
                var tempFp = TryCreateFileInFolder(dir, namesToAvoid);

                if (tempFp != null)
                {
                    return tempFp;
                }
            }
            // try to create file at system temp folder
            var tempFolder = Environment.GetEnvironmentVariable("Temp");
            return TryCreateFileInFolder(tempFolder, namesToAvoid);
        }

        /// <summary>
        ///  enumerates the files or directories and renames them as per the renamer
        /// </summary>
        /// <param name="fileSystemInfoItems">Files/directories to rename</param>
        /// <param name="newNames">Names used to rename those files/directories</param>
        /// <param name="rename">Method used to rename file/directories</param>
        /// <param name="remove">Names used to rename those files/directories</param>
        /// <remarks>
        ///  Here the renaming engine uses an algorithm that minimises the creation of temporary
        ///  files and/or folders
        /// </remarks>
        public static void RenameFiles(this IEnumerable<FileSystemInfo> fileSystemInfoItems, 
            IEnumerable<string> newNames, RenameAction rename, RemoveAction remove)
        {
            var nameToNode = new Dictionary<string, NameNode>();
            var pureRenamer = new HashSet<string>();  // a set of nodes that only renames others

            var enumFileSystemInfoItems = fileSystemInfoItems.GetEnumerator();
            var enumNames = newNames.GetEnumerator();

            enumFileSystemInfoItems.Reset();
            enumNames.Reset();

            var filesToRemove = new List<string>();
            while (enumFileSystemInfoItems.MoveNext() && enumNames.MoveNext())
            {
                var fileSystemInfo = enumFileSystemInfoItems.Current;
                var newName = enumNames.Current;
                var oldFullName = fileSystemInfo.FullName;

                if (newName.Trim() == string.Empty)
                {
                    filesToRemove.Add(oldFullName);
                    continue;
                }

                var nameIsAbsolute = Path.IsPathRooted(newName);
                string newFullName;

                if (!nameIsAbsolute)
                {
                    string parentDirName = null;
                    var fileInfo = fileSystemInfo as FileInfo;
                    if (fileInfo != null)
                    {
                        parentDirName = fileInfo.DirectoryName;
                    }
                    else
                    {
                        var directoryInfo = fileSystemInfo as DirectoryInfo;
                        if (directoryInfo != null)
                        {
                            var parentDir = directoryInfo.Parent;
                            if (parentDir != null)
                            {
                                parentDirName = parentDir.FullName;
                            }
                        }
                    }

                    if (parentDirName == null)
                    {
                        var e = new Exception(FileError);
                        e.Data[FileErrorDetail] = string.Format("Couldn't locate folder for new file {0}", newName);
                        throw e;
                    }

                    newFullName = Path.Combine(parentDirName, newName);
                }
                else
                {
                    newFullName = newName;
                }

                if (string.Equals(oldFullName, newFullName))
                {
                    continue;   // no change needed, skipped
                }

                NameNode nodeOfOldName;
                if (!nameToNode.ContainsKey(oldFullName))
                {
                    nodeOfOldName = new NameNode(oldFullName);
                    nameToNode[oldFullName] = nodeOfOldName;
                }
                else
                {
                    nodeOfOldName = nameToNode[oldFullName];
                }

                NameNode nodeOfNewName;
                if (!nameToNode.ContainsKey(newFullName))
                {
                    nodeOfNewName = new NameNode(newFullName);
                    pureRenamer.Add(newFullName);
                    nameToNode[newFullName] = nodeOfNewName;
                }
                else
                {
                    nodeOfNewName = nameToNode[newFullName];
                }

                nodeOfNewName.ToRename = nodeOfOldName;

                if (pureRenamer.Contains(oldFullName))
                {
                    pureRenamer.Remove(oldFullName);
                }
            }

            // rename chains
            if (pureRenamer.Count > 0)
            {
                foreach (var name in pureRenamer)
                {
                    var node = nameToNode[name];
                    var targetName = node.Name;
                    var curr = node.ToRename;
                    nameToNode.Remove(name);
                    for (; curr != null; curr = curr.ToRename)
                    {
                        var sourceName = curr.Name;
                        rename(sourceName, targetName);
                        targetName = sourceName;
                        nameToNode.Remove(sourceName);
                    }
                }
            }

            // rename loops
            while (nameToNode.Count > 0)
            {
                var tempFile = GetTempFile(nameToNode);
                if (tempFile == null)
                {
                    throw new ApplicationException("Unable to create a temporary file");
                }

                var first = nameToNode.Values.First();
                var curr = first;
                NameNode last;
                var targetName = tempFile;
                do
                {
                    var sourceName = curr.Name;
                    rename(sourceName, targetName);
                    targetName = sourceName;
                    last = curr;
                    nameToNode.Remove(sourceName);
                    curr = curr.ToRename;
                } while (curr != first);
                rename(tempFile, last.Name);
            }

            // remove files/directories
            foreach (var file in filesToRemove)
            {
                remove(file);
            }
        }

        /// <summary>
        ///  enumerates a list of files and renames them as per the renamer
        /// </summary>
        /// <param name="files">Files to rename</param>
        /// <param name="newNames">Names used to rename those files</param>
        /// <remarks>
        ///  Note it's assumed and it's so conducted that the manipulation of directories always
        ///  happens after that of files
        /// </remarks>
        public static void RenameFiles(this IEnumerable<FileInfo> files, IEnumerable<string> newNames)
        {
            // TODO what if target file exists or file is not deletable
            RenameFiles(files, newNames,
                        (s, t) =>
                            {
                                if (File.Exists(t))
                                {
                                    File.Delete(t);
                                }
                                File.Move(s, t);
                            },
                        File.Delete);
        }

        /// <summary>
        ///  enumerates a list of files and renames them as per the renamer
        /// </summary>
        /// <param name="dirs">directories to rename</param>
        /// <param name="newNames">Names used to rename those files</param>
        /// <remarks>
        ///  Note it's assumed and it's so conducted that the manipulation of directories always
        ///  happens after that of files;
        ///  Also note Directory.Move and Directory.Delete works on all contents and the renaming operation
        ///  deletes the target if it exists instead of merging directories
        /// </remarks>
        public static void RenameDirectories(this IEnumerable<DirectoryInfo> dirs, IEnumerable<string> newNames)
        {

            RenameFiles(dirs, newNames,
                        (s, t) =>
                            {
                                if (Directory.Exists(t))
                                {
                                    Directory.Delete(t);
                                }
                                Directory.Move(s, t);
                            },
                        d => Directory.Delete(d, true));
        }

        #endregion
    }
}
