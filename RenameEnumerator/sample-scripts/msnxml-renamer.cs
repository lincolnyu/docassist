using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RenameScript
{
    class Program
    {
        static int CompareDate(int y1, int m1, int d1, int y2, int m2, int d2)
        {
            var c = y1.CompareTo(y2);
            if (c != 0) return c;
            c = m1.CompareTo(m2);
            return c != 0 ? c : d1.CompareTo(d2);
        }

        public static IList<string> Rename(IEnumerable<FileSystemInfo> files)
		{
			var newNames = new List<string>();
			foreach (var f in files)
			{
				if (!(f is FileInfo))
				{
					newNames.Add(f.FullName);	// ignore non-files
					continue;
				}
				var file = (FileInfo)f;
				if (file.Length == 0)
				{
					newNames.Add("");	// remove empty files
					continue;
				}
                int minDateYear = 0, minDateMonth = 0, minDateDay = 0;
                int maxDateYear = 0, maxDateMonth = 0, maxDateDay = 0;
			    var firstTime = true;
				using (var sr = new StreamReader(file.OpenRead()))
				{
					var fullText = sr.ReadToEnd();
				    //Console.WriteLine("read : {0}", f.FullName);
				    var startIndex = 0;
				    const string target = "<Message Date=\"";
                    while (startIndex < fullText.Length)
                    {
                        var position = fullText.IndexOf(target, startIndex, System.StringComparison.Ordinal);
                        if (position < 0) break;
                        var readPos = position + target.Length;
                        var readEnd = fullText.IndexOf('"', readPos);
                        var dateString = fullText.Substring(readPos, readEnd - readPos);
                        var comps = dateString.Split('-');
                        int year, month, day;
                        if (comps.Length < 3)
                        {
                            comps = dateString.Split('/');
                            //Console.WriteLine("{0} {1} {2}", comps[0], comps[1], comps[2]);
                            year = int.Parse(comps[2]);
                            month = int.Parse(comps[1]);
                            day = int.Parse(comps[0]);
                        }
                        else
                        {
                            year = int.Parse(comps[0]);
                            month = int.Parse(comps[1]);
                            day = int.Parse(comps[2]);
                        }

                        if (firstTime || CompareDate(year, month, day, minDateYear, minDateMonth, minDateDay) < 0)
                        {
                            minDateDay = day;
                            minDateMonth = month;
                            minDateYear = year;
                        }
                        if (firstTime || CompareDate(year, month, day, maxDateYear, maxDateMonth, maxDateDay) > 0)
                        {
                            maxDateDay = day;
                            maxDateMonth = month;
                            maxDateYear = year;
                        }
                        firstTime = false;
                        startIndex = readEnd + 1;
                    }
				}

				//Console.WriteLine("processed");
			    var datePrefix = string.Format("{0:0000}{1:00}{2:00}-{3:0000}{4:00}{5:00}", minDateYear, minDateMonth, minDateDay,
			                                   maxDateYear, maxDateMonth, maxDateDay);


			    var fileName = Path.GetFileNameWithoutExtension(file.Name);
			    var i = 0;
			    for (; i < fileName.Length && (fileName[i] < '0' || fileName[i] > '9'); i++) ;

			    var newName = new StringBuilder(datePrefix);
			    newName.Append('-');
			    newName.Append(fileName.Substring(0, i));
				newName.Append(".xml");
                newNames.Add(Path.Combine(@"F:\_bkp\Documents\My Mails\chatlog\msn\output", newName.ToString()));
			}

			return newNames;
		}
    }
}
