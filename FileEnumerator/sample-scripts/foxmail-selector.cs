using System.IO;

namespace SelectScript
{
	class Program
	{
		public static bool FileSelector(FileInfo file)
		{
			return file.Extension.ToLower() == ".box";
		}
	}
}