using System.IO;

namespace SelectScript
{
    public class Program
    {
        public static bool FileSelector(FileInfo file)
        {
            return file.Extension.ToLower() == ".xml";
        }
    }
}
