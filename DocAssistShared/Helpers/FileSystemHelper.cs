using System;
using System.IO;

namespace DocAssistShared.Helpers
{
    public static class FileSystemHelper
    {
        public enum FileSystemObjectTypes
        {
            NotFound,
            File,
            Directory
        }

        public static FileSystemObjectTypes GetPathFileSystemType(this string path)
        {
            if (File.Exists(path)) return FileSystemObjectTypes.File;
            if (Directory.Exists(path)) return FileSystemObjectTypes.Directory;
            return FileSystemObjectTypes.NotFound;
        }

        public static string GetDistinctPath(this string filename)
        {
            // TODO what is this method supposed to return?
            return filename;
        }
    }
}