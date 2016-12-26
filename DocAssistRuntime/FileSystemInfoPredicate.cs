using System.IO;

namespace DocAssistRuntime
{
    /// <summary>
    ///  A delegate for user to customise which file system item to choose
    /// </summary>
    /// <param name="fileSystemInfo">The file/directory to be judged if it's to be selected</param>
    /// <returns>true if it's selected</returns>
    public delegate bool FileSysItemPredicate<in T>(T fileSystemInfo) where T : FileSystemInfo;
}
