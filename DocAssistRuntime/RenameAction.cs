using System.Collections.Generic;
using System.IO;

namespace DocAssistRuntime
{
    /// <summary>
    ///  A delegate that renames the specified files (and/or directories) and returns the 
    ///  corresponding new names for them.
    /// </summary>
    /// <param name="files">
    ///  The files and folders to rename; Note the implementation is not allowed to change them 
    ///  or their order
    /// </param>
    /// <returns>
    ///  The new names to be assigned to the files; they can either be absolute paths (preferrably)
    ///  or paths relative to the original location of the files
    /// </returns>
    /// <remarks>
    ///  The uniqueness of the returned file name is ensured by the implementation of the delegate;
    ///  So are the dependencies between the files and directories; It's also the implementing 
    ///  method's responsibility to ensure the type consistency between the source and target
    ///  However it doesn't have to ensure the new names are different from any existing names
    /// </remarks>
    public delegate IList<string> RenameAction<in T>(IEnumerable<T> files) where T : FileSystemInfo;
}
