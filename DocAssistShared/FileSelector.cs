using System.IO;
using System.Text;
using DocAssistRuntime;

namespace DocAssistShared
{
    /// <summary>
    ///  A helper class that selects files using customisable filters
    /// </summary>
    public static class FileSelector
    {
        #region Delegates

        /// <summary>
        ///  A delegate that visits a specified file or directory
        /// </summary>
        /// <param name="fileSystemInfo">The file or directory to visit</param>
        /// <param name="relativeDir">The current relative dir the file/directory sits in</param>
        public delegate void Visit<in T>(T fileSystemInfo, string relativeDir) where T : FileSystemInfo;


        #endregion

        #region Methods

        /// <summary>
        ///  Recursively select files from the start folder and its sub-directories in pre-order (lowest level first)
        /// </summary>
        /// <param name="startFolder">The folder to start with</param>
        /// <param name="directoryFilter">The filter that determines if a subdirectory should be considered</param>
        /// <param name="visitFile">The method that receives and processes the file selected</param>
        /// <param name="visitDir">The method that receives and processes the directory selected</param>
        /// <param name="currRelativeDir">The current directory relative to the initial start folder</param>
        public static void SelectFilesPreOrder(this DirectoryInfo startFolder,
                                               FileSysItemPredicate<DirectoryInfo> directoryFilter,
                                               Visit<FileInfo> visitFile, Visit<DirectoryInfo> visitDir,
                                               string currRelativeDir = "")
        {
            if (visitFile != null)
            {
                foreach (var f in startFolder.GetFiles())
                {
                    visitFile(f, currRelativeDir);
                }
            }

            foreach (var d in startFolder.GetDirectories())
            {
                visitDir?.Invoke(d, currRelativeDir);

                if (directoryFilter == null || directoryFilter(d))
                {
                    var sb = new StringBuilder(currRelativeDir);
                    sb.Append(d.Name);
                    sb.Append(Path.PathSeparator);
                    SelectFilesPreOrder(d, directoryFilter, visitFile, visitDir, sb.ToString());
                }
            }
        }

        /// <summary>
        ///  Recursively select files from the start folder and its sub-directories in post-order (highest level first)
        /// </summary>
        /// <param name="startFolder">The folder to start with</param>
        /// <param name="directoryFilter">The filter that determines if a subdirectory should be considered</param>
        /// <param name="visitFile">The method that receives and processes the file selected</param>
        /// <param name="visitDir">The method that receives and processes the directory selected</param>
        /// <param name="currRelativeDir">The current directory relative to the initial start folder</param>
        public static void SelectFilesPostOrder(this DirectoryInfo startFolder, FileSysItemPredicate<DirectoryInfo> directoryFilter,
                                                Visit<FileInfo> visitFile, Visit<DirectoryInfo> visitDir, string currRelativeDir="")
        {
            foreach (var d in startFolder.GetDirectories())
            {
                if (directoryFilter == null || directoryFilter(d))
                {
                    var sb = new StringBuilder(currRelativeDir);
                    sb.Append(d.Name);
                    sb.Append(Path.PathSeparator);
                    SelectFilesPreOrder(d, directoryFilter, visitFile, visitDir, sb.ToString());
                }

                visitDir?.Invoke(d, currRelativeDir);
            }

            if (visitFile != null)
            {
                foreach (var f in startFolder.GetFiles())
                {
                    visitFile(f, currRelativeDir);
                }
            }
        }

        #endregion
    }
}
