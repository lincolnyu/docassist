using System.IO;

namespace SelectScript
{
    class Program
    {
        public static bool FileSelector(FileInfo file)
		{
			if (file.Extension.ToLower() != ".txt") return false;
			
			var tempFile = Path.Combine(file.Directory.FullName, "___temp.box");
			
			// process the box file
			using (var sr = new StreamReader(file.OpenRead()))
			{
			    using (var sw = new StreamWriter(tempFile))
				{
					string line;
					while (!sr.EndOfStream && (line = sr.ReadLine())!=null)
					{
					    sw.WriteLine(line.Length>0 && line[0] == (char)0x10
					                     ? "================================================================================"
					                     : line);
					}
				}
			}
			
			var fileName = file.FullName;
			file.Delete();
			File.Move(tempFile, fileName);
			
			return true;
		}
    }
}