using System.IO;

namespace DocAssistShared.Merging
{
    public class FileUnit
    {
        public enum CompareResults
        {
            Greater = -2,
            Contained,
            Equal,
            Containing,
            Less
        }

        public FileUnit() { }

        /// <summary>
        ///  Constructs a file unit
        /// </summary>
        /// <param name="originalPath">The actual absolute path where the file is located</param>
        /// <param name="virtualPath">
        ///  The virtual path (should always be a 'relative kind of path') that is used to compare with other file unit
        /// </param>
        public FileUnit(string originalPath, string virtualPath)
        {
            OriginalPath = originalPath;
            VirtualPath = virtualPath;
        }

        public string OriginalPath { get; }
        public string VirtualPath { get; }

        public virtual CompareResults CompareTo(FileUnit other)
        {
            if (VirtualPath == other.VirtualPath) return CompareResults.Equal;
            if (VirtualPath.StartsWith(other.VirtualPath)) return CompareResults.Containing;
            if (other.VirtualPath.StartsWith(VirtualPath)) return CompareResults.Contained;
            return VirtualPath.CompareTo(other.VirtualPath) > 0 ? CompareResults.Greater : CompareResults.Less;
        }

        public static FileUnit Create(string originalPath, string basePath)
        {
            System.Diagnostics.Debug.Assert(originalPath.StartsWith(basePath));
            var virtualPath = originalPath.Substring(basePath.Length);
            virtualPath = virtualPath.TrimStart(Path.DirectorySeparatorChar);
            return new FileUnit(originalPath, virtualPath);
        }
    }
}
